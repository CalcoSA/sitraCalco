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

// Pruebas del POST con datos sintéticos en SQLite en memoria, sin conexión a MySQL.
public class CreateConfigurationAssignmentsDiagnosticsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly CommandProbe _probe = new();
    private readonly List<CreateLogDto> _audit = new();
    private TestContext _context = null!;
    private InventoryConfigurationApplication _application = null!;
    private InventoryConfigurationController _controller = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _context = new TestContext(new DbContextOptionsBuilder<SitraCalcoContext>()
            .UseSqlite(_connection).AddInterceptors(_probe).Options);
        await _context.Database.EnsureCreatedAsync();
        _context.InventoryConfigurations.Add(new() { inventory_configuration_id = 1, inventory_configuration_name = "CONFIGURACIÓN DE PRUEBA" });
        _context.SolutionCenterTypes.AddRange(
            new() { solution_center_type_id = 1, solution_center_type_name = "Tipo A" },
            new() { solution_center_type_id = 2, solution_center_type_name = "Tipo B" });
        _context.Sections.AddRange(
            new() { section_id = 1, section_name = "Sección Alfa", solution_center_type_id = 1, is_active = true },
            new() { section_id = 2, section_name = "Sección Beta", solution_center_type_id = 1, is_active = true });
        for (var id = 1; id <= 36; id++)
        {
            _context.SolutionCenters.Add(new() { solution_center_id = id, solution_center_type_id = 1,
                solution_center_code = $"TEST{id:00}", solution_center_name = $"Centro de prueba {id:00}", is_active = true });
            foreach (var sectionId in new long[] { 1, 2 })
                _context.SolutionCenterSections.Add(new() { solution_center_id = id, section_id = sectionId, is_active = true });
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var logs = new Mock<ILogApplication>();
        logs.Setup(x => x.CreateLog(It.IsAny<CreateLogDto>())).Callback<CreateLogDto>(_audit.Add).ReturnsAsync(1);
        _application = new InventoryConfigurationApplication(new InventoryConfigurationRepository(_context), Mock.Of<IPermissionApplication>());
        _controller = new InventoryConfigurationController(_application, logs.Object,
            new CreateInventoryConfigurationDtoValidator(), new CreateInventoryConfigurationAssignmentsDtoValidator(),
            new UpdateInventoryConfigurationAssignmentStatusDtoValidator(), new UpdateInventoryConfigurationDtoValidator(),
            new AddInventoryConfigurationDaysDtoValidator(), NullLogger<InventoryConfigurationController>.Instance)
            .WithIdentity(userLogin: " ejecutor.jwt ", role: "ADMINISTRADOR");
        _probe.Commands.Clear();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task OneDuplicate_ReturnsConfigurationSectionCodeAndRealName()
    {
        await SeedExisting((1, 1, true));
        var center = await _context.SolutionCenters.SingleAsync(x => x.solution_center_id == 1);
        center.solution_center_code = "CAMBIADO";
        center.solution_center_name = "Nombre actualizado";
        await _context.SaveChangesAsync();
        var response = Bad(await _controller.CreateAssignments(1, Request((1, 1))));
        Assert.Equal("No se pudieron guardar las asignaciones. Los siguientes puntos de venta o bodegas ya tienen asignada la configuración CONFIGURACIÓN DE PRUEBA: sección Sección Alfa (ID 1): CAMBIADO - Nombre actualizado. Desmarca estos centros y vuelve a intentarlo.", response.Message);
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task BatchOf36_ReportsEveryConflict_WithoutPartialWritesOrNPlusOne()
    {
        await SeedExisting((1, 1, true), (12, 1, false), (36, 1, true));
        var before = await Snapshot();
        _probe.Commands.Clear();

        var response = Bad(await _controller.CreateAssignments(1, Request(Enumerable.Range(1, 36).Select(id => ((long)id, 1L)).ToArray())));

        Assert.Contains("TEST01 - Centro de prueba 01, TEST12 - Centro de prueba 12, TEST36 - Centro de prueba 36", response.Message);
        Assert.DoesNotContain("TEST02", response.Message);
        Assert.Contains("CONFIGURACIÓN DE PRUEBA", response.Message);
        Assert.Contains("inactivas", response.Message);
        Assert.Equal(5, _probe.Commands.Count);
        Assert.All(_probe.Commands, command =>
        {
            Assert.StartsWith("SELECT", command.Text);
            Assert.True(command.InTransaction);
        });
        Assert.Equal(before, await Snapshot());
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task ConflictsInMultipleSections_IdentifyEachExactPair()
    {
        await SeedExisting((1, 1, true), (2, 2, true));
        var response = Bad(await _controller.CreateAssignments(1, Request((1, 1), (1, 2), (2, 2))));
        Assert.Contains("sección Sección Alfa (ID 1): TEST01 - Centro de prueba 01; sección Sección Beta (ID 2): TEST02 - Centro de prueba 02", response.Message);
        Assert.Equal(2, await _context.InventoryConfigurationAssignments.CountAsync());
    }

    [Fact]
    public async Task ExistingAssignmentsInDifferentSections_DoNotConflictWithRequestedPairs()
    {
        // El producto cartesiano de centros y secciones incluye estas filas,
        // pero ninguna coincide con las parejas solicitadas.
        await SeedExisting((1, 1, true), (2, 2, false));
        var response = Ok(await _controller.CreateAssignments(1, Request((1, 2), (2, 1))));
        Assert.True(response.IsSuccess);
        Assert.Equal(4, await _context.InventoryConfigurationAssignments.CountAsync());
        Assert.False((await _context.InventoryConfigurationAssignments.SingleAsync(x => x.solution_center_id == 2 && x.section_id == 2)).is_active);
    }

    [Fact]
    public async Task InactiveAssignment_RemainsDuplicate_AndExistingReactivationStillWorks()
    {
        await SeedExisting((1, 1, false));
        var response = Bad(await _controller.CreateAssignments(1, Request((1, 1))));
        Assert.Contains("utiliza la opción de reactivación", response.Message);
        Assert.False((await _context.InventoryConfigurationAssignments.AsNoTracking().SingleAsync()).is_active);
        Assert.True((await _application.UpdateAssignmentStatus(1, 1, 1, true)).Value);
        Assert.True((await _context.InventoryConfigurationAssignments.AsNoTracking().SingleAsync()).is_active);
        response = Bad(await _controller.CreateAssignments(1, Request((1, 1))));
        Assert.Contains("ya tienen asignada", response.Message);
        Assert.DoesNotContain("inactivas", response.Message);
        Assert.Single(await _context.InventoryConfigurationAssignments.ToListAsync());
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData("missing", "no están asociadas al centro indicado")]
    [InlineData("inactive", "relaciones centro–sección están inactivas")]
    [InlineData("type", "no corresponden al tipo del centro indicado")]
    public async Task InvalidCenterSectionRelation_HasSpecificMessage_AndPreventsTheWholeBatch(string reason, string expected)
    {
        var relation = await _context.SolutionCenterSections.SingleAsync(x => x.solution_center_id == 2 && x.section_id == 1);
        if (reason == "missing") _context.SolutionCenterSections.Remove(relation);
        if (reason == "inactive") relation.is_active = false;
        if (reason == "type")
            (await _context.SolutionCenters.SingleAsync(x => x.solution_center_id == 2)).solution_center_type_id = 2;
        await _context.SaveChangesAsync();
        // Aunque otra pareja sea duplicada, no disfrazar el problema de relación.
        await SeedExisting((1, 1, true));
        var before = await Snapshot();
        var response = Bad(await _controller.CreateAssignments(1, Request((1, 1), (2, 1), (3, 1))));
        Assert.Contains(expected, response.Message);
        Assert.Contains("TEST02 - Centro de prueba 02 / sección Sección Alfa (ID 1)", response.Message);
        Assert.DoesNotContain("ya tienen asignada", response.Message);
        Assert.Equal(before, await Snapshot());
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData(99, 1, 1, "La configuración de inventario con ID 99 no existe.")]
    [InlineData(1, 99, 1, "No existen los centros de solución con ID: 99.")]
    [InlineData(1, 1, 99, "No existen las secciones con ID: 99.")]
    public async Task MissingEntities_HaveSpecificMessages(long configurationId, long centerId, long sectionId, string expected)
    {
        var response = Bad(await _controller.CreateAssignments(configurationId, Request((centerId, sectionId))));
        var prefix = configurationId == 99 ? "No se pudieron guardar las asignaciones."
            : "No se pudieron guardar las asignaciones de la configuración CONFIGURACIÓN DE PRUEBA.";
        Assert.Equal($"{prefix} {expected}", response.Message);
        Assert.Empty(await _context.InventoryConfigurationAssignments.ToListAsync());
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task DuplicateRequestPair_HasValidationMessage_AndPreservesErrorsArray()
    {
        var request = Request((1, 1), (1, 1));
        var response = Bad(await _controller.CreateAssignments(1, request));
        const string message = "No se pueden enviar combinaciones repetidas de bodega o punto de venta y sección.";
        Assert.Equal(message, response.Message);
        Assert.Equal(new[] { message }, Assert.IsAssignableFrom<IEnumerable<string>>(response.Result));
        Assert.Equal(message, (await _application.CreateAssignments(1, request)).Description);
        Assert.Empty(_probe.Commands);
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task Success_PreservesResponseActiveAssignmentsAndAudit_WithFiveBatchReads()
    {
        // El estado global no era una restricción del POST; no añadirla.
        (await _context.Sections.SingleAsync(x => x.section_id == 1)).is_active = false;
        (await _context.SolutionCenters.SingleAsync(x => x.solution_center_id == 1)).is_active = false;
        await _context.SaveChangesAsync();
        _probe.Commands.Clear();
        var response = Ok(await _controller.CreateAssignments(1, Request((1, 1), (2, 1))));
        Assert.Equal("Asignaciones de la configuración de inventario guardadas correctamente.", response.Message);
        var result = JsonSerializer.SerializeToElement(response.Result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(1, result.GetProperty("inventoryConfigurationId").GetInt64());
        Assert.Equal(2, result.GetProperty("assignmentsCreated").GetInt32());
        Assert.Equal(5, _probe.Commands.Count(command => command.Text.StartsWith("SELECT", StringComparison.Ordinal)));
        Assert.All(_probe.Commands, command => Assert.True(command.InTransaction));
        var saved = await _context.InventoryConfigurationAssignments.AsNoTracking().ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.is_active));
        var audit = Assert.Single(_audit);
        Assert.Equal(("Crear", "ConfiguracionInventarios", "ejecutor.jwt"), (audit.Action, audit.Module, audit.UserName));
        Assert.Equal("Se agregaron 2 asignaciones a la configuración de inventario CONFIGURACIÓN DE PRUEBA.", audit.Description);
    }

    [Fact]
    public async Task UnexpectedDatabaseFailure_Remains500_NotADuplicateMessage()
    {
        _probe.FailRead = true;
        var result = Assert.IsType<ObjectResult>(await _controller.CreateAssignments(1, Request((1, 1))));
        Assert.Equal(500, result.StatusCode);
        var response = Assert.IsType<ResponseApi>(result.Value);
        Assert.DoesNotContain("ya tienen asignada", response.Message);
        Assert.Empty(_audit);
    }

    private async Task SeedExisting(params (long Center, long Section, bool Active)[] rows)
    {
        _context.InventoryConfigurationAssignments.AddRange(rows.Select(row => new InventoryConfigurationAssignment
        {
            inventory_configuration_id = 1, solution_center_id = row.Center, section_id = row.Section, is_active = row.Active
        }));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }
    private async Task<string> Snapshot() => JsonSerializer.Serialize(await _context.InventoryConfigurationAssignments.AsNoTracking()
        .OrderBy(x => x.inventory_configuration_assignment_id).ToListAsync());
    private static CreateInventoryConfigurationAssignmentsDto Request(params (long Center, long Section)[] pairs) => new()
    {
        Assignments = pairs.Select(pair => new CreateInventoryConfigurationAssignmentDto { SolutionCenterId = pair.Center, SectionId = pair.Section }).ToList()
    };
    private static ResponseApi Bad(IActionResult result)
    {
        var response = Assert.IsType<ResponseApi>(Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.False(response.IsSuccess);
        return response;
    }
    private static ResponseApi Ok(IActionResult result) => Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);

    private sealed class CommandProbe : DbCommandInterceptor
    {
        public List<(string Text, bool InTransaction)> Commands { get; } = new();
        public bool FailRead { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (FailRead) throw new InvalidOperationException("Fallo simulado de base de datos.");
            Commands.Add((command.CommandText, command.Transaction is not null));
            return ValueTask.FromResult(result);
        }
    }
    private sealed class TestContext(DbContextOptions<SitraCalcoContext> options) : SitraCalcoContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var included = new[] { typeof(InventoryConfiguration), typeof(InventoryConfigurationAssignment), typeof(SolutionCenter),
                typeof(SolutionCenterType), typeof(Section), typeof(SolutionCenterSection) };
            foreach (var type in modelBuilder.Model.GetEntityTypes().Select(x => x.ClrType).ToList())
                if (!included.Contains(type)) modelBuilder.Ignore(type);
        }
    }
}
