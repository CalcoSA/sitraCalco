using System.Data.Common;
using System.Text.Json;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Dtos;
using Inventory.Domain.Models;
using Inventory.Domain.Responses;
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

// Todas las operaciones usan una base exclusiva en memoria; nunca la conexión de la aplicación.
public class DeleteSectionAssignmentTests : IAsyncLifetime
{
    private readonly string _connectionString = $"Data Source=section-delete-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=10";
    private SqliteConnection _keeper = null!;
    private TestContext _context = null!;
    private readonly List<CreateLogDto> _audit = new();
    private readonly CommandProbe _probe = new();
    private readonly CommitFailure _commitFailure = new();
    private SolutionCenterController _controller = null!;

    public async Task InitializeAsync()
    {
        _keeper = new SqliteConnection(_connectionString);
        await _keeper.OpenAsync();
        _context = NewContext(_probe, _commitFailure);
        await _context.Database.EnsureCreatedAsync();
        _context.SolutionCenterTypes.Add(new() { solution_center_type_id = 1, solution_center_type_name = "Bodega" });
        _context.SolutionCenters.AddRange(
            new() { solution_center_id = 1, solution_center_type_id = 1, solution_center_code = "PO", solution_center_name = "Poblado", is_active = true },
            new() { solution_center_id = 2, solution_center_type_id = 1, solution_center_code = "UN", solution_center_name = "Unicentro", is_active = true });
        _context.Sections.AddRange(
            new() { section_id = 1, section_name = "Pasillo 1", solution_center_type_id = 1, is_active = true },
            new() { section_id = 2, section_name = "Pasillo 2", solution_center_type_id = 1, is_active = true });
        _context.Products.AddRange(
            new() { product_id = 1, product_name = "Arroz", reference = "AR", unit_of_measure = "KG" },
            new() { product_id = 2, product_name = "Sal", reference = "SA", unit_of_measure = "KG" },
            new() { product_id = 3, product_name = "Café", reference = "CA", unit_of_measure = "UND" });
        _context.InventoryConfigurations.Add(new() { inventory_configuration_id = 1, inventory_configuration_name = "Semanal" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var logs = new Mock<ILogApplication>();
        logs.Setup(x => x.CreateLog(It.IsAny<CreateLogDto>())).Callback<CreateLogDto>(_audit.Add).ReturnsAsync(1L);
        _controller = new SolutionCenterController(Application(_context), logs.Object,
            NullLogger<SolutionCenterController>.Instance).WithIdentity(userLogin: "  ejecutor.jwt  ");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _keeper.DisposeAsync();
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 3)]
    [InlineData(false, 3)]
    public async Task Delete_RemovesOnlyTheRequestedPair_AndAudits(bool active, int productCount)
    {
        await SeedAssignment(1, 1, active, Enumerable.Range(1, productCount).Select(id => ((long)id, id)).ToArray());
        await SeedAssignment(1, 2, true, (1, 8));
        await SeedAssignment(2, 1, false, (2, 5), (3, 9));
        var sections = JsonSerializer.Serialize(await _context.Sections.AsNoTracking().OrderBy(x => x.section_id).ToListAsync());
        var products = JsonSerializer.Serialize(await _context.Products.AsNoTracking().OrderBy(x => x.product_id).ToListAsync());
        var otherAssignments = await OtherRows();
        _probe.Commands.Clear();

        var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(await _controller.DeleteSectionAssignment(1, 1)).Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Sección retirada del centro de solución correctamente.", response.Message);
        var result = JsonSerializer.SerializeToElement(response.Result);
        Assert.Equal(productCount, result.GetProperty("DeletedProducts").GetInt32());
        Assert.Equal(1, result.GetProperty("SolutionCenterId").GetInt64());
        Assert.Equal(1, result.GetProperty("SectionId").GetInt64());
        Assert.All(_probe.Commands, command => Assert.True(command.InTransaction));
        var deletes = _probe.Commands.Where(x => x.Text.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(2, deletes.Count);
        Assert.Contains("solution_center_product", deletes[0].Text);
        Assert.Contains("solution_center_section", deletes[1].Text);
        Assert.False(await _context.SolutionCenterSections.AnyAsync(x => x.solution_center_id == 1 && x.section_id == 1));
        Assert.False(await _context.SolutionCenterProducts.AnyAsync(x => x.solution_center_id == 1 && x.section_id == 1));
        Assert.Equal(otherAssignments, await OtherRows());
        Assert.Equal(sections, JsonSerializer.Serialize(await _context.Sections.AsNoTracking().OrderBy(x => x.section_id).ToListAsync()));
        Assert.Equal(products, JsonSerializer.Serialize(await _context.Products.AsNoTracking().OrderBy(x => x.product_id).ToListAsync()));
        var audit = Assert.Single(_audit);
        Assert.Equal("Eliminar", audit.Action);
        Assert.Equal("ConfiguracionBodegas", audit.Module);
        Assert.Equal("ejecutor.jwt", audit.UserName);
        Assert.Equal($"Se retiró la sección Pasillo 1 del centro Poblado y se eliminaron {productCount} asignaciones de productos.", audit.Description);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Configurations_ActiveOrInactive_BlockWithoutChanges(bool active)
    {
        await SeedAssignment(1, 1, true, (1, 1), (2, 2));
        _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 1, solution_center_id = 1, section_id = 1, is_active = active });
        await _context.SaveChangesAsync();

        var response = Assert.IsType<ResponseApi>(Assert.IsType<ConflictObjectResult>(await _controller.DeleteSectionAssignment(1, 1)).Value);

        Assert.False(response.IsSuccess);
        Assert.Contains("activas o inactivas", response.Message);
        Assert.Equal(2, await _context.SolutionCenterProducts.CountAsync());
        Assert.Single(await _context.SolutionCenterSections.ToListAsync());
        Assert.Equal(active, (await _context.InventoryConfigurationAssignments.AsNoTracking().SingleAsync()).is_active);
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task ConfigurationsOfOtherPairs_DoNotBlock_AndRemainIntact()
    {
        await SeedAssignment(1, 1, true, (1, 1));
        await SeedAssignment(1, 2, true);
        await SeedAssignment(2, 1, true);
        _context.InventoryConfigurationAssignments.AddRange(
            new() { inventory_configuration_id = 1, solution_center_id = 1, section_id = 2, is_active = true },
            new() { inventory_configuration_id = 1, solution_center_id = 2, section_id = 1, is_active = false });
        await _context.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await _context.InventoryConfigurationAssignments.AsNoTracking().ToListAsync());

        Assert.IsType<OkObjectResult>(await _controller.DeleteSectionAssignment(1, 1));

        Assert.Equal(before, JsonSerializer.Serialize(await _context.InventoryConfigurationAssignments.AsNoTracking().ToListAsync()));
    }

    [Fact]
    public async Task HistoricalCounts_RemainIdenticalAndQueryable()
    {
        await SeedAssignment(1, 1, true, (1, 1));
        var execution = Guid.NewGuid().ToString();
        for (byte count = 1; count <= 3; count++)
            _context.InventoryRecords.Add(new()
            {
                inventory_execution_id = execution, solution_center_id = 1, solution_center_type_id = 1,
                solution_center_code = "ANT", solution_center_name = "Nombre histórico", inventory_configuration_id = 1,
                inventory_configuration_name = "Configuración histórica", section_id = 1, section_name = "Sección histórica",
                product_id = 1, reference = "REF", product_name = "Producto histórico", unit_of_measure = "KG", plan_id = "PLAN",
                count_number = count, count_value = count + 0.25m, multiplication_value = 4.5m,
                entered_by = "responsable", created_at = new DateTime(2026, 1, count)
            });
        await _context.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await _context.InventoryRecords.AsNoTracking().OrderBy(x => x.inventory_id).ToListAsync());
        var inventory = new InventoryRepository(_context);
        var counts = JsonSerializer.Serialize(await inventory.GetCounts(execution, null));

        Assert.IsType<OkObjectResult>(await _controller.DeleteSectionAssignment(1, 1));

        Assert.Equal(before, JsonSerializer.Serialize(await _context.InventoryRecords.AsNoTracking().OrderBy(x => x.inventory_id).ToListAsync()));
        Assert.Equal(counts, JsonSerializer.Serialize(await inventory.GetCounts(execution, null)));
        Assert.Equal("Sección histórica", (await inventory.GetCountsContext(execution))!.SectionName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reassign_StartsFreshThroughEitherExistingEndpoint(bool withProducts)
    {
        await SeedAssignment(1, 1, false, (1, 8), (2, 9));
        var oldId = (await _context.SolutionCenterSections.SingleAsync()).solution_center_section_id;
        Assert.IsType<OkObjectResult>(await _controller.DeleteSectionAssignment(1, 1));

        var response = withProducts
            ? await _controller.CreateSectionConfiguration(1, new() { IdSection = 1, Products = new() { new() { ProductId = 3, SortOrder = 1 } } })
            : await _controller.AssignExistingSection(1, 1);

        Assert.IsType<OkObjectResult>(response);
        var assignment = await _context.SolutionCenterSections.AsNoTracking().SingleAsync();
        Assert.NotEqual(oldId, assignment.solution_center_section_id);
        Assert.True(assignment.is_active);
        var products = await _context.SolutionCenterProducts.AsNoTracking().ToListAsync();
        if (withProducts)
            Assert.Equal((3L, 1), (Assert.Single(products).product_id, products[0].sort_order));
        else
            Assert.Empty(products);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistenceFailure_RollsBackProductsAndAssignment(bool failAtCommit)
    {
        await SeedAssignment(1, 1, true, (1, 1), (2, 2));
        var before = JsonSerializer.Serialize(await _context.SolutionCenterProducts.AsNoTracking().ToListAsync());
        _probe.FailSectionDelete = !failAtCommit;
        _commitFailure.Enabled = failAtCommit;

        var response = Assert.IsType<ObjectResult>(await _controller.DeleteSectionAssignment(1, 1));

        Assert.Equal(500, response.StatusCode);
        Assert.Equal(before, JsonSerializer.Serialize(await _context.SolutionCenterProducts.AsNoTracking().ToListAsync()));
        Assert.Single(await _context.SolutionCenterSections.AsNoTracking().ToListAsync());
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData(0, 1, 400)]
    [InlineData(1, 0, 400)]
    [InlineData(-1, 1, 400)]
    [InlineData(1, -1, 400)]
    [InlineData(99, 1, 404)]
    [InlineData(1, 99, 404)]
    [InlineData(1, 1, 404)]
    public async Task InvalidOrMissingAssignment_ReturnsControlledError(long centerId, long sectionId, int status)
    {
        var response = Assert.IsAssignableFrom<ObjectResult>(await _controller.DeleteSectionAssignment(centerId, sectionId));
        Assert.Equal(status, response.StatusCode);
        Assert.False(Assert.IsType<ResponseApi>(response.Value).IsSuccess);
        Assert.Empty(_audit);
    }

    [Fact]
    public async Task MissingJwtUser_DoesNotDelete()
    {
        await SeedAssignment(1, 1, true, (1, 1));
        _controller.WithIdentity(userLogin: " ");
        Assert.Equal(403, Assert.IsType<ObjectResult>(await _controller.DeleteSectionAssignment(1, 1)).StatusCode);
        Assert.Single(await _context.SolutionCenterProducts.ToListAsync());
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("order")]
    [InlineData("delete-product")]
    [InlineData("configure")]
    [InlineData("reactivate-section")]
    public async Task DeletedAfterPrevalidation_WritersRecheckInsideTheirTransaction(string operation)
    {
        await SeedAssignment(1, 1, true, (1, 1));
        await using var staleContext = NewContext();
        var repository = new SolutionCenterRepository(staleContext);
        Assert.True(await repository.SectionBelongsToSolutionCenter(1, 1));
        // Incluso una entidad previamente materializada no sustituye la lectura bajo bloqueo.
        await staleContext.SolutionCenterSections.SingleAsync();
        var oldProduct = await staleContext.SolutionCenterProducts.SingleAsync();
        Assert.IsType<OkObjectResult>(await _controller.DeleteSectionAssignment(1, 1));

        switch (operation)
        {
            case "add":
                Assert.Equal(0, (await repository.AddProductToSection(1, 1, await staleContext.Products.SingleAsync(x => x.product_id == 3), 1, "ejecutor")).Value);
                break;
            case "order":
                Assert.False((await repository.UpdateProductOrder(1, 1, oldProduct.solution_center_product_id, 2)).Value);
                break;
            case "delete-product":
                Assert.False((await repository.DeleteProductFromSection(1, 1, oldProduct.solution_center_product_id)).Value);
                break;
            case "configure":
                Assert.Equal(0, (await new InventoryConfigurationRepository(staleContext).CreateAssignments(new[]
                    { new InventoryConfigurationAssignment { inventory_configuration_id = 1, solution_center_id = 1, section_id = 1, is_active = true } })).Value);
                break;
            default:
                Assert.False(await repository.UpdateSectionAssignmentStatus(1, 1, true));
                break;
        }
        Assert.Empty(await _context.SolutionCenterSections.ToListAsync());
        Assert.Empty(await _context.SolutionCenterProducts.ToListAsync());
        Assert.Empty(await _context.InventoryConfigurationAssignments.ToListAsync());
    }

    [Fact]
    public async Task OrphanConfiguration_CannotBeReactivated()
    {
        _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 1, solution_center_id = 1, section_id = 1, is_active = false });
        await _context.SaveChangesAsync();
        Assert.False((await new InventoryConfigurationRepository(_context).UpdateAssignmentStatus(1, 1, 1, true)).Value);
        Assert.False((await _context.InventoryConfigurationAssignments.AsNoTracking().SingleAsync()).is_active);
    }

    [Fact]
    public async Task ConfigurationBatch_WithRemovedPair_DoesNotInsertAnyRows()
    {
        await SeedAssignment(2, 1, true);
        var result = await new InventoryConfigurationRepository(_context).CreateAssignments(new[]
        {
            new InventoryConfigurationAssignment { inventory_configuration_id = 1, solution_center_id = 2, section_id = 1, is_active = true },
            new InventoryConfigurationAssignment { inventory_configuration_id = 1, solution_center_id = 1, section_id = 1, is_active = true }
        });
        Assert.Equal(0, result.Value);
        Assert.Empty(await _context.InventoryConfigurationAssignments.ToListAsync());
    }

    [Theory]
    [InlineData("delete", "add", true, false, 0)]
    [InlineData("add", "delete", true, true, 0)]
    [InlineData("delete", "configure", true, false, 0)]
    [InlineData("configure", "delete", true, false, 1)]
    [InlineData("reuse", "delete", true, true, 0)]
    [InlineData("delete", "reuse", true, true, 1)]
    [InlineData("delete", "assign", true, true, 0)]
    [InlineData("reactivate-config", "delete", true, false, 1)]
    public async Task ConcurrentWriters_AreSerializedWithoutOrphanRows(
        string firstOperation, string secondOperation, bool firstSuccess, bool secondSuccess, int remainingProducts)
    {
        await SeedAssignment(1, 1, true, (1, 1));
        if (firstOperation == "reactivate-config")
        {
            _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 1, solution_center_id = 1, section_id = 1, is_active = false });
            await _context.SaveChangesAsync();
        }

