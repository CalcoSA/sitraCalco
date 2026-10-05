using FluentValidation;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;

namespace Inventory.Test.Application
{
    public class PermissionVisibilityTests
    {
        private readonly List<Permission> _permissions = new()
        {
            new Permission { permission_id = 1, permission_key = "VIEW_ALL_SOLUTION_CENTERS", permission_value = "ADMINISTRADOR,COSTOS,CONTROL INTERNO" },
            new Permission { permission_id = 2, permission_key = "VIEW_WAREHOUSES_ONLY", permission_value = "ALMACEN" },
            new Permission { permission_id = 3, permission_key = "VIEW_FULL_INVENTORY_DETAIL", permission_value = "ADMINISTRADOR,COSTOS,CONTROL INTERNO,ALMACEN" },
            new Permission { permission_id = 4, permission_key = "VIEW_LIMITED_INVENTORY_DETAIL", permission_value = "AUXILIAR,PDV" }
        };
        private readonly Mock<ISolutionCenterRepository> _centers = new();
        private readonly Mock<IInventoryConfigurationRepository> _configurations = new();
        private readonly PermissionApplication _permissionApplication;

        public PermissionVisibilityTests()
        {
            var repository = new Mock<IPermissionRepository>();
            repository.Setup(x => x.GetByKey(It.IsAny<string>()))
                .ReturnsAsync((string key) => _permissions.FirstOrDefault(p => p.permission_key == key));
            _permissionApplication = new PermissionApplication(repository.Object);
        }

        private SolutionCenterController SolutionController(string role)
        {
            return new SolutionCenterController(
                new SolutionCenterApplication(_centers.Object, Mock.Of<IProductRepository>(), _permissionApplication),
                Mock.Of<ILogApplication>(), NullLogger<SolutionCenterController>.Instance)
                .WithIdentity(role: role);
        }

        private InventoryConfigurationController ConfigurationController(string role)
        {
            return new InventoryConfigurationController(
                new InventoryConfigurationApplication(_configurations.Object, _permissionApplication),
                Mock.Of<ILogApplication>(),
                Mock.Of<IValidator<CreateInventoryConfigurationDto>>(),
                Mock.Of<IValidator<CreateInventoryConfigurationAssignmentsDto>>(),
                Mock.Of<IValidator<UpdateInventoryConfigurationAssignmentStatusDto>>(),
                Mock.Of<IValidator<UpdateInventoryConfigurationDto>>(),
                Mock.Of<IValidator<AddInventoryConfigurationDaysDto>>(),
                NullLogger<InventoryConfigurationController>.Instance)
                .WithIdentity(role: role);
        }

