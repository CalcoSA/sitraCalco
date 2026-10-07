using Inventory.Api.Controllers;
using Inventory.Api.Extensions;
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
using System.Security.Claims;
using System.Text.Json;

namespace Inventory.Test.Controllers
{
    public class AuthenticatedClaimsTests
    {
        private static IPermissionApplication CreatePermissions()
        {
            var repository = new Mock<IPermissionRepository>();
            repository.Setup(x => x.GetByKey("VIEW_WAREHOUSES_ONLY"))
                .ReturnsAsync(new Permission
                {
                    permission_key = "VIEW_WAREHOUSES_ONLY",
                    permission_value = "ALMACEN"
                });
            return new PermissionApplication(repository.Object);
        }

        [Fact]
        public async Task GetSolutionCenters_ShouldFilterByClaim_WhenQueryContainsAnotherRole()
        {
            var repository = new Mock<ISolutionCenterRepository>(MockBehavior.Strict);
            repository.Setup(x => x.GetPagedSolutionCenters(1, 10, 1))
                .ReturnsAsync(new PagedDto<SolutionCenterListDto>
                {
                    Items = new List<SolutionCenterListDto>()
                });

            var application = new SolutionCenterApplication(
                repository.Object, Mock.Of<IProductRepository>(), CreatePermissions());
            var controller = new SolutionCenterController(
                application, Mock.Of<ILogApplication>(),
                NullLogger<SolutionCenterController>.Instance)
                .WithIdentity(role: "ALMACEN");
            controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");

            var result = await controller.GetSolutionCenters();

            Assert.IsType<OkObjectResult>(result);
            repository.Verify(x => x.GetPagedSolutionCenters(1, 10, 1), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetSolutionCenters_ShouldRejectMissingRole_EvenWhenQuerySuppliesIt(string? role)
        {
            var application = new Mock<ISolutionCenterApplication>(MockBehavior.Strict);
            var controller = new SolutionCenterController(
                application.Object, Mock.Of<ILogApplication>(),
                NullLogger<SolutionCenterController>.Instance)
                .WithIdentity(role: role);
            controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");

            var result = await controller.GetSolutionCenters();

            Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
            application.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AddProductToSection_ShouldUseClaim_ForPersistenceAndAudit()
        {
            var repository = new Mock<ISolutionCenterRepository>(MockBehavior.Strict);
            repository.Setup(x => x.SolutionCenterExists(1)).ReturnsAsync(true);
            repository.Setup(x => x.SectionBelongsToSolutionCenter(1, 2)).ReturnsAsync(true);
            repository.Setup(x => x.GetSectionProducts(1, 2))
                .ReturnsAsync(Array.Empty<Product>());
            repository.Setup(x => x.AddProductToSection(1, 2, 10, 1, "juan.zapata"))
                .ReturnsAsync(100);

            var products = new Mock<IProductRepository>(MockBehavior.Strict);
            products.Setup(x => x.GetProductById(10)).ReturnsAsync(new Product
            {
                product_id = 10,
                product_name = "Producto",
                unit_of_measure = "UND"
            });

            var logs = new Mock<ILogApplication>();
            var controller = new SolutionCenterController(
                new SolutionCenterApplication(repository.Object, products.Object, CreatePermissions()),
                logs.Object, NullLogger<SolutionCenterController>.Instance)
                .WithIdentity(userLogin: "  juan.zapata  ");
            controller.Request.Headers["X-User"] = "usuario-falso";
            controller.Request.QueryString = new QueryString("?userName=usuario-falso");

            var request = JsonSerializer.Deserialize<AddSectionProductDto>(
                """{"productId":10,"position":1,"createdBy":"usuario-falso"}""",
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

            var result = await controller.AddProductToSection(1, 2, request);

            Assert.IsType<OkObjectResult>(result);
            repository.Verify(x => x.AddProductToSection(1, 2, 10, 1, "juan.zapata"), Times.Once);
            logs.Verify(x => x.CreateLog(It.Is<CreateLogDto>(log =>
                log.UserName == "juan.zapata")), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SyncProducts_ShouldRejectMissingLogin_EvenWhenHeaderSuppliesIt(string? userLogin)
        {
            var application = new Mock<IProductApplication>(MockBehavior.Strict);
            var logs = new Mock<ILogApplication>(MockBehavior.Strict);
            var controller = new ProductController(
                application.Object, logs.Object, NullLogger<ProductController>.Instance,
                Microsoft.Extensions.Options.Options.Create(new Inventory.Domain.Options.GoogleCloudStorageOptions()))
                .WithIdentity(userLogin: userLogin);
            controller.Request.Headers["X-User"] = "usuario-falso";

            var result = await controller.SyncProducts();

            Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
            application.VerifyNoOtherCalls();
            logs.VerifyNoOtherCalls();
        }

        [Fact]
        public void Claims_ShouldNotBeRead_FromUnauthenticatedIdentity()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userLogin", "usuario-falso"),
                new Claim("nameRole", "ADMINISTRADOR")
            }));

            Assert.Null(user.GetUserLogin());
            Assert.Null(user.GetRoleName());
        }

        [Theory]
        [InlineData("userLogin")]
        [InlineData("nameRole")]
        public void Claims_ShouldRejectAmbiguousIdentity(string claimType)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(claimType, "valor-uno"),
                new Claim(claimType, "valor-dos")
            }, "Bearer"));

            var value = claimType == "userLogin" ? user.GetUserLogin() : user.GetRoleName();

            Assert.Null(value);
        }
    }
}
