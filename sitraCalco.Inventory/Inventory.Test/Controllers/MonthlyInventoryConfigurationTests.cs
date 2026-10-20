using System.Data.Common;
using System.Text.Json;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Dtos;
using Inventory.Domain.Models;
using Inventory.Domain.Responses;
using Inventory.Domain.Validators;
using Inventory.Infrastructure.Persistance.Data;
using Inventory.Infrastructure.Persistance.Repositories;
using Inventory.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Inventory.Test.Controllers;

// Integración Controller/Application/Repository con SQLite en memoria, sin acceder a MySQL.
public class MonthlyInventoryConfigurationTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Mock<IPermissionApplication> _permissions = new();
    private readonly Mock<ILogApplication> _logs = new();
    private readonly List<CreateLogDto> _audit = new();
    private readonly CommandProbe _commands = new();
    private readonly LocalClock _clock = new();
    private TestContext _context = null!;
    private InventoryConfigurationApplication _application = null!;
    private InventoryConfigurationController _controller = null!;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _context = new TestContext(new DbContextOptionsBuilder<SitraCalcoContext>()
            .UseSqlite(_connection).AddInterceptors(_commands).Options);
        await _context.Database.EnsureCreatedAsync();
        _context.InventoryConfigurations.AddRange(
            new() { inventory_configuration_id = 1, inventory_configuration_name = "Mensual", start_day = 15, end_day = 30 },
            new() { inventory_configuration_id = 2, inventory_configuration_name = "Sin asociaciones", start_day = 1, end_day = 31 });
        _context.SolutionCenterTypes.Add(new() { solution_center_type_id = 1, solution_center_type_name = "Bodega" });
        _context.SolutionCenters.AddRange(
            new() { solution_center_id = 1, solution_center_type_id = 1, solution_center_code = "B1", solution_center_name = "Poblado", is_active = true },
            new() { solution_center_id = 2, solution_center_type_id = 1, solution_center_code = "B2", solution_center_name = "Unicentro", is_active = true });
        _context.Sections.Add(new() { section_id = 1, section_name = "Pasillo 1", solution_center_type_id = 1, is_active = true });
        _context.SolutionCenterSections.Add(new() { solution_center_id = 1, section_id = 1, is_active = true });
        _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 1, solution_center_id = 1, section_id = 1, is_active = true });
        _context.Products.Add(new() { product_id = 1, product_name = "Arroz", reference = "AR1", unit_of_measure = "KG" });
        _context.SolutionCenterProducts.Add(new() { solution_center_id = 1, section_id = 1, product_id = 1, sort_order = 1, created_by = "original" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _permissions.Setup(x => x.HasPermission(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _logs.Setup(x => x.CreateLog(It.IsAny<CreateLogDto>())).Callback<CreateLogDto>(_audit.Add).ReturnsAsync(1);
        _application = new InventoryConfigurationApplication(new InventoryConfigurationRepository(_context), _permissions.Object, timeProvider: _clock);
        _controller = new InventoryConfigurationController(_application, _logs.Object,
            new CreateInventoryConfigurationDtoValidator(), new CreateInventoryConfigurationAssignmentsDtoValidator(),
            new UpdateInventoryConfigurationAssignmentStatusDtoValidator(), new UpdateInventoryConfigurationDtoValidator(),
            new AddInventoryConfigurationDaysDtoValidator(), NullLogger<InventoryConfigurationController>.Instance)
            .WithIdentity(userLogin: " ejecutor.jwt ", role: "ADMINISTRADOR");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Post_PersistsMonthlyRangeWithoutWeekdays_AndAuditsJwt()
    {
        var response = Success(await _controller.Create(Read<CreateInventoryConfigurationDto>(
            """{"inventoryConfigurationName":" Uno a uno ","startDay":15,"endDay":30}""")));
        var id = JsonSerializer.SerializeToElement(response.Result, Json).GetProperty("inventoryConfigurationId").GetInt64();
        var saved = await _context.InventoryConfigurations.AsNoTracking().SingleAsync(x => x.inventory_configuration_id == id);
        Assert.Equal(("Uno a uno", 15, 30), (saved.inventory_configuration_name, saved.start_day, saved.end_day));
        Assert.Empty(await _context.InventoryConfigurationDays.ToListAsync());
        Assert.Empty(await _context.InventoryConfigurationAssignments.Where(x => x.inventory_configuration_id == id).ToListAsync());
        var audit = Assert.Single(_audit);
        Assert.Equal("ejecutor.jwt", audit.UserName);
        Assert.Contains("Día inicial mensual: 15. Día final mensual: 30.", audit.Description);
        Assert.Equal("ConfiguracionInventarios", audit.Module);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(15, 0)]
    [InlineData(-1, 30)]
    [InlineData(1, 32)]
    [InlineData(32, 32)]
    [InlineData(25, 5)]
    public async Task Post_InvalidRange_DoesNotPersist(int start, int end)
    {
        Assert.IsType<BadRequestObjectResult>(await _controller.Create(new()
        {
            InventoryConfigurationName = "Inválida", StartDay = start, EndDay = end
        }));
        Assert.Equal(2, await _context.InventoryConfigurations.CountAsync());
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData("{\"inventoryConfigurationName\":\"Nueva\"}")]
    [InlineData("{\"inventoryConfigurationName\":\"Nueva\",\"startDay\":15}")]
    [InlineData("{\"inventoryConfigurationName\":\"Nueva\",\"endDay\":30}")]
    public async Task Post_MissingDays_AreInvalid(string json) =>
        Assert.IsType<BadRequestObjectResult>(await _controller.Create(Read<CreateInventoryConfigurationDto>(json)));

    [Theory]
    [InlineData("{\"startDate\":\"2026-01-01\"}")]
    [InlineData("{\"endDate\":\"2026-01-31\"}")]
    [InlineData("{\"startDay\":\"2026-01-01\"}")]
    [InlineData("{\"endDay\":\"2026-01-31\"}")]
    public void Json_RejectsOldPropertiesAndFullDates(string json)
    {
        Assert.Throws<JsonException>(() => Read<CreateInventoryConfigurationDto>(json));
        Assert.Throws<JsonException>(() => Read<UpdateInventoryConfigurationDto>(json));
    }

    [Theory]
    [InlineData("{\"endDay\":31}", "Mensual", 15, 31)]
    [InlineData("{\"startDay\":20}", "Mensual", 20, 30)]
    [InlineData("{\"inventoryConfigurationName\":\" Nuevo nombre \"}", "Nuevo nombre", 15, 30)]
    [InlineData("{\"startDay\":1,\"endDay\":10}", "Mensual", 1, 10)]
    [InlineData("{}", "Mensual", 15, 30)]
    public async Task Patch_MergesOnlySuppliedFields_AndReturnsAndAuditsPersistedValues(string json, string name, int start, int end)
    {
        var result = Assert.IsType<InventoryConfigurationDto>(Success(await _controller.UpdateInventoryConfiguration(1,
            Read<UpdateInventoryConfigurationDto>(json))).Result);
        var saved = await _context.InventoryConfigurations.AsNoTracking().SingleAsync(x => x.inventory_configuration_id == 1);
        Assert.Equal((name, start, end), (saved.inventory_configuration_name, saved.start_day, saved.end_day));
        Assert.Equal((name, start, end), (result.InventoryConfigurationName, result.StartDay, result.EndDay));
        Assert.Equal($"Se actualizó la configuración de inventario {name}. Día inicial mensual: {start}. Día final mensual: {end}.", Assert.Single(_audit).Description);
    }

    [Theory]
    [InlineData("{\"startDay\":31}")]
    [InlineData("{\"endDay\":14}")]
    [InlineData("{\"startDay\":0}")]
    [InlineData("{\"endDay\":32}")]
    [InlineData("{\"startDay\":null}")]
    [InlineData("{\"endDay\":null}")]
    [InlineData("{\"inventoryConfigurationName\":null}")]
    [InlineData("{\"inventoryConfigurationName\":\" \"}")]
    [InlineData("{\"inventoryConfigurationName\":\"Sin asociaciones\"}")]
    public async Task Patch_InvalidMergedRangeOrName_DoesNotWriteOrAudit(string json)
    {
        Assert.IsType<BadRequestObjectResult>(await _controller.UpdateInventoryConfiguration(1, Read<UpdateInventoryConfigurationDto>(json)));
        var saved = await _context.InventoryConfigurations.AsNoTracking().SingleAsync(x => x.inventory_configuration_id == 1);
        Assert.Equal(("Mensual", 15, 30), (saved.inventory_configuration_name, saved.start_day, saved.end_day));
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task Patch_WriteFailure_DoesNotChangeRangeOrAudit()
    {
        _commands.FailUpdate = true;
        var response = Assert.IsType<ObjectResult>(await _controller.UpdateInventoryConfiguration(1, new() { StartDay = 1, EndDay = 31 }));
        Assert.Equal(500, response.StatusCode);
        var saved = await _context.InventoryConfigurations.AsNoTracking().SingleAsync(x => x.inventory_configuration_id == 1);
        Assert.Equal((15, 30), (saved.start_day, saved.end_day));
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task Patch_ConcurrentChange_CannotOverwriteFieldsOrPersistAnInvalidMergedRange()
    {
        _commands.BeforeUpdate = async () =>
        {
            // Simular otro escritor después de leer 15–30 y antes de guardar startDay=25.
            using var command = _connection.CreateCommand();
            command.CommandText = "UPDATE sitracalco_inventory_configuration SET end_day = 20 WHERE inventory_configuration_id = 1";
            await command.ExecuteNonQueryAsync();
        };
        Assert.IsType<BadRequestObjectResult>(await _controller.UpdateInventoryConfiguration(1, new() { StartDay = 25 }));
        var saved = await _context.InventoryConfigurations.AsNoTracking().SingleAsync(x => x.inventory_configuration_id == 1);
        Assert.Equal((15, 20), (saved.start_day, saved.end_day));
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task AdministrativeGets_IncludeUnassignedConfigurationsAndRealWeekdayIds_WithBoundedQueries()
    {
        await AddDays();
        _commands.Reads = 0;
        var page = Assert.IsType<PagedDto<InventoryConfigurationListDto>>(Success(await _controller.GetInventoryConfigurations(1, 20)).Result);
        Assert.Equal(3, _commands.Reads); // COUNT + página + días de toda la página.
        Assert.Equal((2, 1, 20, 1), (page.Total, page.Page, page.Take, page.Pages));
        Assert.Equal(new long[] { 1, 2 }, page.Items.Select(x => x.InventoryConfigurationId));
        Assert.Equal(new long[] { 9, 10 }, page.Items.First().DaysOfWeek.Select(x => x.InventoryConfigurationDayId));
        Assert.Empty(page.Items.Last().DaysOfWeek);
        var second = Assert.IsType<PagedDto<InventoryConfigurationListDto>>(Success(await _controller.GetInventoryConfigurations(2, 1)).Result);
        Assert.Equal(2, Assert.Single(second.Items).InventoryConfigurationId);
        var beyond = Assert.IsType<PagedDto<InventoryConfigurationListDto>>(Success(await _controller.GetInventoryConfigurations(3, 1)).Result);
        Assert.Empty(beyond.Items);
        Assert.Equal(2, beyond.Total);

        var detail = Assert.IsType<InventoryConfigurationByIdDto>(Success(await _controller.GetInventoryConfigurationById(1)).Result);
        Assert.Equal((15, 30), (detail.StartDay, detail.EndDay));
        Assert.Equal(new[] { "Lunes", "Domingo" }, detail.Days);
        Assert.Equal(detail.Days, detail.DaysOfWeek.Select(x => x.DayOfWeek));
        Assert.Equal(new long[] { 9, 10 }, detail.DaysOfWeek.Select(x => x.InventoryConfigurationDayId));
        Assert.Single(Assert.Single(detail.SolutionCenters).Sections);
        var unassigned = Assert.IsType<InventoryConfigurationByIdDto>(Success(await _controller.GetInventoryConfigurationById(2)).Result);
        Assert.Empty(unassigned.DaysOfWeek);
        Assert.Empty(unassigned.SolutionCenters);
        var byCenter = await _application.GetInventoryConfigurationsBySolutionCenterId(1, "ADMINISTRADOR");
        var configuration = Assert.Single(byCenter.Data!.Configurations);
        Assert.Equal((15, 30), (configuration.StartDay, configuration.EndDay));
        Assert.Equal(detail.Days, configuration.Days);
        Assert.Equal(new long[] { 9, 10 }, configuration.DaysOfWeek.Select(x => x.InventoryConfigurationDayId));
        var serialized = JsonSerializer.Serialize(detail, Json);
        Assert.DoesNotContain("startDate", serialized);
        Assert.DoesNotContain("endDate", serialized);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public async Task List_RejectsInvalidPagination(int page, int take) =>
        Assert.IsType<BadRequestObjectResult>(await _controller.GetInventoryConfigurations(page, take));

    [Fact]
    public async Task List_RespectsVisibilityPermissionAndJwtRole()
    {
        _permissions.Setup(x => x.HasPermission(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        _commands.Reads = 0;
        Assert.Equal(403, Assert.IsType<ObjectResult>(await _controller.GetInventoryConfigurations()).StatusCode);
        Assert.Equal(0, _commands.Reads);
        _controller.WithIdentity(role: "");
        Assert.Equal(403, Assert.IsType<ObjectResult>(await _controller.GetInventoryConfigurations()).StatusCode);
        _controller.WithIdentity(role: "ALMACEN");
        _permissions.Setup(x => x.HasPermission(Inventory.Application.Constants.PermissionKeys.ViewWarehousesOnly, "ALMACEN")).ReturnsAsync(true);
        Assert.IsType<OkObjectResult>(await _controller.GetInventoryConfigurations());
    }

    [Theory]
    [InlineData("2026-02-14", 15, 30, false)]
    [InlineData("2026-02-15", 15, 30, true)]
    [InlineData("2026-02-28", 15, 30, true)]
    [InlineData("2028-02-29", 15, 30, true)]
    [InlineData("2026-04-30", 15, 31, true)]
    [InlineData("2026-01-31", 15, 30, false)]
    [InlineData("2026-03-15", 15, 30, true)]
    [InlineData("2026-02-28", 29, 31, false)]
    [InlineData("2028-02-29", 29, 31, true)]
    [InlineData("2026-04-30", 31, 31, false)]
    [InlineData("2026-01-31", 31, 31, true)]
    [InlineData("2026-02-01", 1, 31, true)]
    [InlineData("2026-02-28", 1, 31, true)]
    [InlineData("2028-02-29", 1, 31, true)]
    [InlineData("2026-04-30", 1, 31, true)]
    [InlineData("2026-01-31", 1, 31, true)]
    public async Task Availability_UsesRecurringInclusiveMonthlyRangeAcrossAllConsumers(string date, int start, int end, bool available)
    {
        await _context.InventoryConfigurations.Where(x => x.inventory_configuration_id == 1)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.start_day, start).SetProperty(x => x.end_day, end));
        _clock.SetLocalDate(date);
        var configurations = await _application.GetAvailableInventoryConfigurations(1, "ADMINISTRADOR");
        Assert.Equal(available ? 1 : 0, configurations.Data!.Count());
        var sections = await _application.GetAvailableInventorySections(1, 1, "ADMINISTRADOR");
        Assert.Equal(available, sections.IsAvailable);
        Assert.Equal(available ? 1 : 0, sections.Data.Count());
        var products = await _application.GetAvailableInventoryProducts(1, 1, 1, "ADMINISTRADOR", 1, 20);
        Assert.Equal(available, products.IsAvailable);
        Assert.Equal(available ? 1 : 0, products.Data.Items.Count());
    }

    [Theory]
    [InlineData("2026-02-15", "Domingo", true)]
    [InlineData("2026-02-15", "Lunes", false)]
    [InlineData("2026-02-14", "Sabado", false)]
    [InlineData("2026-03-15", "Domingo", true)]
    public async Task Availability_CombinesWeekdayWithMonthlyWindow(string date, string weekday, bool available)
    {
        await _application.AddDays(1, new() { Days = new() { weekday } });
        _clock.SetLocalDate(date);
        Assert.Equal(available ? 1 : 0, (await _application.GetAvailableInventoryConfigurations(1, "ADMINISTRADOR")).Data!.Count());
    }

    [Fact]
    public async Task Availability_UsesServerLocalDateInsteadOfUtcDate()
    {
        _clock.SetUtcNow(new DateTimeOffset(2026, 2, 15, 2, 0, 0, TimeSpan.Zero)); // Aún es 14 local.
        Assert.Empty((await _application.GetAvailableInventoryConfigurations(1, "ADMINISTRADOR")).Data!);
        _clock.SetUtcNow(new DateTimeOffset(2026, 2, 15, 5, 0, 0, TimeSpan.Zero)); // Comienza el 15 local.
        Assert.Single((await _application.GetAvailableInventoryConfigurations(1, "ADMINISTRADOR")).Data!);
    }

    [Theory]
    [InlineData("center")]
    [InlineData("sectionAssociation")]
    [InlineData("configurationAssignment")]
    public async Task Availability_PreservesActiveAssignmentFiltersAndCenterIsolation(string inactive)
    {
        if (inactive == "center")
            await _context.SolutionCenters.Where(x => x.solution_center_id == 1).ExecuteUpdateAsync(s => s.SetProperty(x => x.is_active, false));
        if (inactive == "sectionAssociation")
            await _context.SolutionCenterSections.ExecuteUpdateAsync(s => s.SetProperty(x => x.is_active, false));
        if (inactive == "configurationAssignment")
            await _context.InventoryConfigurationAssignments.ExecuteUpdateAsync(s => s.SetProperty(x => x.is_active, false));
        Assert.Empty((await _application.GetAvailableInventoryConfigurations(1, "ADMINISTRADOR")).Data!);
        Assert.Empty((await _application.GetAvailableInventoryConfigurations(2, "ADMINISTRADOR")).Data!);
        Assert.Equal(2, (await _application.GetInventoryConfigurations("ADMINISTRADOR", 1, 20))!.Total);
    }

    [Fact]
    public async Task Inventory_CountsFollowCalendarButHistoricalReadsRemainAvailable()
    {
        var inventory = new InventoryApplication(new InventoryRepository(_context), _application, _permissions.Object, _logs.Object);
        var request = new CreateInventoryDto
        {
            SolutionCenterId = 1, InventoryConfigurationId = 1, SectionId = 1, CountNumber = 1, EnteredBy = "Operario",
            Items = new() { new() { ProductId = 1, CountValue = 4, MultiplicationValue = 1 } }
        };
        var first = await inventory.Create(request, "ejecutor.jwt", "ADMINISTRADOR");
        Assert.Equal(InventorySaveStatus.Success, first.Status);
        request.InventoryExecutionId = first.Data!.InventoryExecutionId;
        _clock.SetLocalDate("2026-03-14");
        foreach (var count in new[] { 2, 3 })
        {
            request.CountNumber = count;
            Assert.Equal(InventorySaveStatus.InvalidContext, (await inventory.Create(request, "ejecutor.jwt", "ADMINISTRADOR")).Status);
        }
        var historical = await inventory.GetCounts(request.InventoryExecutionId, null, "ADMINISTRADOR");
        Assert.Equal(InventoryCountsStatus.Success, historical.Status);
        Assert.Single(historical.Data!.Items);
        Assert.Equal(1, await _context.InventoryRecords.CountAsync());
        _clock.SetLocalDate("2026-03-15");
        foreach (var count in new[] { 2, 3 })
        {
            request.CountNumber = count;
            Assert.Equal(InventorySaveStatus.Success, (await inventory.Create(request, "ejecutor.jwt", "ADMINISTRADOR")).Status);
        }
        Assert.Equal(3, await _context.InventoryRecords.CountAsync());
        Assert.Equal(3, (await inventory.GetCounts(request.InventoryExecutionId, null, "ADMINISTRADOR")).Data!.Items.Single().Counts.Count);
        Assert.Equal(4, (await _context.InventoryRecords.AsNoTracking().SingleAsync(x => x.count_number == 1)).count_value);
    }

    private async Task AddDays()
    {
        _context.InventoryConfigurationDays.AddRange(
            new() { inventory_configuration_day_id = 10, inventory_configuration_id = 1, day_of_week = "Domingo" },
            new() { inventory_configuration_day_id = 9, inventory_configuration_id = 1, day_of_week = "Lunes" });
        await _context.SaveChangesAsync();
    }

    private static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Json)!;
    private static ResponseApi Success(IActionResult result) => Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);

    private sealed class LocalClock : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 2, 15, 5, 1, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone { get; } = TimeZoneInfo.CreateCustomTimeZone("Test local", TimeSpan.FromHours(-5), "Test local", "Test local");
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
        public void SetLocalDate(string date) => _utcNow = new DateTimeOffset(DateTime.ParseExact(date, "yyyy-MM-dd", global::System.Globalization.CultureInfo.InvariantCulture).AddHours(5).AddMinutes(1), TimeSpan.Zero);
    }

    private sealed class CommandProbe : DbCommandInterceptor
    {
        public int Reads { get; set; }
        public bool FailUpdate { get; set; }
        public Func<Task>? BeforeUpdate { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(result);
        }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (FailUpdate && command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal))
                throw new InvalidOperationException("Fallo de escritura simulado.");
            if (BeforeUpdate is { } beforeUpdate && command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal))
            {
                BeforeUpdate = null;
                await beforeUpdate();
            }
            return result;
        }
    }

    private sealed class TestContext(DbContextOptions<SitraCalcoContext> options) : SitraCalcoContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var included = new[] { typeof(InventoryConfiguration), typeof(InventoryConfigurationDay), typeof(InventoryConfigurationAssignment),
                typeof(SolutionCenter), typeof(SolutionCenterType), typeof(Section), typeof(Product), typeof(SolutionCenterSection),
                typeof(SolutionCenterProduct), typeof(InventoryRecord) };
            foreach (var type in modelBuilder.Model.GetEntityTypes().Select(x => x.ClrType).ToList())
                if (!included.Contains(type)) modelBuilder.Ignore(type);
            modelBuilder.Entity<InventoryConfigurationDay>().Property(x => x.day_of_week).HasColumnType("TEXT");
            modelBuilder.Entity<InventoryRecord>().Property(x => x.created_at).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
