using System.Text.Json;
using System.Data.Common;
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

// Controlador, aplicación y repositorios reales; conexión exclusiva a SQLite en memoria.
public class CreateSectionConfigurationTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Mock<ILogApplication> _logs = new();
    private readonly List<CreateLogDto> _audit = new();
    private readonly ReadCounter _queries = new();
    private TestContext _context = null!;
    private SolutionCenterController _controller = null!;
    private SectionController _sections = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _context = new TestContext(new DbContextOptionsBuilder<SitraCalcoContext>()
            .UseSqlite(_connection).AddInterceptors(_queries).Options);
        await _context.Database.EnsureCreatedAsync();
        _context.SolutionCenterTypes.AddRange(
            new() { solution_center_type_id = 1, solution_center_type_name = "Bodega" },
            new() { solution_center_type_id = 2, solution_center_type_name = "Punto de Venta" });
        _context.SolutionCenters.AddRange(
            new() { solution_center_id = 1, solution_center_type_id = 1, solution_center_code = "B1", solution_center_name = "Poblado", is_active = true },
            new() { solution_center_id = 2, solution_center_type_id = 1, solution_center_code = "B2", solution_center_name = "Centro", is_active = true },
            new() { solution_center_id = 3, solution_center_type_id = 2, solution_center_code = "P1", solution_center_name = "PDV Norte", is_active = true },
            new() { solution_center_id = 4, solution_center_type_id = 2, solution_center_code = "P2", solution_center_name = "PDV Sur", is_active = true });
        _context.Sections.Add(new() { section_id = 2, section_name = "Pasillo 3", is_active = true, solution_center_type_id = 1 });
        _context.Products.AddRange(
            new() { product_id = 1, product_name = "Arroz", reference = "AR001", unit_of_measure = "KG" },
            new() { product_id = 2, product_name = "Sal", reference = "SA001", unit_of_measure = "KG" },
            new() { product_id = 3, product_name = "Arroz otro plan", reference = " ar001 ", unit_of_measure = " kg " },
            new() { product_id = 4, product_name = "Arroz", reference = "AR001", unit_of_measure = "UND" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        _logs.Setup(x => x.CreateLog(It.IsAny<CreateLogDto>()))
            .Callback<CreateLogDto>(_audit.Add).ReturnsAsync(1L);
        var application = new SolutionCenterApplication(new SolutionCenterRepository(_context),
            new ProductRepository(_context), Mock.Of<IPermissionApplication>());
        _controller = new SolutionCenterController(application, _logs.Object,
            NullLogger<SolutionCenterController>.Instance).WithIdentity(userLogin: "  ejecutor.jwt  ");
        _sections = new SectionController(
            new SectionApplication(new SectionRepository(_context), new SolutionCenterRepository(_context)),
            _logs.Object, new CreateSectionDtoValidator(), new UpdateSectionDtoValidator(),
            NullLogger<SectionController>.Instance).WithIdentity(userLogin: "ejecutor.jwt");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Theory]
    [InlineData("{\"id_section\":null,\"sectionName\":\" Pasillo nuevo \",\"products\":[{\"productId\":1,\"sortOrder\":1}]}")]
    [InlineData("{\"sectionName\":\" Pasillo nuevo \",\"products\":[{\"productId\":1,\"sortOrder\":1}],\"userName\":\"intruso\"}")]
    public async Task NewSection_AndLegacyRequest_CreateAllRowsAndUseJwt(string json)
    {
        var request = JsonSerializer.Deserialize<CreateSectionConfigurationDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Null(request.IdSection);

        var sectionId = Success(await _controller.CreateSectionConfiguration(1, request));

        Assert.NotEqual(2, sectionId);
        var section = await _context.Sections.AsNoTracking().SingleAsync(x => x.section_id == sectionId);
        Assert.Equal("Pasillo nuevo", section.section_name);
        Assert.True(section.is_active);
        Assert.Equal(1, section.solution_center_type_id);
        Assert.Equal(2, await _context.Sections.CountAsync());
        var assignment = await _context.SolutionCenterSections.SingleAsync();
        Assert.Equal((1L, sectionId, true), (assignment.solution_center_id, assignment.section_id, assignment.is_active));
        var product = await _context.SolutionCenterProducts.SingleAsync();
        Assert.Equal((1L, sectionId, 1L, 1), (product.solution_center_id, product.section_id, product.product_id, product.sort_order));
        Assert.Equal("ejecutor.jwt", product.created_by);
        var log = Assert.Single(_audit);
        Assert.Equal("ejecutor.jwt", log.UserName);
        Assert.Equal("Crear", log.Action);
        Assert.Equal("ConfiguracionBodegas", log.Module);
        Assert.Contains("Se creó la sección Pasillo nuevo", log.Description);
    }

    [Theory]
    [InlineData("{\"id_section\":2,\"products\":[{\"productId\":1,\"sortOrder\":1}]}")]
    [InlineData("{\"id_section\":2,\"sectionName\":\"Nombre ignorado\",\"products\":[{\"productId\":1,\"sortOrder\":1}]}")]
    [InlineData("{\"id_section\":2,\"sectionName\":\"\",\"products\":[{\"productId\":1,\"sortOrder\":1}]}")]
    public async Task ExistingSection_UsesCatalogNameAndPreservesGlobalStatus(string json)
    {
        var section = await _context.Sections.SingleAsync();
        section.is_active = false;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var request = JsonSerializer.Deserialize<CreateSectionConfigurationDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(2, request.IdSection);

        Assert.Equal(2, Success(await _controller.CreateSectionConfiguration(1, request)));

        section = await _context.Sections.AsNoTracking().SingleAsync();
        Assert.Equal("Pasillo 3", section.section_name);
        Assert.False(section.is_active);
        Assert.Equal(2, (await _context.SolutionCenterSections.SingleAsync()).section_id);
        Assert.Equal(2, (await _context.SolutionCenterProducts.SingleAsync()).section_id);
        Assert.Contains("Se reutilizó la sección Pasillo 3 en el centro de solución Poblado", Assert.Single(_audit).Description);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidSectionId_Returns400WithoutWrites(long sectionId)
    {
        Failure(await _controller.CreateSectionConfiguration(1, Request(sectionId)));
        await AssertNoAssignments();
    }

    [Fact]
    public async Task DuplicateGlobalName_ExplainsHowToReuseIt()
    {
        var request = Request(null);
        request.SectionName = "  pasillo 3  ";

        var response = Failure(await _controller.CreateSectionConfiguration(1, request));

        Assert.Contains("utilice su ID en id_section", response.Message);
        await AssertNoAssignments();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistingAssignment_IsIdempotentAndKeepsStatusAndPositions(bool active)
    {
        await SeedAssignment(1, 2, active, (1, 4));
        var request = Request(2, (1, 7), (2, 1));

        Assert.Equal(2, Success(await _controller.CreateSectionConfiguration(1, request)));
        Assert.Equal(2, Success(await _controller.CreateSectionConfiguration(1, request)));

        Assert.Equal(active, (await _context.SolutionCenterSections.AsNoTracking().SingleAsync()).is_active);
        var products = await _context.SolutionCenterProducts.AsNoTracking().OrderBy(x => x.product_id).ToListAsync();
        Assert.Equal(2, products.Count);
        Assert.Equal((1L, 4, "original"), (products[0].product_id, products[0].sort_order, products[0].created_by));
        Assert.Equal((2L, 1, "ejecutor.jwt"), (products[1].product_id, products[1].sort_order, products[1].created_by));
        Assert.Contains("se agregaron 0 productos", _audit[1].Description);
    }

    [Fact]
    public async Task SharedSection_KeepsOtherCentersAndSectionsIndependent()
    {
        await SeedAssignment(2, 2, false, (1, 1), (3, 2));
        _context.Sections.Add(new() { section_id = 3, section_name = "Otra sección", is_active = true, solution_center_type_id = 1 });
        await SeedAssignment(1, 3, false, (2, 1));

        Assert.Equal(2, Success(await _controller.CreateSectionConfiguration(1, Request(2, (1, 3), (2, 1)))));

        var otherCenter = await _context.SolutionCenterProducts.AsNoTracking()
            .Where(x => x.solution_center_id == 2).OrderBy(x => x.product_id).ToListAsync();
        Assert.Equal(new[] { (1L, 1), (3L, 2) }, otherCenter.Select(x => (x.product_id, x.sort_order)));
        Assert.All(otherCenter, x => Assert.Equal("original", x.created_by));
        var otherSection = await _context.SolutionCenterProducts.AsNoTracking().SingleAsync(x => x.section_id == 3);
        Assert.Equal((2L, 1, "original"), (otherSection.product_id, otherSection.sort_order, otherSection.created_by));
        Assert.Equal(2, await _context.SolutionCenterProducts.CountAsync(x => x.solution_center_id == 1 && x.section_id == 2));
        Assert.Equal(3, await _context.SolutionCenterSections.CountAsync());
        Assert.All(await _context.SolutionCenterSections.AsNoTracking()
            .Where(x => x.solution_center_id == 2 || x.section_id == 3).ToListAsync(), x => Assert.False(x.is_active));
    }

    [Fact]
    public async Task OccupiedPosition_Returns400WithoutAddingAnyProduct()
    {
        await SeedAssignment(1, 2, false, (1, 1));

        Failure(await _controller.CreateSectionConfiguration(1, Request(2, (2, 1), (4, 2))));

        Assert.Single(await _context.SolutionCenterProducts.ToListAsync());
        Assert.False((await _context.SolutionCenterSections.AsNoTracking().SingleAsync()).is_active);
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public async Task ExistingReference_RejectsSameUnitAndAllowsDifferentUnit(long productId, bool valid)
    {
        await SeedAssignment(1, 2, true, (1, 1));

        var result = await _controller.CreateSectionConfiguration(1, Request(2, (productId, 2)));

        if (valid)
            Assert.Equal(2, Success(result));
        else
            Failure(result);
        Assert.Equal(valid ? 2 : 1, await _context.SolutionCenterProducts.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(2L)]
    public async Task InvalidProductRequests_Return400WithoutWrites(long? sectionId)
    {
        var requests = new[]
        {
            Request(sectionId, (0, 1)), Request(sectionId, (1, 0)),
            Request(sectionId, (999, 1)), Request(sectionId, (1, 1), (1, 2)),
            Request(sectionId, (1, 1), (2, 1)), Request(sectionId, (1, 1), (3, 2)),
            new CreateSectionConfigurationDto { IdSection = sectionId, SectionName = "Nueva" },
            new CreateSectionConfigurationDto { IdSection = sectionId, SectionName = "Nueva", Products = null! },
            new CreateSectionConfigurationDto { IdSection = sectionId, SectionName = "Nueva", Products = new() { null! } }
        };
        foreach (var request in requests)
        {
            Failure(await _controller.CreateSectionConfiguration(1, request));
            await AssertNoAssignments();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NewSection_StillRequiresName(string? name)
    {
        var request = Request(null);
        request.SectionName = name;
        Failure(await _controller.CreateSectionConfiguration(1, request));
        await AssertNoAssignments();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(2L)]
    public async Task MissingCenter_Returns400WithoutWrites(long? sectionId)
    {
        Failure(await _controller.CreateSectionConfiguration(999, Request(sectionId)));
        await AssertNoAssignments();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(2L, false)]
    [InlineData(2L, true)]
    public async Task ProductInsertFailure_RollsBackSectionAssignmentAndProducts(long? sectionId, bool assigned)
    {
        if (assigned)
            await SeedAssignment(1, 2, false, (4, 3));
        // El segundo INSERT falla en el motor, después de haber intentado guardar el primero.
        await _context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER fail_product BEFORE INSERT ON sitracalco_inventory_solution_center_product
            WHEN NEW.product_id = 2
            BEGIN SELECT RAISE(ABORT, 'Fallo de inserción para la prueba'); END;
            """);

        var result = Assert.IsType<ObjectResult>(await _controller.CreateSectionConfiguration(1, Request(sectionId, (1, 1), (2, 2))));

        Assert.Equal(500, result.StatusCode);
        _context.ChangeTracker.Clear();
        Assert.Equal(1, await _context.Sections.CountAsync());
        Assert.Equal(assigned ? 1 : 0, await _context.SolutionCenterSections.CountAsync());
        Assert.Equal(assigned ? 1 : 0, await _context.SolutionCenterProducts.CountAsync());
        if (assigned)
        {
            Assert.False((await _context.SolutionCenterSections.SingleAsync()).is_active);
            var product = await _context.SolutionCenterProducts.SingleAsync();
            Assert.Equal((4L, 3, "original"), (product.product_id, product.sort_order, product.created_by));
        }
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(2L)]
    public async Task CatalogCreate_RequiresAndPersistsAnExistingType(long typeId)
    {
        var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(await _sections.Create(
            new CreateSectionDto { SectionName = " Nueva sección ", SolutionCenterTypeId = typeId })).Value);

        Assert.True(response.IsSuccess);
        var created = await _context.Sections.AsNoTracking().SingleAsync(x => x.section_name == "Nueva sección");
        Assert.Equal(typeId, created.solution_center_type_id);
        Assert.True(created.is_active);
        Assert.Equal("Se creó la sección Nueva sección.", Assert.Single(_audit).Description);
        Assert.Equal("ejecutor.jwt", _audit[0].UserName);
    }

    [Theory]
    [InlineData("{\"sectionName\":\"Nueva\"}")]
    [InlineData("{\"sectionName\":\"Nueva\",\"solutionCenterTypeId\":0}")]
    [InlineData("{\"sectionName\":\"Nueva\",\"solutionCenterTypeId\":-1}")]
    [InlineData("{\"sectionName\":\"Nueva\",\"solutionCenterTypeId\":999}")]
    public async Task CatalogCreate_InvalidOrOmittedTypeDoesNotInsert(string json)
    {
        var request = JsonSerializer.Deserialize<CreateSectionDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.IsType<BadRequestObjectResult>(await _sections.Create(request));
        await AssertNoAssignments();
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData(1L, 2)]
    [InlineData(2L, 1)]
    public async Task CatalogGet_FiltersByTypeAndKeepsNameOrderAndInactiveSections(long? typeId, int count)
    {
        foreach (var type in await _context.SolutionCenterTypes.ToListAsync())
            type.solution_center_type_name = $"Tipo real {type.solution_center_type_id}";
        _context.Sections.AddRange(
            new() { section_name = "A PDV", solution_center_type_id = 2, is_active = false },
            new() { section_name = "Z Bodega", solution_center_type_id = 1, is_active = true });
        await _context.SaveChangesAsync();

        var readsBefore = _queries.Reads;
        var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(await _sections.GetAll(typeId)).Value);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionDto>>(response.Result).ToList();

        Assert.InRange(_queries.Reads - readsBefore, 1, typeId.HasValue ? 2 : 1);
        Assert.All(sections, x => Assert.Equal($"Tipo real {x.SolutionCenterTypeId}", x.SolutionCenterTypeName));
        Assert.Equal(count, sections.Count);
        Assert.Equal(sections.OrderBy(x => x.SectionName).Select(x => x.SectionId), sections.Select(x => x.SectionId));
        if (typeId.HasValue)
            Assert.All(sections, x => Assert.Equal(typeId, x.SolutionCenterTypeId));
        if (typeId != 1)
            Assert.Contains(sections, x => !x.IsActive && x.SolutionCenterTypeId == 2);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(999L)]
    public async Task CatalogGet_InvalidTypeReturns400(long typeId)
    {
        Failure(await _sections.GetAll(typeId));
        await AssertNoAssignments();
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(2L)]
    public async Task CatalogDetail_IncludesTypeNameAndPreservesNotFound(long typeId)
    {
        await SetSectionType(typeId);
        (await _context.SolutionCenterTypes.SingleAsync(x => x.solution_center_type_id == typeId))
            .solution_center_type_name = "Nombre actualizado del catálogo";
        await _context.SaveChangesAsync();
        var readsBefore = _queries.Reads;
        var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(await _sections.GetById(2)).Value);
        var section = Assert.IsType<SectionDto>(response.Result);
        Assert.Equal(1, _queries.Reads - readsBefore);
        Assert.Equal((2L, "Pasillo 3", typeId, true), (section.SectionId, section.SectionName, section.SolutionCenterTypeId, section.IsActive));
        var json = JsonSerializer.SerializeToElement(section, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Nombre actualizado del catálogo", json.GetProperty("solutionCenterTypeName").GetString());
        Assert.IsType<NotFoundObjectResult>(await _sections.GetById(999));
    }

    [Theory]
    [InlineData("{\"sectionName\":\"Renombrada\",\"isActive\":false}", true)]
    [InlineData("{\"sectionName\":\"Renombrada\",\"isActive\":false,\"solutionCenterTypeId\":null}", false)]
    [InlineData("{\"sectionName\":\"Renombrada\",\"isActive\":false,\"solutionCenterTypeId\":1}", true)]
    [InlineData("{\"sectionName\":\"Renombrada\",\"isActive\":false,\"solutionCenterTypeId\":1}", false)]
    public async Task CatalogUpdate_OmittedOrUnchangedTypeAllowsExistingRelationships(string json, bool active)
    {
        await SeedAssignment(1, 2, active, (1, 4));
        var request = JsonSerializer.Deserialize<UpdateSectionDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.IsType<OkObjectResult>(await _sections.Update(2, request));

        var section = await _context.Sections.AsNoTracking().SingleAsync();
        Assert.Equal(("Renombrada", false, 1L), (section.section_name, section.is_active, section.solution_center_type_id));
        Assert.Equal(active, (await _context.SolutionCenterSections.AsNoTracking().SingleAsync()).is_active);
        var product = await _context.SolutionCenterProducts.AsNoTracking().SingleAsync();
        Assert.Equal((1L, 4, "original"), (product.product_id, product.sort_order, product.created_by));
        Assert.Contains("Se actualizó la sección Renombrada", Assert.Single(_audit).Description);
    }

    [Theory]
    [InlineData(1L, 2L, "Bodega", "Punto de Venta")]
    [InlineData(2L, 1L, "Punto de Venta", "Bodega")]
    public async Task CatalogUpdate_ChangesTypeWithoutDependenciesAndAuditsRealNames(
        long oldType, long newType, string oldName, string newName)
    {
        await SetSectionType(oldType);
        var request = new UpdateSectionDto { SectionName = " Renombrada ", IsActive = false, SolutionCenterTypeId = newType };

        var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(await _sections.Update(2, request)).Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Sección actualizada correctamente.", response.Message);
        var section = await _context.Sections.AsNoTracking().SingleAsync();
        Assert.Equal(("Renombrada", false, newType), (section.section_name, section.is_active, section.solution_center_type_id));
        Assert.Empty(await _context.SolutionCenterSections.ToListAsync());
        Assert.Empty(await _context.SolutionCenterProducts.ToListAsync());
        Assert.Empty(await _context.InventoryConfigurationAssignments.ToListAsync());
        var log = Assert.Single(_audit);
        Assert.Contains($"tipo de {oldName} a {newName}", log.Description);
        Assert.Equal("ejecutor.jwt", log.UserName);

        // La nueva clasificación sigue respetándose en los endpoints existentes de SolutionCenter.
        var incompatibleCenter = newType == 1 ? 3L : 1L;
        Failure(await _controller.CreateSectionConfiguration(incompatibleCenter, Request(2)));
        Assert.Empty(await _context.SolutionCenterProducts.ToListAsync());
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(999L)]
    public async Task CatalogUpdate_InvalidTypeDoesNotChangeAnyProperty(long typeId)
    {
        var result = Assert.IsType<BadRequestObjectResult>(await _sections.Update(2,
            new UpdateSectionDto { SectionName = "No guardar", IsActive = false, SolutionCenterTypeId = typeId }));
        var response = Assert.IsType<ResponseApi>(result.Value);

        Assert.False(response.IsSuccess);
        if (typeId > 0)
            Assert.Equal("El tipo de centro de solución no existe.", response.Message);
        else
            Assert.Contains("El tipo de centro de solución debe ser mayor a cero.",
                Assert.IsAssignableFrom<IEnumerable<string>>(response.Result));
        await AssertOriginalSection();
    }

    [Theory]
    [InlineData("assignment", true)]
    [InlineData("assignment", false)]
    [InlineData("product", false)]
    [InlineData("configuration", true)]
    [InlineData("configuration", false)]
    public async Task CatalogUpdate_TypeChangeRejectsEveryDependencyWithoutChangingData(string relation, bool active)
    {
        if (relation == "assignment")
            await SeedAssignment(1, 2, active);
        if (relation == "product")
            _context.SolutionCenterProducts.Add(new() { solution_center_id = 1, section_id = 2,
                product_id = 1, sort_order = 4, created_by = "original", created_at = DateTime.UtcNow });
        if (relation == "configuration")
        {
            _context.InventoryConfigurations.Add(new() { inventory_configuration_id = 1, inventory_configuration_name = "Semanal" });
            _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 1, solution_center_id = 1, section_id = 2, is_active = active });
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var response = Failure(await _sections.Update(2,
            new UpdateSectionDto { SectionName = "No guardar", IsActive = false, SolutionCenterTypeId = 2 }));

        Assert.Equal("No se puede cambiar el tipo de la sección porque tiene centros de solución, productos o configuraciones de inventario asociados.", response.Message);
        await AssertOriginalSection();
        Assert.Equal(relation == "assignment" ? 1 : 0, await _context.SolutionCenterSections.CountAsync());
        Assert.Equal(relation == "product" ? 1 : 0, await _context.SolutionCenterProducts.CountAsync());
        Assert.Equal(relation == "configuration" ? 1 : 0, await _context.InventoryConfigurationAssignments.CountAsync());
        if (relation == "assignment")
            Assert.Equal(active, (await _context.SolutionCenterSections.AsNoTracking().SingleAsync()).is_active);
        if (relation == "product")
        {
            var product = await _context.SolutionCenterProducts.AsNoTracking().SingleAsync();
            Assert.Equal((1L, 4, "original"), (product.product_id, product.sort_order, product.created_by));
        }
        if (relation == "configuration")
            Assert.Equal(active, (await _context.InventoryConfigurationAssignments.AsNoTracking().SingleAsync()).is_active);
    }

    [Fact]
    public async Task CatalogUpdate_TypeChangeStillRejectsDuplicateNames()
    {
        _context.Sections.Add(new() { section_name = "Nombre ocupado", solution_center_type_id = 2, is_active = true });
        await _context.SaveChangesAsync();

        var response = Failure(await _sections.Update(2,
            new UpdateSectionDto { SectionName = " nombre ocupado ", IsActive = false, SolutionCenterTypeId = 2 }));

        Assert.Equal("No se pudo actualizar la sección. Verifique que el nombre no esté registrado.", response.Message);
        await AssertOriginalSection();
        Assert.Equal(2, await _context.Sections.CountAsync());
    }

    private async Task AssertOriginalSection()
    {
        var section = await _context.Sections.AsNoTracking().SingleAsync(x => x.section_id == 2);
        Assert.Equal(("Pasillo 3", true, 1L), (section.section_name, section.is_active, section.solution_center_type_id));
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("assignment")]
    [InlineData("product")]
    [InlineData("configuration")]
    public async Task CatalogDelete_KeepsRestrictionsAndDoesNotDeleteParentsOrRelatedRows(string relation)
    {
        if (relation == "assignment")
            await SeedAssignment(1, 2, false);
        if (relation == "product")
            _context.SolutionCenterProducts.Add(new() { solution_center_id = 1, section_id = 2,
                product_id = 1, sort_order = 3, created_by = "original", created_at = DateTime.UtcNow });
        if (relation == "configuration")
        {
            _context.InventoryConfigurations.Add(new() { inventory_configuration_id = 1, inventory_configuration_name = "Semanal" });
            _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 1, solution_center_id = 1, section_id = 2, is_active = false });
        }
        await _context.SaveChangesAsync();

        var result = await _sections.Delete(2);

        if (relation == "none")
        {
            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(await _context.Sections.ToListAsync());
            Assert.Contains("Se eliminó la sección Pasillo 3", Assert.Single(_audit).Description);
        }
        else
        {
            Failure(result);
            Assert.Single(await _context.Sections.ToListAsync());
            Assert.Empty(_audit);
        }
        Assert.Equal(relation == "assignment" ? 1 : 0, await _context.SolutionCenterSections.CountAsync());
        Assert.Equal(relation == "product" ? 1 : 0, await _context.SolutionCenterProducts.CountAsync());
        Assert.Equal(relation == "configuration" ? 1 : 0, await _context.InventoryConfigurationAssignments.CountAsync());
        Assert.Equal(4, await _context.SolutionCenters.CountAsync());
        Assert.Equal(2, await _context.SolutionCenterTypes.CountAsync());
        var foreignKey = _context.Model.FindEntityType(typeof(Section))!.GetForeignKeys().Single();
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Theory]
    [InlineData(1L, 1L, 2L)]
    [InlineData(3L, 2L, 1L)]
    public async Task NewSectionFromCenter_InheritsStoredTypeAndIgnoresFrontendType(long centerId, long expectedType, long forgedType)
    {
        var request = JsonSerializer.Deserialize<CreateSectionConfigurationDto>(
            $$"""{"sectionName":"Nueva","solutionCenterTypeId":{{forgedType}},"products":[{"productId":1,"sortOrder":1}]}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var sectionId = Success(await _controller.CreateSectionConfiguration(centerId, request));

        Assert.Equal(expectedType, (await _context.Sections.AsNoTracking().SingleAsync(x => x.section_id == sectionId)).solution_center_type_id);
    }

    [Theory]
    [InlineData(1L, 1L, 2L)]
    [InlineData(2L, 3L, 4L)]
    public async Task ReuseBetweenCentersOfSameType_KeepsIndependentProducts(long typeId, long sourceCenter, long targetCenter)
    {
        await SetSectionType(typeId);
        await SeedAssignment(sourceCenter, 2, false, (1, 1));

        Assert.Equal(2, Success(await _controller.CreateSectionConfiguration(targetCenter, Request(2, (1, 3), (2, 2)))));

        Assert.Single(await _context.Sections.ToListAsync());
        var original = await _context.SolutionCenterProducts.AsNoTracking().SingleAsync(x => x.solution_center_id == sourceCenter);
        Assert.Equal((1L, 1, "original"), (original.product_id, original.sort_order, original.created_by));
        Assert.False((await _context.SolutionCenterSections.AsNoTracking().SingleAsync(x => x.solution_center_id == sourceCenter)).is_active);
        Assert.Equal(2, await _context.SolutionCenterProducts.CountAsync(x => x.solution_center_id == targetCenter));
    }

    [Theory]
    [InlineData(1L, 3L)]
    [InlineData(2L, 1L)]
    public async Task ReuseAcrossTypes_ReturnsClear400WithoutWrites(long typeId, long centerId)
    {
        await SetSectionType(typeId);
        var response = Failure(await _controller.CreateSectionConfiguration(centerId, Request(2)));

        Assert.Equal("El tipo de la sección no coincide con el tipo del centro de solución.", response.Message);
        await AssertNoAssignments();
    }

    [Theory]
    [InlineData(1L, 1L, true)]
    [InlineData(2L, 3L, true)]
    [InlineData(1L, 3L, false)]
    [InlineData(2L, 1L, false)]
    public async Task DirectAssignment_ValidatesTypeBeforeCreatingOrReactivating(long typeId, long centerId, bool compatible)
    {
        await SetSectionType(typeId);
        if (compatible)
        {
            Assert.IsType<OkObjectResult>(await _controller.AssignExistingSection(centerId, 2));
            Failure(await _controller.AssignExistingSection(centerId, 2)); // No duplica una relación activa.
        }
        else
        {
            Assert.Contains("tipo", Failure(await _controller.AssignExistingSection(centerId, 2)).Message);
            await AssertNoAssignments();
            await SeedAssignment(centerId, 2, false); // Simular una relación histórica incompatible.
        }

        var assignment = await _context.SolutionCenterSections.SingleAsync();
        assignment.is_active = false;
        await _context.SaveChangesAsync();
        var result = await _controller.AssignExistingSection(centerId, 2);
        if (compatible)
            Assert.IsType<OkObjectResult>(result);
        else
            Failure(result);
        Assert.Equal(compatible, (await _context.SolutionCenterSections.AsNoTracking().SingleAsync()).is_active);
        Assert.Empty(await _context.SolutionCenterProducts.ToListAsync());
    }

    [Fact]
    public async Task SectionAssignmentStatus_DoesNotReactivateIncompatibleTypes()
    {
        await SeedAssignment(3, 2, false, (1, 4));
        Failure(await _controller.UpdateSectionAssignmentStatus(3, 2, new UpdateStatusDto { IsActive = true }));
        Assert.False((await _context.SolutionCenterSections.AsNoTracking().SingleAsync()).is_active);
        Assert.Equal(4, (await _context.SolutionCenterProducts.AsNoTracking().SingleAsync()).sort_order);
        Assert.Empty(_audit);
    }

    [Theory]
    [InlineData(2L, true)]
    [InlineData(3L, false)]
    public async Task InventoryAssignments_ValidateAllTypesBeforeInserting(long otherCenter, bool compatible)
    {
        await SeedAssignment(1, 2, true);
        await SeedAssignment(otherCenter, 2, true);
        _context.InventoryConfigurations.Add(new() { inventory_configuration_id = 1, inventory_configuration_name = "Semanal" });
        await _context.SaveChangesAsync();
        var application = new InventoryConfigurationApplication(new InventoryConfigurationRepository(_context), Mock.Of<IPermissionApplication>());

        var result = await application.CreateAssignments(1, new CreateInventoryConfigurationAssignmentsDto
        {
            Assignments = new() { new() { SolutionCenterId = 1, SectionId = 2 }, new() { SolutionCenterId = otherCenter, SectionId = 2 } }
        });

        Assert.Equal(compatible ? 2 : 0, result.Value);
        Assert.Equal(compatible ? 2 : 0, await _context.InventoryConfigurationAssignments.CountAsync());
    }

    [Theory]
    [InlineData(1L, true)]
    [InlineData(3L, false)]
    public async Task InventoryAssignmentStatus_OnlyReactivatesCompatibleTypes(long centerId, bool compatible)
    {
        await SeedAssignment(centerId, 2, true);
        _context.InventoryConfigurations.Add(new() { inventory_configuration_id = 1, inventory_configuration_name = "Semanal" });
        _context.InventoryConfigurationAssignments.Add(new() { inventory_configuration_id = 1, solution_center_id = centerId, section_id = 2, is_active = false });
        await _context.SaveChangesAsync();
        var application = new InventoryConfigurationApplication(new InventoryConfigurationRepository(_context), Mock.Of<IPermissionApplication>());

        Assert.Equal(compatible, (await application.UpdateAssignmentStatus(1, centerId, 2, true)).Value);
        Assert.Equal(compatible, (await _context.InventoryConfigurationAssignments.AsNoTracking().SingleAsync()).is_active);
        Assert.True((await application.UpdateAssignmentStatus(1, centerId, 2, false)).Value);
    }

    private async Task SetSectionType(long typeId)
    {
        (await _context.Sections.SingleAsync()).solution_center_type_id = typeId;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task AssertNoAssignments()
    {
        Assert.Equal(1, await _context.Sections.CountAsync());
        Assert.Empty(await _context.SolutionCenterSections.ToListAsync());
        Assert.Empty(await _context.SolutionCenterProducts.ToListAsync());
        Assert.Empty(_audit);
    }

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

    private static CreateSectionConfigurationDto Request(long? sectionId, params (long Id, int Order)[] products) => new()
    {
        IdSection = sectionId,
        SectionName = sectionId.HasValue ? null : "Pasillo nuevo",
        Products = (products.Length == 0 ? new[] { (Id: 1L, Order: 1) } : products)
            .Select(product => new SectionProductDto { ProductId = product.Id, SortOrder = product.Order }).ToList()
    };

    private static long Success(IActionResult result)
    {
        var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(response.IsSuccess);
        Assert.Equal("Configuración de la sección guardada correctamente.", response.Message);
        return JsonSerializer.SerializeToElement(response.Result).GetProperty("SectionId").GetInt64();
    }

    private static ResponseApi Failure(IActionResult result)
    {
        var response = Assert.IsType<ResponseApi>(Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.False(response.IsSuccess);
        Assert.Empty(JsonSerializer.SerializeToElement(response.Result).EnumerateObject());
        return response;
    }

    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Reads { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class TestContext(DbContextOptions<SitraCalcoContext> options) : SitraCalcoContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Usar los mapeos de producción sólo de las entidades bajo prueba.
            var included = new[] { typeof(Product), typeof(Section), typeof(SolutionCenter),
                typeof(SolutionCenterType), typeof(SolutionCenterSection), typeof(SolutionCenterProduct),
                typeof(InventoryConfiguration), typeof(InventoryConfigurationAssignment) };
            foreach (var type in modelBuilder.Model.GetEntityTypes().Select(entity => entity.ClrType).ToList())
                if (!included.Contains(type))
                    modelBuilder.Ignore(type);
        }
    }
}
