using System.Data.Common;
using System.Reflection;
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Inventory.Test.Controllers;

// Integración aislada en SQLite en memoria: no usa la conexión MySQL de la aplicación.
public class DeleteConfigurationSolutionCenterTests : IAsyncLifetime
{
    private readonly string _connectionString = $"Data Source=configuration-delete-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=10";
    private readonly List<CreateLogDto> _audit = new();
    private readonly CommandProbe _probe = new();
    private readonly CommitFailure _commitFailure = new();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private SqliteConnection _keeper = null!;
    private TestContext _context = null!;
    private InventoryConfigurationController _controller = null!;

    public async Task InitializeAsync()
    {
        _keeper = new SqliteConnection(_connectionString);
        await _keeper.OpenAsync();
        _context = NewContext(_probe, _commitFailure);
        await _context.Database.EnsureCreatedAsync();
        _context.InventoryConfigurations.AddRange(
            new() { inventory_configuration_id = 2, inventory_configuration_name = "Mensual", start_day = 1, end_day = 31 },
            new() { inventory_configuration_id = 3, inventory_configuration_name = "Otra" });
        _context.InventoryConfigurationDays.Add(new() { inventory_configuration_id = 2, day_of_week = "Lunes" });
        _context.SolutionCenterTypes.Add(new() { solution_center_type_id = 1, solution_center_type_name = "Bodega" });
        foreach (var centerId in new long[] { 3, 4 })
            _context.SolutionCenters.Add(new() { solution_center_id = centerId, solution_center_type_id = 1,
                solution_center_code = $"B{centerId}", solution_center_name = $"Centro {centerId}", is_active = true });
        foreach (var sectionId in new long[] { 1, 2, 4, 5 })
        {
            _context.Sections.Add(new() { section_id = sectionId, section_name = $"Sección {sectionId}", solution_center_type_id = 1, is_active = true });
            _context.SolutionCenterSections.Add(new() { solution_center_id = 3, section_id = sectionId, is_active = sectionId != 4 });
        }
        _context.SolutionCenterSections.Add(new() { solution_center_id = 4, section_id = 1, is_active = true });
        _context.Products.Add(new() { product_id = 1, product_name = "Arroz", reference = "AR1", unit_of_measure = "KG" });
        var execution = Guid.NewGuid().ToString();
        foreach (var sectionId in new long[] { 1, 2, 4 })
        {
            _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 2, solution_center_id = 3,
                section_id = sectionId, is_active = sectionId == 2 });
            _context.SolutionCenterProducts.Add(new() { solution_center_id = 3, section_id = sectionId, product_id = 1,
                sort_order = 1, created_by = "original" });
        }
        _context.InventoryConfigurationAssignments.AddRange(
            new() { inventory_configuration_id = 2, solution_center_id = 4, section_id = 1, is_active = true },
            new() { inventory_configuration_id = 3, solution_center_id = 3, section_id = 1, is_active = false });
        for (byte count = 1; count <= 3; count++)
            _context.InventoryRecords.Add(new()
            {
                inventory_execution_id = execution, solution_center_id = 3, solution_center_type_id = 1,
                solution_center_code = "ANT", solution_center_name = "Centro histórico", inventory_configuration_id = 2,
                inventory_configuration_name = "Configuración histórica", section_id = 1, section_name = "Sección histórica",
                product_id = 1, reference = "REF", product_name = "Producto histórico", unit_of_measure = "KG", plan_id = "PLAN",
                count_number = count, count_value = count + 0.25m, multiplication_value = 4.5m,
                entered_by = "responsable", created_at = new DateTime(2026, 1, count)
            });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var logs = new Mock<ILogApplication>();
        logs.Setup(x => x.CreateLog(It.IsAny<CreateLogDto>())).Callback<CreateLogDto>(_audit.Add).ReturnsAsync(1);
        _controller = new InventoryConfigurationController(Application(_context), logs.Object,
            new CreateInventoryConfigurationDtoValidator(), new CreateInventoryConfigurationAssignmentsDtoValidator(),
            new UpdateInventoryConfigurationAssignmentStatusDtoValidator(), new UpdateInventoryConfigurationDtoValidator(),
            new AddInventoryConfigurationDaysDtoValidator(), NullLogger<InventoryConfigurationController>.Instance)
            .WithIdentity(userLogin: "  ejecutor.jwt  ", role: "ADMINISTRADOR");
        _probe.Commands.Clear();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _keeper.DisposeAsync();
    }

    [Fact]
    public async Task Delete_RemovesAllStatesOnlyForPair_PreservesRelatedRowsAndHistory_AndAudits()
    {
        var relatedBefore = await RelatedRows();
        var otherBefore = await OtherAssignments();
        _probe.Commands.Clear();

        var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(
            await _controller.DeleteAssignmentsBySolutionCenter(2, 3)).Value);

        Assert.Equal("""{"isSuccess":true,"message":"Configuración de inventario retirada del centro de solución correctamente.","result":{"inventoryConfigurationId":2,"solutionCenterId":3,"deletedAssignments":3}}""",
            JsonSerializer.Serialize(response, new JsonSerializerOptions(Json) { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        Assert.All(_probe.Commands, command => Assert.True(command.InTransaction));
        var deletion = Assert.Single(_probe.Commands, x => x.Text.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("sitracalco_inventory_configuration_assignment", deletion.Text);
        Assert.Contains("solution_center_section", _probe.Commands.First().Text);
        Assert.Equal(0, await TargetCount());
        Assert.Equal(otherBefore, await OtherAssignments());
        Assert.Equal(relatedBefore, await RelatedRows());
        var audit = Assert.Single(_audit);
        Assert.Equal(("Eliminar", "ConfiguracionInventarios", "ejecutor.jwt"), (audit.Action, audit.Module, audit.UserName));
        Assert.Equal("Se retiró la configuración de inventario Mensual del centro de solución Centro 3 y se eliminaron 3 asignaciones de secciones (activas o inactivas).", audit.Description);

        Assert.IsType<NotFoundObjectResult>(await _controller.DeleteAssignmentsBySolutionCenter(2, 3));
        Assert.Single(_audit);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    [InlineData(2, 0)]
    [InlineData(2, -1)]
    public async Task InvalidIds_Return400_WithoutDatabaseAccess(long configurationId, long centerId)
    {
        Assert.IsType<BadRequestObjectResult>(await _controller.DeleteAssignmentsBySolutionCenter(configurationId, centerId));
        Assert.Equal(0, (await Application(_context).DeleteAssignmentsBySolutionCenter(configurationId, centerId)).Value);
        Assert.Empty(_probe.Commands);
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData(3, 4)] // Ambos existen, pero no tienen asignaciones entre sí.
    [InlineData(999, 3)]
    [InlineData(2, 999)]
    public async Task NoAssignments_Return404_WithoutAuditOrChanges(long configurationId, long centerId)
    {
        var response = Assert.IsType<ResponseApi>(Assert.IsType<NotFoundObjectResult>(
            await _controller.DeleteAssignmentsBySolutionCenter(configurationId, centerId)).Value);
        Assert.False(response.IsSuccess);
        Assert.Equal("No se encontraron asignaciones de la configuración de inventario en el centro de solución indicado.", response.Message);
        Assert.Equal(5, await _context.InventoryConfigurationAssignments.CountAsync());
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task JwtUserRequired_AndNewRouteCoexistsWithSectionDelete()
    {
        _controller.WithIdentity(userLogin: " ");
        Assert.Equal(403, Assert.IsType<ObjectResult>(await _controller.DeleteAssignmentsBySolutionCenter(2, 3)).StatusCode);
        Assert.Empty(_probe.Commands);
        Assert.Empty(_audit);
        var type = typeof(InventoryConfigurationController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("{inventoryConfigurationId:long}/solution-centers/{solutionCenterId:long}",
            type.GetMethod(nameof(InventoryConfigurationController.DeleteAssignmentsBySolutionCenter))!.GetCustomAttribute<HttpDeleteAttribute>()!.Template);
        Assert.Equal("{inventoryConfigurationId:long}/solution-centers/{solutionCenterId:long}/sections/{sectionId:long}",
            type.GetMethod(nameof(InventoryConfigurationController.DeleteAssignment))!.GetCustomAttribute<HttpDeleteAttribute>()!.Template);
    }

    [Fact]
    public async Task CommitFailure_RollsBackEveryDeletedAssignment_AndDoesNotAudit()
    {
        var relatedBefore = await RelatedRows();
        _probe.Commands.Clear();
        _commitFailure.Enabled = true;

        var result = Assert.IsType<ObjectResult>(await _controller.DeleteAssignmentsBySolutionCenter(2, 3));

        Assert.Equal(500, result.StatusCode);
        Assert.False(Assert.IsType<ResponseApi>(result.Value).IsSuccess);
        Assert.Single(_probe.Commands, x => x.Text.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, await TargetCount());
        Assert.Equal(5, await _context.InventoryConfigurationAssignments.CountAsync());
        Assert.Equal(relatedBefore, await RelatedRows());
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData("delete", "create", 3, 1, 1)]
    [InlineData("create", "delete", 1, 4, 0)]
    [InlineData("delete", "reactivate", 3, 0, 0)]
    [InlineData("reactivate", "delete", 1, 3, 0)]
    public async Task ConcurrentCreateAndReactivation_SerializeWithDelete(
        string firstOperation, string secondOperation, int firstResult, int secondResult, int remaining)
    {
        var gate = new AssignmentReadGate();
        var attempt = new TransactionAttempt();
        await using var firstContext = NewContext(gate);
        await using var secondContext = NewContext(attempt);
        var first = Write(firstContext, firstOperation);
        Task<int>? second = null;
        try
        {
            await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(15));
            second = Task.Run(() => Write(secondContext, secondOperation));
            await attempt.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(second.IsCompleted);
        }
        finally
        {
            gate.Release.TrySetResult(true);
            await first.WaitAsync(TimeSpan.FromSeconds(20));
            if (second is not null) await second.WaitAsync(TimeSpan.FromSeconds(20));
        }
        Assert.Equal(firstResult, await first);
        Assert.Equal(secondResult, await second!);
        Assert.Equal(remaining, await TargetCount());
        Assert.Equal(2, await _context.InventoryConfigurationAssignments.CountAsync(x => x.inventory_configuration_id != 2 || x.solution_center_id != 3));
    }

    private static async Task<int> Write(SitraCalcoContext context, string operation)
    {
        var repository = new InventoryConfigurationRepository(context);
        return operation switch
        {
            "delete" => (await repository.DeleteAssignmentsBySolutionCenter(2, 3)).Value,
            "create" => (await repository.CreateAssignments(new[] { new InventoryConfigurationAssignment
                { inventory_configuration_id = 2, solution_center_id = 3, section_id = 5, is_active = true } })).Value,
            _ => (await repository.UpdateAssignmentStatus(2, 3, 1, true)).Value ? 1 : 0
        };
    }

    private Task<int> TargetCount() => _context.InventoryConfigurationAssignments.CountAsync(x => x.inventory_configuration_id == 2 && x.solution_center_id == 3);
    private TestContext NewContext(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<SitraCalcoContext>()
        .UseSqlite(_connectionString).AddInterceptors(interceptors).Options);
    private static InventoryConfigurationApplication Application(SitraCalcoContext context) => new(
        new InventoryConfigurationRepository(context), Mock.Of<IPermissionApplication>());
    private async Task<string> OtherAssignments() => JsonSerializer.Serialize(await _context.InventoryConfigurationAssignments.AsNoTracking()
        .Where(x => x.inventory_configuration_id != 2 || x.solution_center_id != 3).OrderBy(x => x.inventory_configuration_assignment_id).ToListAsync());
    private async Task<string> RelatedRows() => JsonSerializer.Serialize(new
    {
        Configurations = await _context.InventoryConfigurations.AsNoTracking().OrderBy(x => x.inventory_configuration_id).ToListAsync(),
        Days = await _context.InventoryConfigurationDays.AsNoTracking().OrderBy(x => x.inventory_configuration_day_id).ToListAsync(),
        Centers = await _context.SolutionCenters.AsNoTracking().OrderBy(x => x.solution_center_id).ToListAsync(),
        Sections = await _context.Sections.AsNoTracking().OrderBy(x => x.section_id).ToListAsync(),
        Products = await _context.Products.AsNoTracking().OrderBy(x => x.product_id).ToListAsync(),
        CenterSections = await _context.SolutionCenterSections.AsNoTracking().OrderBy(x => x.solution_center_section_id).ToListAsync(),
        CenterProducts = await _context.SolutionCenterProducts.AsNoTracking().OrderBy(x => x.solution_center_product_id).ToListAsync(),
        History = await _context.InventoryRecords.AsNoTracking().OrderBy(x => x.inventory_id).ToListAsync()
    });

    private sealed class CommandProbe : DbCommandInterceptor
    {
        public List<(string Text, bool InTransaction)> Commands { get; } = new();
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add((command.CommandText, command.Transaction is not null));
            return ValueTask.FromResult(result);
        }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Commands.Add((command.CommandText, command.Transaction is not null));
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CommitFailure : DbTransactionInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Enabled) throw new InvalidOperationException("Fallo simulado antes del commit.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class AssignmentReadGate : DbCommandInterceptor
    {
        public TaskCompletionSource<bool> Locked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.Transaction is not null && command.CommandText.Contains("FROM \"sitracalco_inventory_solution_center_section\"", StringComparison.Ordinal) &&
                Locked.TrySetResult(true))
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            return result;
        }
    }

    private sealed class TransactionAttempt : DbTransactionInterceptor
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            return ValueTask.FromResult(result);
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