        var gate = new AssignmentReadGate();
        var attempt = new TransactionAttempt();
        await using var firstContext = NewContext(gate);
        await using var secondContext = NewContext(attempt);
        var first = ConcurrentWrite(firstContext, firstOperation);
        Task<bool>? second = null;
        try
        {
            await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(15));
            // SQLite puede bloquear sincrónicamente al iniciar la segunda transacción.
            second = Task.Run(() => ConcurrentWrite(secondContext, secondOperation));
            await attempt.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(second.IsCompleted);
        }
        finally
        {
            gate.Release.TrySetResult(true);
            await first.WaitAsync(TimeSpan.FromSeconds(20));
            if (second is not null) await second.WaitAsync(TimeSpan.FromSeconds(20));
        }

        Assert.Equal(firstSuccess, await first);
        Assert.Equal(secondSuccess, await second!);
        Assert.Equal(remainingProducts, await _context.SolutionCenterProducts.CountAsync());
        Assert.False(await _context.SolutionCenterProducts.AnyAsync(product =>
            !_context.SolutionCenterSections.Any(section => section.solution_center_id == product.solution_center_id && section.section_id == product.section_id)));
        Assert.False(await _context.InventoryConfigurationAssignments.AnyAsync(configuration =>
            !_context.SolutionCenterSections.Any(section => section.solution_center_id == configuration.solution_center_id && section.section_id == configuration.section_id)));
        if (secondOperation == "reuse")
            Assert.Equal(3, (await _context.SolutionCenterProducts.SingleAsync()).product_id);
    }

    private static async Task<bool> ConcurrentWrite(SitraCalcoContext context, string operation)
    {
        var repository = new SolutionCenterRepository(context);
        return operation switch
        {
            "delete" => (await repository.DeleteSectionAssignment(1, 1)).Value.Status == SolutionCenterSectionStatus.Success,
            "add" => (await repository.AddProductToSection(1, 1,
                new Product { product_id = 3, product_name = "Café", reference = "CA", unit_of_measure = "UND" }, 1, "ejecutor")).Value > 0,
            "configure" => (await new InventoryConfigurationRepository(context).CreateAssignments(new[]
                { new InventoryConfigurationAssignment { inventory_configuration_id = 1, solution_center_id = 1, section_id = 1, is_active = true } })).Value > 0,
            "reactivate-config" => (await new InventoryConfigurationRepository(context).UpdateAssignmentStatus(1, 1, 1, true)).Value,
            "assign" => await repository.CreateSectionAssignment(new() { solution_center_id = 1, section_id = 1, is_active = true }),
            _ => (await repository.CreateSectionConfiguration(1,
                new() { section_id = 1, section_name = "Pasillo 1", solution_center_type_id = 1, is_active = true },
                new() { new() { ProductId = 3, SortOrder = 2 } }, "ejecutor")).Value > 0
        };
    }

    private TestContext NewContext(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<SitraCalcoContext>()
        .UseSqlite(_connectionString).AddInterceptors(interceptors).Options);

    private static SolutionCenterApplication Application(SitraCalcoContext context) => new(
        new SolutionCenterRepository(context), new ProductRepository(context), Mock.Of<IPermissionApplication>());

    private async Task SeedAssignment(long centerId, long sectionId, bool active, params (long Id, int Order)[] products)
    {
        _context.SolutionCenterSections.Add(new() { solution_center_id = centerId, section_id = sectionId, is_active = active });
        _context.SolutionCenterProducts.AddRange(products.Select(product => new SolutionCenterProduct
        {
            solution_center_id = centerId, section_id = sectionId, product_id = product.Id,
            sort_order = product.Order, created_by = "original", created_at = new DateTime(2026, 1, 1)
        }));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task<string> OtherRows() => JsonSerializer.Serialize(new
    {
        Assignments = await _context.SolutionCenterSections.AsNoTracking().Where(x => x.solution_center_id != 1 || x.section_id != 1).OrderBy(x => x.solution_center_section_id).ToListAsync(),
        Products = await _context.SolutionCenterProducts.AsNoTracking().Where(x => x.solution_center_id != 1 || x.section_id != 1).OrderBy(x => x.solution_center_product_id).ToListAsync()
    });

    private sealed class CommandProbe : DbCommandInterceptor
    {
        public List<(string Text, bool InTransaction)> Commands { get; } = new();
        public bool FailSectionDelete { get; set; }

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
            if (FailSectionDelete && command.CommandText.StartsWith("DELETE FROM \"sitracalco_inventory_solution_center_section\"", StringComparison.Ordinal))
                throw new InvalidOperationException("Fallo simulado al eliminar la asociación.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CommitFailure : DbTransactionInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Enabled)
                throw new InvalidOperationException("Fallo simulado antes del commit.");
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
            var included = new[] { typeof(Product), typeof(Section), typeof(SolutionCenter), typeof(SolutionCenterType),
                typeof(SolutionCenterSection), typeof(SolutionCenterProduct), typeof(InventoryConfiguration),
                typeof(InventoryConfigurationAssignment), typeof(InventoryRecord) };
            foreach (var type in modelBuilder.Model.GetEntityTypes().Select(entity => entity.ClrType).ToList())
                if (!included.Contains(type)) modelBuilder.Ignore(type);
            modelBuilder.Entity<InventoryRecord>().Property(x => x.created_at).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