        [Theory]
        [InlineData("ALMACEN", 1L)]
        [InlineData(" almacen ", 1L)]
        [InlineData("COSTOS", null)]
        [InlineData("Administrador", null)]
        [InlineData("control interno", null)]
        public async Task SolutionCenters_ShouldUseConfiguredVisibility_AndIgnoreSuppliedRoles(
            string role, long? expectedTypeId)
        {
            _centers.Setup(x => x.GetPagedSolutionCenters(1, 10, expectedTypeId))
                .ReturnsAsync(new PagedDto<SolutionCenterListDto>
                {
                    Items = new List<SolutionCenterListDto>()
                });
            var controller = SolutionController(role);
            controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");
            controller.Request.Headers["role"] = "ADMINISTRADOR";
            controller.Request.ContentType = "application/json";
            controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"role":"ADMINISTRADOR"}"""));

            Assert.IsType<OkObjectResult>(await controller.GetSolutionCenters());

            _centers.Verify(x => x.GetPagedSolutionCenters(1, 10, expectedTypeId), Times.Once);
            _centers.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Visibility_ShouldFollowChangedParameters_ForANewRole()
        {
            _permissions[0].permission_value = "ROL_NUEVO";
            _permissions[1].permission_value = "COSTOS";
            _centers.Setup(x => x.GetPagedSolutionCenters(1, 10, It.IsAny<long?>()))
                .ReturnsAsync(new PagedDto<SolutionCenterListDto> { Items = new List<SolutionCenterListDto>() });

            Assert.IsType<OkObjectResult>(await SolutionController("rol_nuevo").GetSolutionCenters());
            Assert.IsType<OkObjectResult>(await SolutionController("COSTOS").GetSolutionCenters());
            var denied = await SolutionController("ADMINISTRADOR").GetSolutionCenters();

            _centers.Verify(x => x.GetPagedSolutionCenters(1, 10, null), Times.Once);
            _centers.Verify(x => x.GetPagedSolutionCenters(1, 10, 1), Times.Once);
            Assert.Equal(403, Assert.IsType<ObjectResult>(denied).StatusCode);
        }

        [Theory]
        [InlineData("AUXILIAR")]
        [InlineData("PDV")]
        [InlineData("COSTO")]
        [InlineData("SIN_PERMISO")]
        public async Task Queries_ShouldReturn403_WhenRoleHasNoVisibilityPermission(string role)
        {
            var centersResult = await SolutionController(role).GetSolutionCenters();
            var controller = ConfigurationController(role);
            var byCenter = await controller.GetInventoryConfigurationsBySolutionCenterId(1);
            var byId = await controller.GetInventoryConfigurationById(1);

            Assert.Equal(403, Assert.IsType<ObjectResult>(centersResult).StatusCode);
            Assert.Equal(403, Assert.IsType<ObjectResult>(byCenter).StatusCode);
            Assert.Equal(403, Assert.IsType<ObjectResult>(byId).StatusCode);
            _centers.VerifyNoOtherCalls();
            _configurations.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FutureDetailPermissions_ShouldNotGrantVisibilityByThemselves()
        {
            _permissions.RemoveAll(p => p.permission_key == "VIEW_ALL_SOLUTION_CENTERS" ||
                p.permission_key == "VIEW_WAREHOUSES_ONLY");

            Assert.Equal(403, Assert.IsType<ObjectResult>(
                await SolutionController("ALMACEN").GetSolutionCenters()).StatusCode);
            var controller = ConfigurationController("ALMACEN");
            Assert.Equal(403, Assert.IsType<ObjectResult>(
                await controller.GetInventoryConfigurationsBySolutionCenterId(1)).StatusCode);
            Assert.Equal(403, Assert.IsType<ObjectResult>(
                await controller.GetInventoryConfigurationById(1)).StatusCode);
            _configurations.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("ALMACEN", 1L, 200)]
        [InlineData("ALMACEN", 2L, 403)]
        [InlineData("COSTOS", 1L, 200)]
        [InlineData("COSTOS", 2L, 200)]
        [InlineData("Administrador", 2L, 200)]
        public async Task ConfigurationsByCenter_ShouldEnforceConfiguredVisibility(
            string role, long typeId, int expectedStatus)
        {
            _configurations.Setup(x => x.GetInventoryConfigurationsBySolutionCenterId(1))
                .ReturnsAsync(new SolutionCenterInventoryConfigurationsDto
                {
                    SolutionCenterId = 1, SolutionCenterTypeId = typeId
                });
            var controller = ConfigurationController(role);
            controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");
            controller.Request.Headers["role"] = "ADMINISTRADOR";
            controller.Request.ContentType = "application/json";
            controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"role":"ADMINISTRADOR"}"""));

            var result = await controller.GetInventoryConfigurationsBySolutionCenterId(1);

            Assert.Equal(expectedStatus, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        }

        [Theory]
        [InlineData("ALMACEN", 1)]
        [InlineData("COSTOS", 2)]
        [InlineData("Administrador", 2)]
        public async Task ConfigurationDetails_ShouldFilterCenters_UsingVisibilityPermission(string role, int expectedCount)
        {
            ConfigureDetails();
            var controller = ConfigurationController(role);
            controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");
            controller.Request.Headers["role"] = "ADMINISTRADOR";
            controller.Request.ContentType = "application/json";
            controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"role":"ADMINISTRADOR"}"""));

            var result = await controller.GetInventoryConfigurationById(1);

            var response = Assert.IsType<Inventory.Domain.Responses.ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            var data = Assert.IsType<InventoryConfigurationByIdDto>(response.Result);
            Assert.Equal(expectedCount, data.SolutionCenters.Count);
            if (expectedCount == 1)
                Assert.Equal(1, data.SolutionCenters[0].SolutionCenterTypeId);
        }

        [Fact]
        public async Task AllCentersPermission_ShouldTakePrecedence_WhenBothPermissionsContainRole()
        {
            _permissions[0].permission_value += ",ALMACEN";
            _centers.Setup(x => x.GetPagedSolutionCenters(1, 10, null))
                .ReturnsAsync(new PagedDto<SolutionCenterListDto> { Items = new List<SolutionCenterListDto>() });
            _configurations.Setup(x => x.GetInventoryConfigurationsBySolutionCenterId(1))
                .ReturnsAsync(new SolutionCenterInventoryConfigurationsDto { SolutionCenterTypeId = 2 });
            ConfigureDetails();

            Assert.IsType<OkObjectResult>(await SolutionController("ALMACEN").GetSolutionCenters());
            var controller = ConfigurationController("ALMACEN");
            Assert.IsType<OkObjectResult>(await controller.GetInventoryConfigurationsBySolutionCenterId(1));
            var result = Assert.IsType<OkObjectResult>(await controller.GetInventoryConfigurationById(1));
            var response = Assert.IsType<Inventory.Domain.Responses.ResponseApi>(result.Value);
            Assert.Equal(2, Assert.IsType<InventoryConfigurationByIdDto>(response.Result).SolutionCenters.Count);
            _centers.Verify(x => x.GetPagedSolutionCenters(1, 10, null), Times.Once);
        }

        [Fact]
        public async Task ConfigurationQueries_ShouldPreserve404_ForMissingResources()
        {
            var controller = ConfigurationController("ALMACEN");

            Assert.IsType<NotFoundObjectResult>(await controller.GetInventoryConfigurationsBySolutionCenterId(1));
            Assert.IsType<NotFoundObjectResult>(await controller.GetInventoryConfigurationById(1));
        }

        private void ConfigureDetails()
        {
            _configurations.Setup(x => x.GetInventoryConfigurationById(1))
                .ReturnsAsync(new InventoryConfigurationByIdDto
                {
                    InventoryConfigurationId = 1,
                    SolutionCenters = new List<InventoryConfigurationSolutionCenterDto>
                    {
                        new() { SolutionCenterId = 1, SolutionCenterTypeId = 1 },
                        new() { SolutionCenterId = 2, SolutionCenterTypeId = 2 }
                    }
                });
        }
    }
}
