using Inventory.Application.Constants;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Helpers;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class InventoryApplication : IInventoryApplication
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IInventoryConfigurationApplication _configurationApplication;
        private readonly IPermissionApplication _permissionApplication;
        private readonly ILogApplication _logApplication;

        public InventoryApplication(
            IInventoryRepository inventoryRepository,
            IInventoryConfigurationApplication configurationApplication,
            IPermissionApplication permissionApplication,
            ILogApplication logApplication)
        {
            _inventoryRepository = inventoryRepository;
            _configurationApplication = configurationApplication;
            _permissionApplication = permissionApplication;
            _logApplication = logApplication;
        }

        public async Task<InventoryCountsResultDto> GetCounts(string inventoryExecutionId, int? countNumber, string role)
        {
            if (string.IsNullOrWhiteSpace(inventoryExecutionId) || !Guid.TryParse(inventoryExecutionId.Trim(), out var executionGuid))
                return CountsFailure(InventoryCountsStatus.InvalidRequest, "El identificador de ejecución debe tener formato GUID.");
            if (countNumber.HasValue && countNumber.Value is < 1 or > 3)
                return CountsFailure(InventoryCountsStatus.InvalidRequest, "El número de conteo debe ser 1, 2 o 3.");
            if (string.IsNullOrWhiteSpace(role))
                return CountsFailure(InventoryCountsStatus.Forbidden, "El token no contiene un nameRole válido.");

            var canViewAll = await _permissionApplication.HasPermission(PermissionKeys.ViewAllSolutionCenters, role);
            var canViewWarehouses = !canViewAll &&
                await _permissionApplication.HasPermission(PermissionKeys.ViewWarehousesOnly, role);
            if (!canViewAll && !canViewWarehouses)
                return CountsFailure(InventoryCountsStatus.Forbidden, "El rol no tiene permisos para consultar centros de soluciones.");

            var canViewFullDetail = await _permissionApplication.HasPermission(PermissionKeys.ViewFullInventoryDetail, role);
            var canViewLimitedDetail = !canViewFullDetail &&
                await _permissionApplication.HasPermission(PermissionKeys.ViewLimitedInventoryDetail, role);
            if (!canViewFullDetail && !canViewLimitedDetail)
                return CountsFailure(InventoryCountsStatus.Forbidden, "El rol no tiene permisos para consultar el detalle del inventario.");
            if (!canViewFullDetail && !countNumber.HasValue)
                return CountsFailure(InventoryCountsStatus.InvalidRequest, "Debe indicar countNumber para consultar un inventario con permiso de detalle limitado.");

            var executionId = executionGuid.ToString();
            var data = await _inventoryRepository.GetCountsContext(executionId);
            if (data is null)
                return CountsFailure(InventoryCountsStatus.NotFound, "La ejecución de inventario no existe.");

            const long warehouseTypeId = 1;
            const long pointOfSaleTypeId = 2;
            if (!canViewAll && data.SolutionCenterTypeId != warehouseTypeId)
                return CountsFailure(InventoryCountsStatus.Forbidden, "El rol no tiene permisos para consultar este centro.");
            if (data.SolutionCenterTypeId != warehouseTypeId && data.SolutionCenterTypeId != pointOfSaleTypeId)
                throw new InvalidOperationException($"La ejecución {executionId} contiene un tipo de centro no reconocido.");

            var isWarehouse = data.SolutionCenterTypeId == warehouseTypeId;
            data.CountMode = isWarehouse ? "SINGLE" : "OPEN_CLOSED";
            // El filtro autorizado llega a SQL antes de leer los valores; FULL tiene prioridad sobre LIMITED.
            data.Items = await _inventoryRepository.GetCounts(executionId, countNumber);
            foreach (var item in data.Items)
            {
                foreach (var count in item.Counts)
                {
                    if (isWarehouse)
                    {
                        count.Open = null;
                        count.Closed = null;
                    }
                    else
                    {
                        count.Value = null;
                    }
                }
            }

            return new InventoryCountsResultDto { Status = InventoryCountsStatus.Success, Data = data };
        }

        private static InventoryCountsResultDto CountsFailure(InventoryCountsStatus status, string message)
        {
            return new InventoryCountsResultDto { Status = status, Message = message };
        }

        public async Task<CreateInventoryResultDto> Create(CreateInventoryDto request, string userLogin, string role)
        {
            var validationError = InventoryRequestValidation.Validate(request);
            if (validationError is not null)
                return Failure(InventorySaveStatus.InvalidRequest, validationError);
            if (string.IsNullOrWhiteSpace(userLogin))
                return Failure(InventorySaveStatus.Forbidden, "El token no contiene un userLogin válido.");
            if (string.IsNullOrWhiteSpace(role))
                return Failure(InventorySaveStatus.Forbidden, "El token no contiene un nameRole válido.");

            var isExistingExecution = !string.IsNullOrWhiteSpace(request.InventoryExecutionId);
            var executionGuid = Guid.Empty;
            if (isExistingExecution && !Guid.TryParse(request.InventoryExecutionId!.Trim(), out executionGuid))
                return Failure(InventorySaveStatus.InvalidExecution, "El identificador de ejecución debe tener formato GUID.");

            // Reutiliza visibilidad, centro activo, fechas/día y asignaciones activas de configuración/secciones.
            var availability = await _configurationApplication.GetAvailableInventorySections(
                request.SolutionCenterId, request.InventoryConfigurationId, role);
            if (!availability.IsValidRole)
                return Failure(InventorySaveStatus.Forbidden, "El rol del token no tiene permisos para esta consulta.");
            if (!availability.IsAllowed)
                return Failure(InventorySaveStatus.Forbidden, "El rol no tiene permisos para consultar este centro.");

            var canViewFullDetail = await _permissionApplication.HasPermission(PermissionKeys.ViewFullInventoryDetail, role);
            var canViewLimitedDetail = !canViewFullDetail &&
                await _permissionApplication.HasPermission(PermissionKeys.ViewLimitedInventoryDetail, role);
            if (!canViewFullDetail && !canViewLimitedDetail)
                return Failure(InventorySaveStatus.Forbidden, "El rol no tiene permisos para consultar el detalle del inventario.");

            if (!availability.SolutionCenterExists)
                return Failure(InventorySaveStatus.InvalidContext, "La bodega o punto de venta no existe.");
            if (!availability.ConfigurationExists)
                return Failure(InventorySaveStatus.InvalidContext, "La configuración de inventario no existe.");
            if (!availability.IsAvailable)
                return Failure(InventorySaveStatus.InvalidContext, "La configuración de inventario no está disponible para este centro.");
            if (!availability.Data.Any(section => section.SectionId == request.SectionId))
                return Failure(InventorySaveStatus.InvalidContext, "La sección no está disponible para esta configuración y centro.");

            var context = await _inventoryRepository.GetContext(
                request.SolutionCenterId, request.InventoryConfigurationId, request.SectionId);
            if (context is null || !context.SolutionCenter.is_active || !context.Section.is_active)
                return Failure(InventorySaveStatus.InvalidContext, "El contexto del inventario ya no está disponible.");

            // Convención de tipos utilizada actualmente en InventoryConfigurationApplication/Repository.
            const long warehouseTypeId = 1;
            const long pointOfSaleTypeId = 2;
            var typeId = context.SolutionCenter.solution_center_type_id;
            if (typeId != warehouseTypeId && typeId != pointOfSaleTypeId)
                return Failure(InventorySaveStatus.InvalidContext, "El tipo de centro no permite este formato de inventario.");

            var items = request.Items!.Select(item => item!).ToList();
            if (typeId == warehouseTypeId && items.Any(item => item.Open.HasValue || item.Closed.HasValue))
                return Failure(InventorySaveStatus.InvalidRequest, "Un inventario de bodega no puede contener open ni closed.");
            if (typeId == pointOfSaleTypeId && items.Any(item => item.CountValue.HasValue))
                return Failure(InventorySaveStatus.InvalidRequest, "Un inventario de punto de venta no puede contener countValue.");

            // Rechazar valores no representables en la tabla, sin redondearlos ni recalcularlos.
            if (items.Any(item => !FitsDecimal(item.CountValue) || !FitsDecimal(item.Open) ||
                !FitsDecimal(item.Closed) || !FitsDecimal(item.MultiplicationValue)))
                return Failure(InventorySaveStatus.InvalidRequest, "Los valores numéricos deben caber en DECIMAL(18,4), sin más de cuatro decimales.");

            var executionId = isExistingExecution ? executionGuid.ToString() : Guid.NewGuid().ToString();
            if (isExistingExecution)
            {
                var executionContexts = (await _inventoryRepository.GetExecutionContexts(executionId)).ToList();
                if (executionContexts.Count == 0)
                    return Failure(InventorySaveStatus.InvalidExecution, "La ejecución de inventario indicada no existe.");
                if (executionContexts.Any(existing => existing.SolutionCenterId != request.SolutionCenterId ||
                    existing.InventoryConfigurationId != request.InventoryConfigurationId || existing.SectionId != request.SectionId))
                    return Failure(InventorySaveStatus.InvalidExecution, "La ejecución de inventario pertenece a otro centro, configuración o sección.");
                if (request.CountNumber == 1 && executionContexts.Any(existing => existing.CountNumber == 1))
                    return Failure(InventorySaveStatus.Duplicate, "El Conteo 1 ya fue guardado para esta ejecución.");
            }

            var productIds = items.Select(item => item.ProductId).Distinct().ToList();
            var products = (await _inventoryRepository.GetProducts(
                request.SolutionCenterId, request.SectionId, productIds)).ToDictionary(product => product.product_id);
            if (productIds.Any(productId => !products.ContainsKey(productId)))
                return Failure(InventorySaveStatus.InvalidRequest, "Uno o más productos no existen o no pertenecen al centro y sección seleccionados.");

            if (items.GroupBy(item => new
                {
                    item.ProductId,
                    UnitOfMeasure = products[item.ProductId].unit_of_measure
                }).Any(group => group.Count() > 1))
                return Failure(InventorySaveStatus.Duplicate, "La petición contiene productos duplicados para el mismo conteo y unidad de medida.");

            var records = items.Select(item =>
            {
                var product = products[item.ProductId];
                return new InventoryRecord
                {
                    inventory_execution_id = executionId,
                    solution_center_id = context.SolutionCenter.solution_center_id,
                    solution_center_type_id = typeId,
                    solution_center_code = context.SolutionCenter.solution_center_code,
                    solution_center_name = context.SolutionCenter.solution_center_name,
                    inventory_configuration_id = context.Configuration.inventory_configuration_id,
                    inventory_configuration_name = context.Configuration.inventory_configuration_name,
                    section_id = context.Section.section_id,
                    section_name = context.Section.section_name,
                    product_id = product.product_id,
                    reference = product.reference,
                    product_name = product.product_name,
                    unit_of_measure = product.unit_of_measure,
                    plan_id = product.plan_id,
                    count_number = (byte)request.CountNumber,
                    count_value = typeId == warehouseTypeId ? item.CountValue : null,
                    open = typeId == pointOfSaleTypeId ? item.Open : null,
                    closed = typeId == pointOfSaleTypeId ? item.Closed : null,
                    multiplication_value = item.MultiplicationValue,
                    entered_by = request.EnteredBy!.Trim()
                    // created_at queda a cargo del default de MySQL ya mapeado.
                };
            }).ToList();

            const string duplicateMessage = "El conteo contiene registros ya guardados para esta ejecución.";
            if (await _inventoryRepository.HasDuplicates(records))
                return Failure(InventorySaveStatus.Duplicate, duplicateMessage);
            if (!await _inventoryRepository.Save(records, isExistingExecution))
                return Failure(InventorySaveStatus.Duplicate, duplicateMessage);

            var auditSuffix = $" Ejecución: {executionId}. Registros: {records.Count}.";
            var auditDescription = $"Se guardó el conteo {request.CountNumber} del inventario {context.Configuration.inventory_configuration_name} " +
                $"para el centro {context.SolutionCenter.solution_center_name}, sección {context.Section.section_name}.";
            // El log actual admite 500 caracteres; conservar siempre ejecución y cantidad.
            if (auditDescription.Length + auditSuffix.Length > 500)
                auditDescription = auditDescription[..(500 - auditSuffix.Length - 3)] + "...";

            // El repositorio ya confirmó la colección. Mantener el flujo existente de ILogApplication.
            try
            {
                var logId = await _logApplication.CreateLog(new CreateLogDto
                {
                    Action = "Crear",
                    Module = "Inventarios",
                    Description = auditDescription + auditSuffix,
                    UserName = userLogin.Trim()
                });
                if (logId <= 0)
                    throw new InvalidOperationException("No se pudo registrar la auditoría del inventario.");
            }
            catch (Exception ex)
            {
                // El Controller registra esta excepción sin exponer detalles internos al cliente.
                throw new InvalidOperationException(
                    $"El inventario ya fue guardado, pero falló la auditoría. Ejecución: {executionId}. Conteo: {request.CountNumber}. Registros: {records.Count}.", ex);
            }

            return new CreateInventoryResultDto
            {
                Status = InventorySaveStatus.Success,
                Data = new InventorySaveDto
                {
                    InventoryExecutionId = executionId,
                    CountNumber = request.CountNumber,
                    SavedItems = records.Count
                }
            };
        }

        private static bool FitsDecimal(decimal? value)
        {
            const decimal maximum = 99999999999999.9999m;
            return !value.HasValue || (value.Value >= -maximum && value.Value <= maximum &&
                decimal.Round(value.Value, 4) == value.Value);
        }

        private static CreateInventoryResultDto Failure(InventorySaveStatus status, string message)
        {
            return new CreateInventoryResultDto { Status = status, Message = message };
        }
    }
}
