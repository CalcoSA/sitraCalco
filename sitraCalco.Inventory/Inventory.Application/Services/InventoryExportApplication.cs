using System.Globalization;
using Inventory.Application.Constants;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class InventoryExportApplication : IInventoryExportApplication
    {
        private const string ProfileCode = "SIESA_PDV_0412";
        private readonly IInventoryExportRepository _repository;
        private readonly IInventoryExportParameterRepository _parameters;
        private readonly IPermissionApplication _permissions;

        public InventoryExportApplication(IInventoryExportRepository repository,
            IInventoryExportParameterRepository parameters, IPermissionApplication permissions)
        {
            _repository = repository;
            _parameters = parameters;
            _permissions = permissions;
        }

        public async Task<InventoryExportResultDto> GetSiesa(string? date, IReadOnlyCollection<long>? solutionCenterIds,
            string? role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (date is null || date.Length != 10 || !DateOnly.TryParseExact(date, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) || parsedDate == DateOnly.MaxValue)
                return Failure(InventoryExportStatus.InvalidRequest, "Debe indicar una fecha válida con formato YYYY-MM-DD que permita calcular el día siguiente.");
            if (solutionCenterIds?.Any(id => id <= 0) == true)
                return Failure(InventoryExportStatus.InvalidRequest, "Los identificadores de centros de solución deben ser mayores a cero.");
            if (string.IsNullOrWhiteSpace(role))
                return Failure(InventoryExportStatus.Forbidden, "El token no contiene un nameRole válido.");

            var canViewAll = await _permissions.HasPermission(PermissionKeys.ViewAllSolutionCenters, role);
            var warehousesOnly = !canViewAll && await _permissions.HasPermission(PermissionKeys.ViewWarehousesOnly, role);
            if (!canViewAll && !warehousesOnly)
                return Failure(InventoryExportStatus.Forbidden, "El rol no tiene permisos para consultar centros de soluciones.");
            // Este GET reúne conteos; LIMITED no autoriza la exportación de detalle completo.
            if (!await _permissions.HasPermission(PermissionKeys.ViewFullInventoryDetail, role))
                return Failure(InventoryExportStatus.Forbidden, "La exportación SIESA requiere permiso de detalle completo del inventario.");

            var start = parsedDate.ToDateTime(TimeOnly.MinValue);
            var end = parsedDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var ids = solutionCenterIds?.Distinct().OrderBy(id => id).ToArray() ?? Array.Empty<long>();
            if (ids.Length > 0)
            {
                var centers = await _repository.GetCenters(ids, start, end, cancellationToken);
                if (warehousesOnly && centers.Any(center => center.SolutionCenterTypeId != 1 || center.HasNonWarehouseRecords))
                    return Failure(InventoryExportStatus.Forbidden, "Uno o más centros solicitados no están autorizados para el rol.");
                var missing = ids.Except(centers.Select(center => center.SolutionCenterId)).ToList();
                if (missing.Count > 0)
                    return Failure(InventoryExportStatus.NotFound, $"No existen los centros de solución con ID: {string.Join(", ", missing)}.");
            }

            var parameters = await _parameters.GetActiveByConfigurationCode(ProfileCode, cancellationToken);
            if (parameters is null)
                return Failure(InventoryExportStatus.InvalidParameters, $"No existe una configuración activa de parámetros SIESA con código {ProfileCode}.");
            if (parameters.detail_record_type != "0412")
                return Failure(InventoryExportStatus.InvalidParameters, "El perfil SIESA almacenado no tiene el tipo de registro detalle 0412 requerido.");

            var read = await _repository.Read(start, end, ids, warehousesOnly, cancellationToken);
            var data = new InventoryExportDto { Date = date, SiesaParameters = MapParameters(parameters) };
            var summaries = read.Summaries.ToDictionary(row => row.ExecutionId);
            var itemsByExecution = read.SelectedRows.ToLookup(row => row.Record.inventory_execution_id);

            foreach (var centerGroup in read.Headers.GroupBy(row => row.CenterId).OrderBy(group => group.Key))
            {
                var centerHeaders = centerGroup.OrderBy(row => row.ExecutionId, StringComparer.Ordinal)
                    .ThenBy(row => row.CenterCode, StringComparer.Ordinal).ThenBy(row => row.CenterName, StringComparer.Ordinal).ToList();
                var centerHeader = centerHeaders[0];
                var inconsistentCenter = centerHeaders.Select(row => (row.CenterCode, row.CenterName)).Distinct().Count() > 1;
                var center = new InventoryExportCenterDto
                {
                    SolutionCenterId = centerHeader.CenterId,
                    SolutionCenterCode = centerHeader.CenterCode,
                    SolutionCenterName = centerHeader.CenterName,
                    SiesaWarehouseCode = centerHeader.CenterCode
                };
                foreach (var executionGroup in centerHeaders.GroupBy(row => row.ExecutionId)
                    .OrderBy(group => summaries[group.Key].BaseF410).ThenBy(group => group.Key, StringComparer.Ordinal))
                {
                    var execution = new InventoryExportExecutionDto { InventoryExecutionId = executionGroup.Key };
                    center.Inventories.Add(execution);
                    var summary = summaries[executionGroup.Key];
                    if (summary.HasMixedContext)
                    {
                        AddDiagnostic(execution, "INCONSISTENT_EXECUTION", "La ejecución contiene registros de distintos centros, tipos, configuraciones o secciones. No se seleccionaron productos ni F410.");
                        continue;
                    }
                    if (!Guid.TryParse(executionGroup.Key, out var executionGuid) || executionGuid == Guid.Empty)
                        AddDiagnostic(execution, "INVALID_EXECUTION_ID", "La ejecución no tiene un identificador GUID válido.");
                    else if (summary.BaseF410 is < 1 or > 99999999)
                        AddDiagnostic(execution, "INVALID_F410", $"El mínimo inventory_id de la ejecución es {summary.BaseF410.ToString(CultureInfo.InvariantCulture)}; F410 requiere un valor entre 1 y 99999999.");
                    else
                        execution.F410 = summary.BaseF410.ToString("D8", CultureInfo.InvariantCulture);

                    var header = executionGroup.First();
                    execution.InventoryConfigurationId = header.ConfigurationId;
                    execution.SectionId = header.SectionId;
                    var configurationNames = executionGroup.Select(row => row.ConfigurationName).Distinct(StringComparer.Ordinal).ToList();
                    var sectionNames = executionGroup.Select(row => row.SectionName).Distinct(StringComparer.Ordinal).ToList();
                    execution.InventoryConfigurationName = configurationNames.Count == 1 ? configurationNames[0] : null;
                    execution.SectionName = sectionNames.Count == 1 ? sectionNames[0] : null;
                    if (inconsistentCenter || configurationNames.Count != 1 || sectionNames.Count != 1)
                        AddDiagnostic(execution, "INCONSISTENT_HISTORICAL_NAMES", "Los registros seleccionados contienen códigos o nombres históricos incompatibles. Deben revisarse antes de construir el plano.");
                    if (header.CenterTypeId is not (1 or 2))
                        AddDiagnostic(execution, "UNKNOWN_CENTER_TYPE", "El tipo histórico del centro no corresponde a una bodega o punto de venta reconocido.");
                    if (header.CenterId <= 0 || header.ConfigurationId <= 0 || header.SectionId <= 0 ||
                        string.IsNullOrWhiteSpace(header.CenterCode) || string.IsNullOrWhiteSpace(header.CenterName) ||
                        string.IsNullOrWhiteSpace(execution.InventoryConfigurationName) || string.IsNullOrWhiteSpace(execution.SectionName))
                        AddDiagnostic(execution, "INCOMPLETE_EXECUTION_CONTEXT", "Faltan identificadores, código de bodega o nombres históricos del centro, configuración o sección.");

                    foreach (var productGroup in itemsByExecution[executionGroup.Key]
                        .GroupBy(row => (row.Record.product_id, row.Record.unit_of_measure))
                        .OrderBy(group => group.Key.product_id).ThenBy(group => group.Key.unit_of_measure, StringComparer.Ordinal))
                    {
                        if (productGroup.Count() != 1 || productGroup.Any(row => row.HasDuplicateCounts))
                        {
                            AddDiagnostic(execution, "DUPLICATE_COUNT", "Existen filas duplicadas para un mismo conteo, producto y unidad en la fecha solicitada. No se eligió una fila arbitraria.",
                                productGroup.Key.product_id, productGroup.Key.unit_of_measure);
                            continue;
                        }
                        var row = productGroup.Single().Record;
                        execution.Items.Add(new InventoryExportItemDto
                        {
                            ProductId = row.product_id, Reference = row.reference, ProductName = row.product_name,
                            UnitOfMeasure = row.unit_of_measure, PlanId = row.plan_id, CountNumber = row.count_number,
                            CountValue = DecimalText(row.count_value), Open = DecimalText(row.open),
                            Closed = DecimalText(row.closed), MultiplicationValue = DecimalText(row.multiplication_value)
                        });
                        if (row.product_id <= 0 || string.IsNullOrWhiteSpace(row.reference) ||
                            string.IsNullOrWhiteSpace(row.product_name) || string.IsNullOrWhiteSpace(row.unit_of_measure))
                            AddDiagnostic(execution, "INCOMPLETE_PRODUCT", "El producto no tiene identificador, referencia, nombre o unidad de medida válidos en el registro histórico.", row.product_id, row.unit_of_measure);
                        if (row.count_number is < 1 or > 3)
                            AddDiagnostic(execution, "INVALID_COUNT_NUMBER", "El conteo seleccionado debe ser 1, 2 o 3.", row.product_id, row.unit_of_measure);
                        if (row.count_value is null && row.open is null && row.closed is null && row.multiplication_value is null)
                            AddDiagnostic(execution, "MISSING_COUNT_VALUES", "Los cuatro valores del conteo seleccionado son NULL. No se recuperaron valores de conteos anteriores.", row.product_id, row.unit_of_measure);
                    }
                }
                data.SolutionCenters.Add(center);
            }
            data.SolutionCenters = data.SolutionCenters.OrderBy(center => center.SolutionCenterCode, StringComparer.Ordinal)
                .ThenBy(center => center.SolutionCenterId).ToList();
            var invalidData = data.SolutionCenters.SelectMany(center => center.Inventories).Any(inventory => inventory.Diagnostics.Count > 0);
            return new InventoryExportResultDto
            {
                Status = invalidData ? InventoryExportStatus.InvalidData : InventoryExportStatus.Success,
                Message = invalidData
                    ? "La información contiene inconsistencias. Revisa los diagnósticos antes de generar el plano SIESA."
                    : data.SolutionCenters.Count == 0 ? "No se encontraron inventarios para la fecha y los centros autorizados."
                    : "Información para planos SIESA consultada correctamente.",
                Data = data
            };
        }

        private static string? DecimalText(decimal? value) => value?.ToString("0.0000", CultureInfo.InvariantCulture);
        private static InventoryExportResultDto Failure(InventoryExportStatus status, string message) => new() { Status = status, Message = message };
        private static void AddDiagnostic(InventoryExportExecutionDto execution, string code, string message, long? productId = null, string? unit = null) =>
            execution.Diagnostics.Add(new InventoryExportDiagnosticDto { Code = code, Message = message, ProductId = productId, UnitOfMeasure = unit });

        private static SiesaExportParametersDto MapParameters(InventoryExportParameter row) => new()
        {
            DetailRecordType = row.detail_record_type, DetailRecordSubtype = row.detail_record_subtype,
            DetailRecordVersion = row.detail_record_version, CompanyCode = row.company_code,
            ReservedField = row.reserved_field, AuxiliaryLocation = row.auxiliary_location, LotCode = row.lot_code,
            LabelNumber = row.label_number, Count_1_1 = row.count_1_1, Count_2_1 = row.count_2_1,
            Count_1_2 = row.count_1_2, Count_2_2 = row.count_2_2, Count_3_2 = row.count_3_2,
            Barcode = row.barcode, DetailExtension1 = row.detail_extension_1, DetailExtension2 = row.detail_extension_2,
            HeaderRecordType = row.header_record_type, HeaderRecordSubtype = row.header_record_subtype,
            HeaderRecordVersion = row.header_record_version, FooterRecordType = row.footer_record_type,
            FooterRecordSubtype = row.footer_record_subtype, FooterRecordVersion = row.footer_record_version
        };
    }
}
