using System.Linq.Expressions;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Dtos;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Inventory.Infrastructure.Persistance.Repositories;
using Inventory.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Inventory.Test.Controllers;

public class AuditDescriptionTests
{
    private readonly Mock<SitraCalcoContext> _context = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly Product _product = new() { product_id = 10, product_name = "Arroz Blanco", reference = "AR001", unit_of_measure = "KG" };
    private readonly Section _section = new() { section_id = 2, section_name = "Pasillo 1", is_active = true };
    private readonly InventoryConfiguration _configuration = new() { inventory_configuration_id = 3, inventory_configuration_name = "Semanal" };
    private readonly SolutionCenterProduct _association = new() { solution_center_product_id = 50, product_id = 10, solution_center_id = 1, section_id = 2, sort_order = 1 };

    public AuditDescriptionTests()
    {
        _context.SetupGet(x => x.Products).Returns(Set(_product).Object);
        _context.SetupGet(x => x.Sections).Returns(Set(_section).Object);
        _context.SetupGet(x => x.SolutionCenters).Returns(Set(new SolutionCenter { solution_center_id = 1, solution_center_name = "Poblado" }).Object);
        _context.SetupGet(x => x.InventoryConfigurations).Returns(Set(_configuration).Object);
        _context.SetupGet(x => x.SolutionCenterSections).Returns(Set(new SolutionCenterSection
            { solution_center_id = 1, section_id = 2, is_active = true }).Object);
        _context.SetupGet(x => x.SolutionCenterProducts).Returns(Set(_association,
            new SolutionCenterProduct { solution_center_product_id = 51, product_id = 99, solution_center_id = 1, section_id = 2, sort_order = 2 }).Object);
        _context.SetupGet(x => x.InventoryConfigurationAssignments).Returns(Set(
            new InventoryConfigurationAssignment { inventory_configuration_id = 3, solution_center_id = 1, section_id = 2 },
            new InventoryConfigurationAssignment { inventory_configuration_id = 3, solution_center_id = 9, section_id = 2 }).Object);
        _context.SetupGet(x => x.InventoryConfigurationDays).Returns(Set(
            new InventoryConfigurationDay { inventory_configuration_id = 3, day_of_week = "Lunes" }).Object);
        _context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var database = new Mock<DatabaseFacade>(_context.Object);
        database.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_transaction.Object);
        database.SetupGet(x => x.CurrentTransaction).Returns(_transaction.Object);
        database.SetupGet(x => x.ProviderName).Returns("Microsoft.EntityFrameworkCore.Sqlite");
        _context.SetupGet(x => x.Database).Returns(database.Object);
    }

    [Theory]
    [InlineData("create", "Se creó la sección Pasillo 1 en el centro de solución Poblado.")]
    [InlineData("status", "Se inactivó la sección Pasillo 1.")]
    [InlineData("add", "Se agregó el producto Arroz Blanco (referencia AR001, unidad KG) a la sección Pasillo 1 del centro de solución Poblado en la posición 2.")]
    [InlineData("order", "Se cambió el orden del producto Arroz Blanco (referencia AR001, unidad KG) en la sección Pasillo 1 del centro de solución Poblado a la posición 2.")]
    [InlineData("delete", "Se eliminó el producto Arroz Blanco (referencia AR001, unidad KG) de la sección Pasillo 1 del centro de solución Poblado.")]
    public async Task CenterOperations_DescribeTheAffectedEntities(string operation, string expected)
    {
        var repository = new SolutionCenterRepository(_context.Object);
        if (operation == "create")
            _section.section_id = 0; // Una sección nueva aún no tiene ID del catálogo.
        var description = operation switch
        {
            "create" => (await repository.CreateSectionConfiguration(1, _section, new(), "ejecutor")).Description,
            "status" => (await repository.UpdateSectionStatus(2, false)).Description,
            "add" => (await repository.AddProductToSection(1, 2, _product, 2, "ejecutor")).Description,
            "order" => (await repository.UpdateProductOrder(1, 2, 50, 2)).Description,
            _ => (await repository.DeleteProductFromSection(1, 2, 50)).Description
        };

        Assert.Equal(expected, description);
        _product.product_name = "Nombre posterior";
        _section.section_name = "Sección posterior";
        Assert.Equal(expected, description);
        Assert.DoesNotContain("ejecutor", description);
        if (operation != "status")
            _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("assign", "Se agregaron 1 asignaciones a la configuración de inventario Semanal.")]
    [InlineData("status", "Se activó la sección Pasillo 1 para el centro de solución Poblado en la configuración de inventario Semanal.")]
    [InlineData("days", "Se agregaron los días Martes a la configuración de inventario Semanal.")]
    [InlineData("delete-day", "Se eliminó el día Lunes de la configuración de inventario Semanal.")]
    [InlineData("delete", "Se eliminó la asociación de la sección Pasillo 1 del centro de solución Poblado en la configuración de inventario Semanal.")]
    [InlineData("delete-section", "Se eliminó la sección Pasillo 1 de 2 asociación(es) de la configuración de inventario Semanal.")]
    public async Task ConfigurationOperations_DescribeStoredNamesAndDays(string operation, string expected)
    {
        var repository = new InventoryConfigurationRepository(_context.Object);
        if (operation == "assign")
            _context.SetupGet(x => x.InventoryConfigurationAssignments).Returns(Set<InventoryConfigurationAssignment>().Object);
        var description = operation switch
        {
            "assign" => (await repository.CreateAssignments(new[] { new InventoryConfigurationAssignment { inventory_configuration_id = 3, solution_center_id = 1, section_id = 2 } })).Description,
            "status" => (await repository.UpdateAssignmentStatus(3, 1, 2, true)).Description,
            "days" => (await new InventoryConfigurationApplication(repository, Mock.Of<IPermissionApplication>())
                .AddDays(3, new AddInventoryConfigurationDaysDto { Days = new() { " martes " } })).Description,
            "delete-day" => (await repository.DeleteDay(3, " lunes ")).Description,
            "delete" => (await repository.DeleteAssignment(3, 1, 2)).Description,
            _ => (await repository.DeleteAssignmentsBySection(3, 2)).Description
        };

        _configuration.inventory_configuration_name = "Nombre posterior";
        Assert.Equal(expected, description);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MissingCatalogNames_DoNotPreventDeletingTheExistingAssignment()
    {
        _context.SetupGet(x => x.Products).Returns(Set<Product>().Object);
        var result = await new SolutionCenterRepository(_context.Object).DeleteProductFromSection(1, 2, 50);

        Assert.True(result.Value);
        Assert.Contains("sin nombre disponible (ID 10)", result.Description);
        Assert.Contains("Pasillo 1 del centro de solución Poblado", result.Description);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("save")]
    [InlineData("commit")]
    public async Task FailedDeletion_DoesNotWriteSuccessAudit(string failure)
    {
        if (failure == "save")
            _context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Falló el guardado"));
        if (failure == "commit")
            _transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Falló la confirmación"));
        var logs = new Mock<ILogApplication>(MockBehavior.Strict);
        var application = new SolutionCenterApplication(new SolutionCenterRepository(_context.Object),
            Mock.Of<Inventory.Domain.Interfaces.IProductRepository>(), Mock.Of<IPermissionApplication>());
        // La operación conserva su validación de asignación activa.
        _context.SetupGet(x => x.SolutionCenterSections).Returns(Set(new SolutionCenterSection
            { solution_center_id = 1, section_id = 2, is_active = true }).Object);
        var controller = new SolutionCenterController(application, logs.Object,
            NullLogger<SolutionCenterController>.Instance).WithIdentity(userLogin: "ejecutor");

        var response = Assert.IsAssignableFrom<ObjectResult>(await controller.DeleteProductFromSection(1, 2, failure == "missing" ? 999 : 50));

        Assert.Equal(failure == "missing" ? 400 : 500, response.StatusCode);
        logs.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LongProductNames_KeepReferenceUnitLocationAndPositionWithinLogLimit()
    {
        _product.product_name = new string('A', 500);
        _section.section_name = new string('S', 150);
        var result = await new SolutionCenterRepository(_context.Object).UpdateProductOrder(1, 2, 50, 2);

        Assert.InRange(result.Description.Length, 1, 500);
        Assert.Contains("referencia AR001, unidad KG", result.Description);
        Assert.Contains("Poblado a la posición 2.", result.Description);
        Assert.Contains("...", result.Description);
    }

    // Consultas LINQ asíncronas en memoria y escrituras simuladas: no se conecta a MySQL.
    private static Mock<DbSet<T>> Set<T>(params T[] rows) where T : class
    {
        var query = new AsyncRows<T>(rows);
        var set = new Mock<DbSet<T>>();
        set.As<IQueryable<T>>().Setup(x => x.Provider).Returns(((IQueryable<T>)query).Provider);
        set.As<IQueryable<T>>().Setup(x => x.Expression).Returns(((IQueryable<T>)query).Expression);
        set.As<IQueryable<T>>().Setup(x => x.ElementType).Returns(typeof(T));
        set.As<IQueryable<T>>().Setup(x => x.GetEnumerator()).Returns(() => rows.AsEnumerable().GetEnumerator());
        set.As<IAsyncEnumerable<T>>().Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => query.GetAsyncEnumerator());
        return set;
    }

    private sealed class AsyncRows<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public AsyncRows(IEnumerable<T> rows) : base(rows) { }
        public AsyncRows(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        IQueryProvider IQueryable.Provider => new AsyncProvider(this);
    }

    private sealed class AsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    }

    private sealed class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => (IQueryable)Activator.CreateInstance(
            typeof(AsyncRows<>).MakeGenericType(expression.Type.GetGenericArguments()[0]), expression)!;
        public IQueryable<T> CreateQuery<T>(Expression expression) => new AsyncRows<T>(expression);
        public object? Execute(Expression expression) => inner.Execute(expression);
        public T Execute<T>(Expression expression) => inner.Execute<T>(expression);
        public T ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken = default) =>
            (T)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(typeof(T).GetGenericArguments()[0])
                .Invoke(null, new[] { inner.Execute(expression) })!;
    }
}
