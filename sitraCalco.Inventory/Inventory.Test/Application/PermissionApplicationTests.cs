using Inventory.Application.Services;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Moq;

namespace Inventory.Test.Application
{
    public class PermissionApplicationTests
    {
        private readonly Mock<IPermissionRepository> _repository = new();
        private readonly PermissionApplication _application;

        public PermissionApplicationTests()
        {
            _application = new PermissionApplication(_repository.Object);
        }

        [Theory]
        [InlineData("Administrador", true)]
        [InlineData(" costos ", true)]
        [InlineData("control interno", true)]
        [InlineData("COSTO", false)]
        [InlineData("CONTROL", false)]
        [InlineData("ALMACEN", false)]
        [InlineData("COSTOS,ALMACEN", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public async Task HasPermission_ShouldMatchWholeRoles_IgnoringCaseAndSpaces(string role, bool expected)
        {
            _repository.Setup(x => x.GetByKey("VIEW_ALL_SOLUTION_CENTERS"))
                .ReturnsAsync(new Permission
                {
                    permission_key = "VIEW_ALL_SOLUTION_CENTERS",
                    permission_value = " ADMINISTRADOR , Costos, CONTROL INTERNO,, "
                });

            var result = await _application.HasPermission(" view_all_solution_centers ", role);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task HasPermission_ShouldDeny_WhenKeyIsMissing()
        {
            Assert.False(await _application.HasPermission("UNKNOWN", "COSTOS"));
        }

        [Fact]
        public async Task HasPermission_ShouldUseCurrentRepositoryValues_WithoutCaching()
        {
            var permission = new Permission { permission_key = "VIEW_EXAMPLE", permission_value = "COSTOS" };
            _repository.Setup(x => x.GetByKey("VIEW_EXAMPLE")).ReturnsAsync(permission);

            Assert.True(await _application.HasPermission("VIEW_EXAMPLE", "COSTOS"));
            permission.permission_value = "ALMACEN";
            Assert.False(await _application.HasPermission("VIEW_EXAMPLE", "COSTOS"));
            Assert.True(await _application.HasPermission("VIEW_EXAMPLE", "ALMACEN"));
            _repository.Verify(x => x.GetByKey("VIEW_EXAMPLE"), Times.Exactly(3));
        }

        [Fact]
        public async Task GetPermissionsByRole_ShouldIncludeFutureDetailKeys_WithoutPartialMatches()
        {
            _repository.Setup(x => x.GetAll()).ReturnsAsync(new[]
            {
                new Permission { permission_id = 1, permission_key = "VIEW_ALL_SOLUTION_CENTERS", permission_value = "ADMINISTRADOR,COSTOS,CONTROL INTERNO" },
                new Permission { permission_id = 2, permission_key = "VIEW_WAREHOUSES_ONLY", permission_value = "ALMACEN" },
                new Permission { permission_id = 3, permission_key = "VIEW_FULL_INVENTORY_DETAIL", permission_value = "ADMINISTRADOR,COSTOS,CONTROL INTERNO,ALMACEN" },
                new Permission { permission_id = 4, permission_key = "VIEW_LIMITED_INVENTORY_DETAIL", permission_value = "AUXILIAR,PDV" },
                new Permission { permission_id = 5, permission_key = "OTHER", permission_value = "ALMACENISTA" }
            });

            var warehousePermissions = (await _application.GetPermissionsByRole(" almacen ")).ToList();
            var limitedPermissions = (await _application.GetPermissionsByRole("pdv")).ToList();

            Assert.Equal(new long[] { 2, 3 }, warehousePermissions.Select(x => x.PermissionId));
            Assert.Equal("VIEW_LIMITED_INVENTORY_DETAIL", Assert.Single(limitedPermissions).PermissionKey);
            Assert.Empty(await _application.GetPermissionsByRole("ALMA"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetPermissionsByRole_ShouldNotReadRepository_ForEmptyRole(string role)
        {
            Assert.Empty(await _application.GetPermissionsByRole(role));
            _repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetByKey_ShouldNormalizeKey()
        {
            _repository.Setup(x => x.GetByKey("VIEW_EXAMPLE")).ReturnsAsync(new Permission
            {
                permission_id = 7, permission_key = "VIEW_EXAMPLE", permission_value = "COSTOS"
            });

            var permission = await _application.GetByKey(" view_example ");

            Assert.NotNull(permission);
            Assert.Equal(7, permission.PermissionId);
            _repository.Verify(x => x.GetByKey("VIEW_EXAMPLE"), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldNormalizeKey_AndDeduplicateRoles()
        {
            _repository.Setup(x => x.CreatePermission(It.IsAny<Permission>())).ReturnsAsync(10);

            var id = await _application.Create(new CreatePermissionDto
            {
                PermissionKey = " view_example ",
                PermissionValue = " costos, ALMACEN ,costos ,, Administrador,administrador "
            });

            Assert.Equal(10, id);
            _repository.Verify(x => x.ExistsByKey("VIEW_EXAMPLE", null), Times.Once);
            _repository.Verify(x => x.CreatePermission(It.Is<Permission>(p =>
                p.permission_key == "VIEW_EXAMPLE" &&
                p.permission_value == "COSTOS,ALMACEN,ADMINISTRADOR")), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldRejectDuplicatedKey()
        {
            _repository.Setup(x => x.ExistsByKey("VIEW_EXAMPLE", null)).ReturnsAsync(true);

            var id = await _application.Create(new CreatePermissionDto
            {
                PermissionKey = "view_example", PermissionValue = "COSTOS"
            });

            Assert.Equal(0, id);
            _repository.Verify(x => x.CreatePermission(It.IsAny<Permission>()), Times.Never);
        }

        [Fact]
        public async Task Update_ShouldNormalizeAndExcludeCurrentRecord_FromDuplicateCheck()
        {
            _repository.Setup(x => x.GetById(3)).ReturnsAsync(new Permission { permission_id = 3 });
            _repository.Setup(x => x.UpdatePermission(It.IsAny<Permission>())).ReturnsAsync(true);

            var updated = await _application.Update(3, new UpdatePermissionDto
            {
                PermissionKey = " view_example ", PermissionValue = " costos, ALMACEN ,costos "
            });

            Assert.True(updated);
            _repository.Verify(x => x.ExistsByKey("VIEW_EXAMPLE", 3), Times.Once);
            _repository.Verify(x => x.UpdatePermission(It.Is<Permission>(p =>
                p.permission_id == 3 && p.permission_key == "VIEW_EXAMPLE" &&
                p.permission_value == "COSTOS,ALMACEN")), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldRejectKeyOwnedByAnotherPermission()
        {
            _repository.Setup(x => x.GetById(3)).ReturnsAsync(new Permission { permission_id = 3 });
            _repository.Setup(x => x.ExistsByKey("OTHER", 3)).ReturnsAsync(true);

            Assert.False(await _application.Update(3, new UpdatePermissionDto
            {
                PermissionKey = " other ", PermissionValue = "COSTOS"
            }));
            _repository.Verify(x => x.UpdatePermission(It.IsAny<Permission>()), Times.Never);
        }

        [Fact]
        public async Task Update_ShouldReturnFalse_WhenMissing()
        {
            Assert.False(await _application.Update(3, new UpdatePermissionDto
            {
                PermissionKey = "VIEW_EXAMPLE", PermissionValue = "COSTOS"
            }));
            _repository.Verify(x => x.UpdatePermission(It.IsAny<Permission>()), Times.Never);
        }

        [Fact]
        public async Task Mutations_ShouldPropagateRepositoryDuplicateResults()
        {
            _repository.Setup(x => x.GetById(3)).ReturnsAsync(new Permission { permission_id = 3 });
            _repository.Setup(x => x.CreatePermission(It.IsAny<Permission>())).ReturnsAsync(0);
            _repository.Setup(x => x.UpdatePermission(It.IsAny<Permission>())).ReturnsAsync(false);

            Assert.Equal(0, await _application.Create(new CreatePermissionDto
            {
                PermissionKey = "VIEW_EXAMPLE", PermissionValue = "COSTOS"
            }));
            Assert.False(await _application.Update(3, new UpdatePermissionDto
            {
                PermissionKey = "VIEW_EXAMPLE", PermissionValue = "COSTOS"
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Delete_ShouldReturnRepositoryResult(bool exists)
        {
            _repository.Setup(x => x.DeletePermission(3)).ReturnsAsync(exists);

            Assert.Equal(exists, await _application.Delete(3));
        }

        [Theory]
        [InlineData(null, "COSTOS")]
        [InlineData(" ", "COSTOS")]
        [InlineData("VIEW_EXAMPLE", null)]
        [InlineData("VIEW_EXAMPLE", " , , ")]
        public async Task Mutations_ShouldRejectEmptyNormalizedInput(string? key, string? value)
        {
            Assert.Equal(0, await _application.Create(new CreatePermissionDto
            {
                PermissionKey = key, PermissionValue = value
            }));
            Assert.False(await _application.Update(3, new UpdatePermissionDto
            {
                PermissionKey = key, PermissionValue = value
            }));
            _repository.VerifyNoOtherCalls();
        }
    }
}
