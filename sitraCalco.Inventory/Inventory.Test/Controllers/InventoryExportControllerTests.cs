using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Inventory.Api.Controllers;
using Inventory.Application.Constants;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Dtos;
using Inventory.Domain.Models;
using Inventory.Domain.Responses;
using Inventory.Infrastructure.Persistance.Data;
using Inventory.Infrastructure.Persistance.Repositories;
using Inventory.Test.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Inventory.Test.Controllers;

// Consultas relacionales contra CTEs de solo lectura en SQLite en memoria.
// No usa MySQL, tablas persistidas, EnsureCreated, INSERT ni migraciones.
public class InventoryExportControllerTests : IAsyncLifetime
{
    private const string Day = "2026-10-30";
    private const string ExecutionA = "10000000-0000-0000-0000-000000000001";
    private const string ExecutionB = "10000000-0000-0000-0000-000000000002";
    private const string WarehouseExecution = "10000000-0000-0000-0000-000000000003";
    private static readonly DateTime Start = new(2026, 10, 30);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ReadOnlySource _source = new();
    private readonly Mock<IPermissionApplication> _permissions = new();
    private SitraCalcoContext _context = null!;
    private InventoryExportController _controller = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _context = new SitraCalcoContext(new DbContextOptionsBuilder<SitraCalcoContext>()
            .UseSqlite(_connection).AddInterceptors(_source).Options);
        _permissions.Setup(x => x.HasPermission(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _controller = new InventoryExportController(new InventoryExportApplication(new InventoryExportRepository(_context),
            new InventoryExportParameterRepository(_context), _permissions.Object), NullLogger<InventoryExportController>.Instance)
            .WithIdentity(role: "ADMINISTRADOR");
        _source.Centers.AddRange(new[]
        {
            new SolutionCenter { solution_center_id = 1, solution_center_type_id = 1, solution_center_code = "NUEVO1", solution_center_name = "Nombre actual 1", is_active = false },
            new SolutionCenter { solution_center_id = 2, solution_center_type_id = 2, solution_center_code = "NUEVO2", solution_center_name = "Nombre actual 2", is_active = false },
            new SolutionCenter { solution_center_id = 3, solution_center_type_id = 2, solution_center_code = "SIN", solution_center_name = "Sin inventarios", is_active = true }
        });
        _source.Parameters.Add(Profile());
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Theory]
    [InlineData("one", 1, 2, 5)]
    [InlineData("several", 2, 3, 5)]
    [InlineData("all", 2, 3, 4)]
    public async Task CentersExecutionsAndLatestCounts_AreIndependent_AndUseHistoricalData(
        string selection, int centers, int executions, int queries)
    {
        SeedCounts();
        string[]? ids = selection switch { "one" => new[] { "2" }, "several" => new[] { "2", "1", "2" }, _ => null };

        var data = Data(await _controller.GetSiesa(Day, ids), 200);

        Assert.Equal(Day, data.Date);
        Assert.Equal(centers, data.SolutionCenters.Count);
        Assert.Equal(executions, data.SolutionCenters.Sum(center => center.Inventories.Count));
        var pdv = data.SolutionCenters.Single(center => center.SolutionCenterId == 2);
        Assert.Equal(("R01", "Centro histórico 2", "R01"), (pdv.SolutionCenterCode, pdv.SolutionCenterName, pdv.SiesaWarehouseCode));
        var execution = pdv.Inventories.Single(item => item.InventoryExecutionId == ExecutionA);
        Assert.Equal((3L, 1L, "Configuración histórica", "Sección histórica"),
            (execution.InventoryConfigurationId, execution.SectionId, execution.InventoryConfigurationName, execution.SectionName));
        Assert.Equal("00000125", execution.F410);
        Assert.Equal(new long[] { 10, 10, 11, 12 }, execution.Items.Select(item => item.ProductId));
        Assert.Equal(new[] { 2, 1, 1, 3 }, execution.Items.Select(item => item.CountNumber));
        var product = execution.Items.Single(item => item.ProductId == 10 && item.UnitOfMeasure == "KG");
        Assert.Equal((null, "5.0000", "2.0000", "12.0000"), (product.CountValue, product.Open, product.Closed, product.MultiplicationValue));
        Assert.Equal("0002361", product.Reference);
        Assert.Equal("FIF", product.PlanId);
        Assert.Equal("77.0000", execution.Items.Single(item => item.ProductId == 10 && item.UnitOfMeasure == "UND").Open);
        var zero = execution.Items.Single(item => item.ProductId == 11);
        Assert.Equal((null, "0.0000", null, null), (zero.CountValue, zero.Open, zero.Closed, zero.MultiplicationValue));
        Assert.Equal(queries, _source.Commands.Count);
        Assert.Single(_source.Commands, command => command.Contains("FROM \"sitracalco_inventory_export_parameter\"", StringComparison.Ordinal));
        Assert.DoesNotContain(_source.Commands, command => command.Contains("date(", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(_context.ChangeTracker.Entries());
        Assert.DoesNotContain("finalQuantity", JsonSerializer.Serialize(data, Json));
        if (centers == 2)
        {
            Assert.Equal(new long[] { 1, 2 }, data.SolutionCenters.Select(center => center.SolutionCenterId));
            var warehouse = Assert.Single(data.SolutionCenters[0].Inventories);
            Assert.Equal("00000100", warehouse.F410);
            Assert.Equal("99999999999999.9999", Assert.Single(warehouse.Items).CountValue);
        }
    }

    [Fact]
    public async Task RepeatedQueriesAndNextDay_KeepGlobalF410_WhileCountsRespectTheRequestedDay()
    {
        SeedCounts();
        var first = Data(await _controller.GetSiesa(Day, new[] { "2" }), 200);
        var again = Data(await _controller.GetSiesa(Day, new[] { "2" }), 200);
        Assert.Equal(JsonSerializer.Serialize(first, Json), JsonSerializer.Serialize(again, Json));
        var later = Data(await _controller.GetSiesa("2026-10-31", new[] { "2" }), 200);
        var execution = Assert.Single(Assert.Single(later.SolutionCenters).Inventories);
        Assert.Equal("00000125", execution.F410);
        Assert.Equal("999.0000", Assert.Single(execution.Items).Open);
        Assert.Equal(3, execution.Items[0].CountNumber);
    }

    [Theory]
    [InlineData(1, 200, "00000001")]
    [InlineData(99999999, 200, "99999999")]
    [InlineData(100000000, 409, null)]
    [InlineData(0, 409, null)]
    [InlineData(-1, 409, null)]
    public async Task F410_ValidatesRangeWithoutTruncating(long id, int status, string? expected)
    {
        _source.Records.Add(Row(id, ExecutionA, 2, 10, 1, Start));
        var execution = OnlyExecution(Data(await _controller.GetSiesa(Day), status));
        Assert.Equal(expected, execution.F410);
        if (status == 409) Assert.Contains(execution.Diagnostics, item => item.Code == "INVALID_F410");
    }

    [Fact]
    public async Task Parameters_ExposeAll22StoredValues_IncludingSpacesAndLeadingZeroes()
    {
        _source.Records.Add(Row(125, ExecutionA, 2, 10, 1, Start));
        _source.Parameters[0].reserved_field = "   contenido reservado   ";
        _source.Parameters[0].company_code = "007";
        var data = Data(await _controller.GetSiesa(Day), 200);
        var actual = JsonSerializer.SerializeToElement(data.SiesaParameters, Json);
        Assert.Equal(22, actual.EnumerateObject().Count());
        var fields = new Dictionary<string, string>
        {
            ["detailRecordType"] = "0412", ["detailRecordSubtype"] = "00", ["detailRecordVersion"] = "02",
            ["companyCode"] = "007", ["reservedField"] = "   contenido reservado   ",
            ["auxiliaryLocation"] = new(' ', 10), ["lotCode"] = new(' ', 15), ["labelNumber"] = "000000000",
            ["count_1_1"] = "00000000000.0000", ["count_2_1"] = "00000000000.0000", ["count_1_2"] = "00000000000.0000",
            ["count_2_2"] = "00000000000.0000", ["count_3_2"] = "00000000000.0000",
            ["barcode"] = new(' ', 20), ["detailExtension1"] = new(' ', 20), ["detailExtension2"] = new(' ', 20),
            ["headerRecordType"] = "0000", ["headerRecordSubtype"] = "00", ["headerRecordVersion"] = "01",
            ["footerRecordType"] = "9999", ["footerRecordSubtype"] = "00", ["footerRecordVersion"] = "01"
        };
        foreach (var field in fields) Assert.Equal(field.Value, actual.GetProperty(field.Key).GetString());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("inactive")]
    [InlineData("wrong-detail")]
    public async Task InvalidProfile_ReturnsControlledError_WithoutReadingCounts(string kind)
    {
        if (kind == "missing") _source.Parameters.Clear();
        if (kind == "inactive") _source.Parameters[0].is_active = false;
        if (kind == "wrong-detail") _source.Parameters[0].detail_record_type = "0410";
        var response = Response(await _controller.GetSiesa(Day), 500);
        Assert.False(response.IsSuccess);
        Assert.Contains(kind == "wrong-detail" ? "0412" : "SIESA_PDV_0412", response.Message);
        Assert.Single(_source.Commands);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-2-01")]
    [InlineData("2026-02-30")]
    [InlineData("30/10/2026")]
    [InlineData("2026-10-30T00:00:00")]
    [InlineData("9999-12-31")]
    public async Task InvalidDates_Return400_WithResponseApi_WithoutReadingData(string? date)
    {
        Assert.False(Response(await _controller.GetSiesa(date), 400).IsSuccess);
        Assert.Empty(_source.Commands);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("1,2")]
    [InlineData("9223372036854775808")]
    public async Task InvalidCenterIds_Return400_WithoutReadingData(string id)
    {
        Assert.False(Response(await _controller.GetSiesa(Day, new[] { id }), 400).IsSuccess);
        Assert.Empty(_source.Commands);
    }

    [Fact]
    public async Task MissingCenter_Returns404_AndExistingCenterWithoutRecordsReturnsEmpty200()
    {
        Assert.Contains("99", Response(await _controller.GetSiesa(Day, new[] { "99" }), 404).Message);
        var response = Response(await _controller.GetSiesa(Day, new[] { "3" }), 200);
        Assert.True(response.IsSuccess);
        Assert.Contains("No se encontraron", response.Message);
        Assert.Empty(Assert.IsType<InventoryExportDto>(response.Result).SolutionCenters);
    }

    [Theory]
    [InlineData("no-centers")]
    [InlineData("limited")]
    [InlineData("no-role")]
    public async Task MissingPermissions_DoNotExposeAnyData(string kind)
    {
        if (kind == "no-role") _controller.WithIdentity(role: null);
        if (kind == "no-centers") _permissions.Setup(x => x.HasPermission(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        if (kind == "limited") _permissions.Setup(x => x.HasPermission(PermissionKeys.ViewFullInventoryDetail, It.IsAny<string>())).ReturnsAsync(false);
        Assert.False(Response(await _controller.GetSiesa(Day), 403).IsSuccess);
        Assert.Empty(_source.Commands);
    }

    [Fact]
    public async Task WarehousesOnly_FiltersAllCenters_AndRejectsExplicitPdvEvenInMixedRequest()
    {
        SeedCounts();
        _permissions.Setup(x => x.HasPermission(PermissionKeys.ViewAllSolutionCenters, It.IsAny<string>())).ReturnsAsync(false);
        var data = Data(await _controller.GetSiesa(Day), 200);
        Assert.Equal(1, Assert.Single(data.SolutionCenters).SolutionCenterId);
        Assert.False(Response(await _controller.GetSiesa(Day, new[] { "1", "2" }), 403).IsSuccess);
        _source.Centers.Single(center => center.solution_center_id == 2).solution_center_type_id = 1;
        Assert.False(Response(await _controller.GetSiesa(Day, new[] { "2" }), 403).IsSuccess);
    }

    [Fact]
    public async Task MixedExecutionOutsideDate_IsDiagnosedWithoutExposingOtherCenterCounts()
    {
        _source.Records.Add(Row(125, ExecutionA, 1, 10, 1, Start));
        var unauthorized = Row(126, ExecutionA, 2, 20, 2, Start.AddDays(1));
        unauthorized.product_name = "PRODUCTO NO AUTORIZADO";
        _source.Records.Add(unauthorized);
        _permissions.Setup(x => x.HasPermission(PermissionKeys.ViewAllSolutionCenters, It.IsAny<string>())).ReturnsAsync(false);
        var data = Data(await _controller.GetSiesa(Day, new[] { "1" }), 409);
        var execution = OnlyExecution(data);
        Assert.Null(execution.F410);
        Assert.Null(execution.InventoryConfigurationId);
        Assert.Empty(execution.Items);
        Assert.Contains(execution.Diagnostics, diagnostic => diagnostic.Code == "INCONSISTENT_EXECUTION");
        Assert.DoesNotContain("PRODUCTO NO AUTORIZADO", JsonSerializer.Serialize(data, Json));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicatedCounts_AreReportedWithoutArbitrarySelection(bool duplicateEarlierCount)
    {
        _source.Records.Add(Row(125, ExecutionA, 2, 10, 1, Start));
        _source.Records.Add(Row(126, ExecutionA, 2, 10, 1, Start));
        if (duplicateEarlierCount) _source.Records.Add(Row(127, ExecutionA, 2, 10, 3, Start));
        var execution = OnlyExecution(Data(await _controller.GetSiesa(Day), 409));
        Assert.Empty(execution.Items);
        var diagnostic = Assert.Single(execution.Diagnostics);
        Assert.Equal("DUPLICATE_COUNT", diagnostic.Code);
        Assert.Equal(10, diagnostic.ProductId);
        Assert.Equal("KG", diagnostic.UnitOfMeasure);
    }

    [Theory]
    [InlineData("reference", "INCOMPLETE_PRODUCT")]
    [InlineData("unit", "INCOMPLETE_PRODUCT")]
    [InlineData("count", "INVALID_COUNT_NUMBER")]
    [InlineData("null-values", "MISSING_COUNT_VALUES")]
    [InlineData("warehouse-code", "INCOMPLETE_EXECUTION_CONTEXT")]
    public async Task IncompleteRecords_ReturnDiagnostics_AndNeverReplaceNulls(string missing, string expectedCode)
    {
        var old = Row(125, ExecutionA, 2, 10, 1, Start);
        var latest = Row(126, ExecutionA, 2, 10, 3, Start);
        if (missing == "reference") latest.reference = "  ";
        if (missing == "unit") { old.unit_of_measure = ""; latest.unit_of_measure = ""; }
        if (missing == "count") latest.count_number = 4;
        if (missing == "null-values") latest.open = latest.closed = latest.multiplication_value = latest.count_value = null;
        if (missing == "warehouse-code") { old.solution_center_code = ""; latest.solution_center_code = ""; }
        _source.Records.AddRange(new[] { old, latest });
        var execution = OnlyExecution(Data(await _controller.GetSiesa(Day), 409));
        Assert.Contains(execution.Diagnostics, diagnostic => diagnostic.Code == expectedCode);
        var item = Assert.Single(execution.Items);
        if (missing == "null-values")
        {
            var json = JsonSerializer.SerializeToElement(item, Json);
            foreach (var field in new[] { "countValue", "open", "closed", "multiplicationValue" })
                Assert.Equal(JsonValueKind.Null, json.GetProperty(field).ValueKind);
        }
    }

    [Fact]
    public async Task DecimalStrings_AreInvariant_AndZeroDoesNotBecomeNull()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-CO");
            var row = Row(125, ExecutionA, 1, 10, 1, Start);
            row.count_value = 99999999999999.9999m;
            row.open = 0m;
            row.closed = null;
            row.multiplication_value = 1.2345m;
            _source.Records.Add(row);
            var item = Assert.Single(OnlyExecution(Data(await _controller.GetSiesa(Day), 200)).Items);
            Assert.Equal(("99999999999999.9999", "0.0000", null, "1.2345"),
                (item.CountValue, item.Open, item.Closed, item.MultiplicationValue));
            Assert.Equal(JsonValueKind.String, JsonSerializer.SerializeToElement(item, Json).GetProperty("countValue").ValueKind);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void LatestCountQuery_TranslatesWithMySqlProvider_WithoutOpeningAConnection()
    {
        using var context = new SitraCalcoContext(new DbContextOptionsBuilder<SitraCalcoContext>()
            .UseMySql("Server=unused;Database=unused;User ID=unused;", new MySqlServerVersion(new Version(8, 0, 36))).Options);
        var end = Start.AddDays(1);
        var executions = new[] { ExecutionA };
        var candidates = context.InventoryRecords.AsNoTracking().Where(row => row.created_at >= Start && row.created_at < end &&
            executions.Contains(row.inventory_execution_id));
        // Verifica la consulta utilizada por el repositorio con Pomelo, sin ejecutarla.
        var method = typeof(InventoryExportRepository).GetMethod("SelectLatestRows", BindingFlags.NonPublic | BindingFlags.Static)!;
        var query = Assert.IsAssignableFrom<IQueryable<InventoryExportSelectedRow>>(method.Invoke(null, new object[] { candidates }));
        var sql = query.ToQueryString();
        Assert.Contains("MAX(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT(DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unit_of_measure", sql);
        Assert.DoesNotContain("DATE(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task RouteRequiresJwt_ExposesOnlyTwoQueryParameters_AndHonorsCancellation()
    {
        var type = typeof(InventoryExportController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("api/[controller]", type.GetCustomAttribute<RouteAttribute>()!.Template);
        var method = type.GetMethod(nameof(InventoryExportController.GetSiesa))!;
        Assert.Equal("siesa", method.GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Equal(new[] { "date", "solutionCenterIds" }, method.GetParameters()
            .Where(parameter => parameter.GetCustomAttribute<FromQueryAttribute>() is not null).Select(parameter => parameter.Name));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _controller.GetSiesa(Day, cancellationToken: cancellation.Token));
        Assert.Empty(_source.Commands);
    }

    private void SeedCounts()
    {
        var before = Row(125, ExecutionA, 2, 10, 1, Start.AddTicks(-10));
        var selected = Row(200, ExecutionA, 2, 10, 2, Start);
        selected.open = 5; selected.closed = 2; selected.multiplication_value = 12;
        var tomorrow = Row(300, ExecutionA, 2, 10, 3, Start.AddDays(1));
        tomorrow.open = 999;
        var otherUnit = Row(205, ExecutionA, 2, 10, 1, Start); otherUnit.unit_of_measure = "UND"; otherUnit.open = 77;
        var zero = Row(201, ExecutionA, 2, 11, 1, Start); zero.open = 0;
        var secondExecution = Row(220, ExecutionB, 2, 13, 1, Start); secondExecution.section_id = 7;
        var warehouse = Row(100, WarehouseExecution, 1, 10, 1, Start); warehouse.count_value = 99999999999999.9999m;
        _source.Records.AddRange(new[] { before, selected, tomorrow, otherUnit, zero,
            Row(202, ExecutionA, 2, 12, 1, Start), Row(203, ExecutionA, 2, 12, 2, Start),
            Row(204, ExecutionA, 2, 12, 3, Start), secondExecution, warehouse });
    }

    private static InventoryRecord Row(long id, string execution, long center, long product, byte count, DateTime time) => new()
    {
        inventory_id = id, inventory_execution_id = execution, solution_center_id = center, solution_center_type_id = center == 1 ? 1 : 2,
        solution_center_code = center == 1 ? "BL02" : "R01", solution_center_name = $"Centro histórico {center}",
        inventory_configuration_id = 3, inventory_configuration_name = "Configuración histórica", section_id = 1, section_name = "Sección histórica",
        product_id = product, reference = "0002361", product_name = "Producto histórico", unit_of_measure = "KG", plan_id = "FIF",
        count_number = count, count_value = center == 1 ? count : null, open = center == 1 ? null : count,
        entered_by = "responsable", created_at = time
    };

    private static InventoryExportParameter Profile() => new()
    {
        export_parameter_id = 1, configuration_code = "SIESA_PDV_0412", configuration_name = "Perfil existente",
        detail_record_type = "0412", detail_record_subtype = "00", detail_record_version = "02", company_code = "009",
        reserved_field = new(' ', 55), auxiliary_location = new(' ', 10), lot_code = new(' ', 15), label_number = "000000000",
        count_1_1 = "00000000000.0000", count_2_1 = "00000000000.0000", count_1_2 = "00000000000.0000",
        count_2_2 = "00000000000.0000", count_3_2 = "00000000000.0000", barcode = new(' ', 20),
        detail_extension_1 = new(' ', 20), detail_extension_2 = new(' ', 20),
        header_record_type = "0000", header_record_subtype = "00", header_record_version = "01",
        footer_record_type = "9999", footer_record_subtype = "00", footer_record_version = "01", is_active = true, created_at = Start
    };

    private static ResponseApi Response(IActionResult result, int status)
    {
        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(status, response.StatusCode);
        return Assert.IsType<ResponseApi>(response.Value);
    }
    private static InventoryExportDto Data(IActionResult result, int status)
    {
        var response = Response(result, status);
        Assert.Equal(status == 200, response.IsSuccess);
        return Assert.IsType<InventoryExportDto>(response.Result);
    }
    private static InventoryExportExecutionDto OnlyExecution(InventoryExportDto data) => Assert.Single(Assert.Single(data.SolutionCenters).Inventories);

    private sealed class ReadOnlySource : DbCommandInterceptor
    {
        public List<InventoryRecord> Records { get; } = new();
        public List<SolutionCenter> Centers { get; } = new();
        public List<InventoryExportParameter> Parameters { get; } = new();
        public List<string> Commands { get; } = new();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT", command.CommandText);
            Commands.Add(command.CommandText);
            var tables = new[]
            {
                Cte(command, "sitracalco_inventory_inventory", Records),
                Cte(command, "sitracalco_inventory_solution_center", Centers),
                Cte(command, "sitracalco_inventory_export_parameter", Parameters)
            };
            command.CommandText = "WITH " + string.Join(", ", tables) + " " + command.CommandText;
            return ValueTask.FromResult(result);
        }

        private static string Cte<T>(DbCommand command, string table, List<T> rows)
        {
            var properties = typeof(T).GetProperties();
            var selects = new List<string>();
            foreach (var row in rows)
            {
                var columns = new List<string>();
                foreach (var property in properties)
                {
                    var value = property.GetValue(row);
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@fixture{command.Parameters.Count}";
                    parameter.Value = property.Name == "inventory_execution_id" ? Guid.Parse((string)value!) : value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                    columns.Add($"{parameter.ParameterName} AS \"{property.Name}\"");
                }
                selects.Add("SELECT " + string.Join(", ", columns));
            }
            if (selects.Count == 0) selects.Add("SELECT " + string.Join(", ", properties.Select(property => $"NULL AS \"{property.Name}\"")) + " WHERE 0");
            return $"\"{table}\" AS (" + string.Join(" UNION ALL ", selects) + ")";
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("La prueba de exportación no admite escrituras.");
    }
}
