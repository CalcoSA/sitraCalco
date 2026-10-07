using Inventory.Test.Helpers;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Models;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Inventory.Domain.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Inventory.Test.Controllers
{
    public class ProductControllerTests
    {
        private readonly Mock<IProductApplication>
            _productApplicationMock;

        private readonly Mock<ILogApplication>
            _logApplicationMock;

        private readonly Mock<ILogger<ProductController>>
            _loggerMock;

        private readonly ProductController
            _controller;

        public ProductControllerTests()
        {
            _productApplicationMock =
                new Mock<IProductApplication>();

            _logApplicationMock =
                new Mock<ILogApplication>();

            _loggerMock =
                new Mock<ILogger<ProductController>>();

            _controller =
                new ProductController(
                    _productApplicationMock.Object,
                    _logApplicationMock.Object,
                    _loggerMock.Object,
                    Options.Create(new GoogleCloudStorageOptions()));
        }

        // =========================================================
        // PRODUCT IMAGES — Controller HTTP contract only
        // =========================================================

        private static IFormFile CreateImageFile(string fileName = "producto.jpg", string contentType = "image/jpeg", long length = 3)
        {
            var file = new Mock<IFormFile>();
            file.SetupGet(x => x.FileName).Returns(fileName);
            file.SetupGet(x => x.ContentType).Returns(contentType);
            file.SetupGet(x => x.Length).Returns(length);
            file.Setup(x => x.OpenReadStream()).Returns(() => new MemoryStream(new byte[] { 1, 2, 3 }));
            return file.Object;
        }

        private Task<IActionResult> WriteImage(bool replace, long productId, IFormFile? file)
        {
            return replace ? _controller.ReplaceImage(productId, file) : _controller.UploadImage(productId, file);
        }

        private void SetupImageWrite(bool replace, ProductImageResultDto result)
        {
            if (replace)
                _productApplicationMock.Setup(x => x.ReplaceImage(10, It.IsAny<Stream>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(result);
            else
                _productApplicationMock.Setup(x => x.UploadImage(10, It.IsAny<Stream>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(result);
        }

        private static ResponseApi AssertImageError(IActionResult result, int statusCode, string message)
        {
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(statusCode, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(message, response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
            return response;
        }

        [Theory]
        [InlineData("post", 0)]
        [InlineData("post", -1)]
        [InlineData("put", 0)]
        [InlineData("put", -1)]
        [InlineData("get", 0)]
        [InlineData("get", -1)]
        public async Task Image_ShouldReturn400_WhenProductIdIsInvalid(string operation, long productId)
        {
            _controller.WithIdentity();
            var result = operation == "get"
                ? await _controller.GetImage(productId)
                : await WriteImage(operation == "put", productId, CreateImageFile());

            AssertImageError(result, 400, "El identificador del producto debe ser mayor a cero.");
            _productApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WriteImage_ShouldReturn400_WhenFileIsMissing(bool replace)
        {
            _controller.WithIdentity();
            AssertImageError(await WriteImage(replace, 10, null), 400, "Debe enviar una imagen en el campo file.");
            _productApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false, "imagen.jpg", "image/jpeg", 0, "El archivo de imagen está vacío.")]
        [InlineData(true, "imagen.jpg", "image/jpeg", 0, "El archivo de imagen está vacío.")]
        [InlineData(false, "imagen.jpg", "image/jpeg", 5242881, "La imagen no puede superar 5 MB.")]
        [InlineData(true, "imagen.jpg", "image/jpeg", 5242881, "La imagen no puede superar 5 MB.")]
        [InlineData(false, "imagen.svg", "image/svg+xml", 3, "Solo se permiten imágenes .jpg, .jpeg, .png y .webp.")]
        [InlineData(true, "imagen.gif", "image/gif", 3, "Solo se permiten imágenes .jpg, .jpeg, .png y .webp.")]
        [InlineData(false, "imagen.exe", "image/jpeg", 3, "Solo se permiten imágenes .jpg, .jpeg, .png y .webp.")]
        [InlineData(true, "imagen.pdf", "application/pdf", 3, "Solo se permiten imágenes .jpg, .jpeg, .png y .webp.")]
        [InlineData(false, "imagen.jpg", "application/pdf", 3, "El Content-Type no es válido para la extensión de la imagen.")]
        [InlineData(true, "imagen.png", "image/jpeg", 3, "El Content-Type no es válido para la extensión de la imagen.")]
        [InlineData(false, "imagen.webp", "", 3, "El Content-Type no es válido para la extensión de la imagen.")]
        [InlineData(true, "imagen.jpg.exe", "image/jpeg", 3, "Solo se permiten imágenes .jpg, .jpeg, .png y .webp.")]
        public async Task WriteImage_ShouldReturn400_WhenFileIsInvalid(
            bool replace, string fileName, string contentType, long length, string expectedMessage)
        {
            _controller.WithIdentity();
            AssertImageError(await WriteImage(replace, 10, CreateImageFile(fileName, contentType, length)), 400, expectedMessage);
            _productApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false, ProductImageStatus.ProductNotFound, 404, "El producto no existe.")]
        [InlineData(true, ProductImageStatus.ProductNotFound, 404, "El producto no existe.")]
        [InlineData(false, ProductImageStatus.ImageAlreadyExists, 400, "El producto ya tiene una imagen. Utilice PUT para reemplazarla.")]
        [InlineData(false, ProductImageStatus.Conflict, 409, "La imagen del producto cambió durante la carga. Consulte el producto e intente nuevamente.")]
        [InlineData(true, ProductImageStatus.Conflict, 409, "La imagen del producto cambió durante la carga. Consulte el producto e intente nuevamente.")]
        public async Task WriteImage_ShouldMapApplicationFailure(bool replace, ProductImageStatus status, int httpStatus, string message)
        {
            _controller.WithIdentity();
            SetupImageWrite(replace, new ProductImageResultDto { Status = status });
            AssertImageError(await WriteImage(replace, 10, CreateImageFile()), httpStatus, message);
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false, "imagen.JPG", "image/jpeg")]
        [InlineData(false, "imagen.jpeg", "image/jpeg")]
        [InlineData(true, "imagen.png", "image/png")]
        [InlineData(true, "imagen.webp", "image/webp")]
        public async Task WriteImage_ShouldReturn200AndForwardFileAndClaim(bool replace, string fileName, string contentType)
        {
            _controller.WithIdentity(userLogin: "juan.zapata");
            _controller.Request.Headers["X-User"] = "otro";
            _controller.Request.QueryString = new QueryString("?userLogin=otro&user=otro");
            _controller.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["userLogin"] = "otro", ["user"] = "otro"
            });
            var data = new ProductImageDto { ProductId = 10, ImagePath = "sitracalco_inventory_dev/image/products/10/nueva.jpg" };
            SetupImageWrite(replace, new ProductImageResultDto { Status = ProductImageStatus.Success, Data = data });

            // Exactamente el límite de 5 MB está permitido; Application está simulada y no sube archivos.
            var result = await WriteImage(replace, 10, CreateImageFile(fileName, contentType, 5242880));

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.IsSuccess);
            Assert.Same(data, response.Result);
            Assert.Equal(replace ? "Imagen del producto actualizada correctamente." : "Imagen del producto cargada correctamente.", response.Message);
            if (replace)
                _productApplicationMock.Verify(x => x.ReplaceImage(10, It.IsAny<Stream>(), fileName, contentType, 5242880, "juan.zapata"), Times.Once);
            else
                _productApplicationMock.Verify(x => x.UploadImage(10, It.IsAny<Stream>(), fileName, contentType, 5242880, "juan.zapata"), Times.Once);
            _productApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(false, "")]
        [InlineData(false, "   ")]
        [InlineData(true, null)]
        [InlineData(true, "")]
        [InlineData(true, "   ")]
        public async Task WriteImage_ShouldReturn403_WhenUserLoginClaimIsMissing(bool replace, string? userLogin)
        {
            _controller.WithIdentity(userLogin: userLogin);
            _controller.Request.Headers["X-User"] = "otro";
            _controller.Request.QueryString = new QueryString("?userLogin=otro");
            _controller.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["userLogin"] = "otro" });

            AssertImageError(await WriteImage(replace, 10, CreateImageFile()), 403, "El token no contiene un userLogin válido.");
            _productApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ProductImageStatus.ProductNotFound, "El producto no existe.")]
        [InlineData(ProductImageStatus.ImageNotFound, "El producto no tiene una imagen.")]
        public async Task GetImage_ShouldReturn404_WhenProductOrImageIsMissing(ProductImageStatus status, string message)
        {
            _productApplicationMock.Setup(x => x.GetImage(10)).ReturnsAsync(new ProductImageResultDto { Status = status });
            AssertImageError(await _controller.GetImage(10), 404, message);
        }

        [Fact]
        public async Task GetImage_ShouldReturnSignedUrlAndExpirationFromApplication()
        {
            var data = new ProductImageDto
            {
                ProductId = 10,
                ImagePath = "sitracalco_inventory_dev/image/products/10/imagen.jpg",
                ImageUrl = "https://storage.googleapis.com/example",
                ExpiresAt = new DateTimeOffset(2026, 10, 1, 12, 15, 0, TimeSpan.Zero)
            };
            _productApplicationMock.Setup(x => x.GetImage(10))
                .ReturnsAsync(new ProductImageResultDto { Status = ProductImageStatus.Success, Data = data });

            var result = await _controller.GetImage(10);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.IsSuccess);
            Assert.Equal("Imagen del producto consultada correctamente.", response.Message);
            Assert.Same(data, response.Result);
            _productApplicationMock.Verify(x => x.GetImage(10), Times.Once);
            _productApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("post")]
        [InlineData("put")]
        [InlineData("get")]
        public async Task Image_ShouldReturn500_WhenApplicationThrows(string operation)
        {
            _controller.WithIdentity();
            var exception = new InvalidOperationException("Detalle interno que no debe exponerse.");
            _productApplicationMock.Setup(x => x.GetImage(10)).ThrowsAsync(exception);
            _productApplicationMock.Setup(x => x.UploadImage(10, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>())).ThrowsAsync(exception);
            _productApplicationMock.Setup(x => x.ReplaceImage(10, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>())).ThrowsAsync(exception);

            var result = operation == "get" ? await _controller.GetImage(10) : await WriteImage(operation == "put", 10, CreateImageFile());

            AssertImageError(result, 500, operation == "get"
                ? "Ocurrió un error al consultar la imagen del producto."
                : "Ocurrió un error al guardar la imagen del producto.");
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task DeleteImage_ShouldReturn400_WhenProductIdIsInvalid(long productId)
        {
            _controller.WithIdentity();

            AssertImageError(await _controller.DeleteImage(productId), 400,
                "El identificador del producto debe ser mayor a cero.");
            _productApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ProductImageStatus.ProductNotFound, "El producto no existe.")]
        [InlineData(ProductImageStatus.ImageNotFound, "El producto no tiene una imagen.")]
        public async Task DeleteImage_ShouldReturn404_WhenProductOrImageIsMissing(ProductImageStatus status, string message)
        {
            _controller.WithIdentity();
            _productApplicationMock.Setup(x => x.DeleteImage(10, "juan.zapata"))
                .ReturnsAsync(new ProductImageResultDto { Status = status });

            AssertImageError(await _controller.DeleteImage(10), 404, message);
            _productApplicationMock.Verify(x => x.DeleteImage(10, "juan.zapata"), Times.Once);
            _productApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteImage_ShouldReturn403_WhenUserLoginClaimIsMissingOrEmpty(string? userLogin)
        {
            _controller.WithIdentity(userLogin: userLogin);
            _controller.Request.Headers["X-User"] = "otro";
            _controller.Request.QueryString = new QueryString("?userLogin=otro&user=otro");

            AssertImageError(await _controller.DeleteImage(10), 403,
                "El token no contiene un userLogin válido.");
            _productApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteImage_ShouldReturn200WithProductId_AndUseOnlyUserLoginClaim()
        {
            _controller.WithIdentity(userLogin: " juan.zapata ");
            _controller.Request.Headers["X-User"] = "otro";
            _controller.Request.QueryString = new QueryString("?userLogin=otro&user=otro");
            _controller.Request.ContentType = "application/json";
            _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                """{"userLogin":"otro","user":"otro"}"""));
            _productApplicationMock.Setup(x => x.DeleteImage(10, "juan.zapata"))
                .ReturnsAsync(new ProductImageResultDto { Status = ProductImageStatus.Success });

            var result = await _controller.DeleteImage(10);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.IsSuccess);
            Assert.Equal("Imagen del producto eliminada correctamente.", response.Message);
            var property = Assert.Single(response.Result.GetType().GetProperties());
            Assert.Equal("ProductId", property.Name);
            Assert.Equal(10L, property.GetValue(response.Result));
            _productApplicationMock.Verify(x => x.DeleteImage(10, "juan.zapata"), Times.Once);
            _productApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteImage_ShouldReturn500_WhenApplicationThrows()
        {
            _controller.WithIdentity();
            _productApplicationMock.Setup(x => x.DeleteImage(10, "juan.zapata"))
                .ThrowsAsync(new InvalidOperationException("Detalle interno que no debe exponerse."));

            AssertImageError(await _controller.DeleteImage(10), 500,
                "Ocurrió un error al eliminar la imagen del producto.");
            _productApplicationMock.Verify(x => x.DeleteImage(10, "juan.zapata"), Times.Once);
            _productApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        // =========================================================
        // SYNC PRODUCTS
        // =========================================================

        [Fact]
        public async Task SyncProducts_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            // Act
            var result =
                await _controller.WithIdentity(userLogin: "")
                    .SyncProducts();

            // Assert
            var forbidden =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    forbidden.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "El token no contiene un userLogin válido.",
                response.Message);

            _productApplicationMock.Verify(
                x => x.SyncProducts(),
                Times.Never);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncProducts_ShouldReturnOkWithIsSuccessFalse_WhenNoProductsAreProcessed()
        {
            // Arrange
            ConfigureSyncProductsResult(
                processed: 0,
                created: 0,
                updated: 0,
                unchanged: 0);

            // Act
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .SyncProducts();

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "No se encontraron productos válidos para sincronizar.",
                response.Message);

            _productApplicationMock.Verify(
                x => x.SyncProducts(),
                Times.Once);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncProducts_ShouldReturnOk_WhenProductsAreProcessed()
        {
            // Arrange
            ConfigureSyncProductsResult(
                processed: 100,
                created: 5,
                updated: 3,
                unchanged: 92);

            // Act
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .SyncProducts();

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Productos sincronizados correctamente.",
                response.Message);

            _productApplicationMock.Verify(
                x => x.SyncProducts(),
                Times.Once);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Sincronizar" &&
                        log.Module == "Productos" &&
                        log.UserName == "juan.zapata" &&
                        log.Description.Contains(
                            "Procesados: 100") &&
                        log.Description.Contains(
                            "nuevos: 5") &&
                        log.Description.Contains(
                            "actualizados: 3") &&
                        log.Description.Contains(
                            "sin cambios: 92"))),
                Times.Once);
        }

        [Fact]
        public async Task SyncProducts_ShouldTrimUserName_WhenCreatingLog()
        {
            // Arrange
            ConfigureSyncProductsResult(
                processed: 10,
                created: 0,
                updated: 0,
                unchanged: 10);

            // Act
            var result =
                await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                    .SyncProducts();

            // Assert
            Assert.IsType<OkObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.UserName ==
                        "juan.zapata")),
                Times.Once);
        }

        [Fact]
        public async Task SyncProducts_ShouldCreateDetailedLogs_ForCreatedAndUpdatedProducts()
        {
            // Arrange
            ConfigureSyncProductsResult(
                processed: 2,
                created: 1,
                updated: 1,
                unchanged: 0,
                addCreatedProduct: true,
                addUpdatedProduct: true);

            // Act
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .SyncProducts();

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(response.IsSuccess);

            // Log general de sincronización.
            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Sincronizar" &&
                        log.Module == "Productos")),
                Times.Once);

            // Log del producto creado.
            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module == "Productos" &&
                        log.Description.Contains(
                            "Producto Creado") &&
                        log.Description.Contains(
                            "0001001"))),
                Times.Once);

            // Log del producto actualizado.
            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Actualizar" &&
                        log.Module == "Productos" &&
                        log.Description.Contains(
                            "Producto Actualizado") &&
                        log.Description.Contains(
                            "0001002"))),
                Times.Once);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Exactly(3));
        }

        [Fact]
        public async Task SyncProducts_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            _productApplicationMock
                .Setup(x =>
                    x.SyncProducts())
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .SyncProducts();

            // Assert
            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    objectResult.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "Ocurrió un error al sincronizar los productos desde SIESA.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        // =========================================================
        // GET PAGED
        // =========================================================

        [Fact]
        public async Task GetPaged_ShouldReturnBadRequest_WhenPageIsZero()
        {
            // Act
            var result =
                await _controller.GetPaged(
                    0,
                    10,
                    null);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "El número de página debe ser mayor a cero.",
                response.Message);

            _productApplicationMock.Verify(
                x => x.GetPaged(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPaged_ShouldReturnBadRequest_WhenPageIsNegative()
        {
            // Act
            var result =
                await _controller.GetPaged(
                    -1,
                    10,
                    null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(
                result);

            _productApplicationMock.Verify(
                x => x.GetPaged(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPaged_ShouldReturnBadRequest_WhenTakeIsZero()
        {
            // Act
            var result =
                await _controller.GetPaged(
                    1,
                    0,
                    null);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "La cantidad de registros debe ser mayor a cero.",
                response.Message);

            _productApplicationMock.Verify(
                x => x.GetPaged(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPaged_ShouldReturnBadRequest_WhenTakeIsNegative()
        {
            // Act
            var result =
                await _controller.GetPaged(
                    1,
                    -1,
                    null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task GetPaged_ShouldReturnOkWithIsSuccessFalse_WhenNoProductsExist()
        {
            // Arrange
            var products =
                new PagedDto<Product>
                {
                    Items =
                        new List<Product>(),

                    Total = 0,

                    Page = 1,

                    Take = 10,

                    Pages = 0
                };

            _productApplicationMock
                .Setup(x =>
                    x.GetPaged(
                        1,
                        10,
                        null))
                .ReturnsAsync(products);

            // Act
            var result =
                await _controller.GetPaged(
                    1,
                    10,
                    null);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "No se encontraron productos.",
                response.Message);

            Assert.Same(
                products,
                response.Result);
        }

        [Fact]
        public async Task GetPaged_ShouldReturnOk_WhenProductsExist()
        {
            // Arrange
            var products =
                new PagedDto<Product>
                {
                    Items =
                        new List<Product>
                        {
                            new Product()
                        },

                    Total = 1,

                    Page = 1,

                    Take = 10,

                    Pages = 1
                };

            _productApplicationMock
                .Setup(x =>
                    x.GetPaged(
                        1,
                        10,
                        null))
                .ReturnsAsync(products);

            // Act
            var result =
                await _controller.GetPaged(
                    1,
                    10,
                    null);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Productos consultados correctamente.",
                response.Message);

            Assert.Same(
                products,
                response.Result);
        }

        [Fact]
        public async Task GetPaged_ShouldSendCorrectParametersToApplication()
        {
            // Arrange
            const int page = 3;
            const int take = 25;
            const string search = "aceite";

            var products =
                new PagedDto<Product>
                {
                    Items =
                        new List<Product>
                        {
                            new Product()
                        },

                    Total = 1,

                    Page = page,

                    Take = take,

                    Pages = 1
                };

            _productApplicationMock
                .Setup(x =>
                    x.GetPaged(
                        page,
                        take,
                        search))
                .ReturnsAsync(products);

            // Act
            await _controller.GetPaged(
                page,
                take,
                search);

            // Assert
            _productApplicationMock.Verify(
                x => x.GetPaged(
                    page,
                    take,
                    search),
                Times.Once);
        }

        [Fact]
        public async Task GetPaged_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            _productApplicationMock
                .Setup(x =>
                    x.GetPaged(
                        1,
                        10,
                        "aceite"))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.GetPaged(
                    1,
                    10,
                    "aceite");

            // Assert
            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    objectResult.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "Ocurrió un error al consultar los productos.",
                response.Message);
        }

        // =========================================================
        // SEARCH PRODUCTS
        // =========================================================

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public async Task SearchProducts_ShouldReturnBadRequest_WhenSearchIsEmpty(
            string search)
        {
            // Act
            var result =
                await _controller.SearchProducts(
                    search,
                    20);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "Debe ingresar una referencia o nombre de producto.",
                response.Message);

            _productApplicationMock.Verify(
                x => x.SearchProducts(
                    It.IsAny<string>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("1")]
        [InlineData(" x ")]
        public async Task SearchProducts_ShouldReturnBadRequest_WhenSearchHasLessThanTwoCharacters(
            string search)
        {
            // Act
            var result =
                await _controller.SearchProducts(
                    search,
                    20);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "La búsqueda debe contener al menos 2 caracteres.",
                response.Message);

            _productApplicationMock.Verify(
                x => x.SearchProducts(
                    It.IsAny<string>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchProducts_ShouldUseDefaultTake20_WhenTakeIsZero()
        {
            // Arrange
            var products =
                new List<ProductOptionDto>
                {
                    new ProductOptionDto()
                };

            _productApplicationMock
                .Setup(x =>
                    x.SearchProducts(
                        "aceite",
                        20))
                .ReturnsAsync(
                    (IEnumerable<ProductOptionDto>)
                    products);

            // Act
            var result =
                await _controller.SearchProducts(
                    "aceite",
                    0);

            // Assert
            Assert.IsType<OkObjectResult>(
                result);

            _productApplicationMock.Verify(
                x => x.SearchProducts(
                    "aceite",
                    20),
                Times.Once);
        }

        [Fact]
        public async Task SearchProducts_ShouldUseDefaultTake20_WhenTakeIsNegative()
        {
            // Arrange
            var products =
                new List<ProductOptionDto>
                {
                    new ProductOptionDto()
                };

            _productApplicationMock
                .Setup(x =>
                    x.SearchProducts(
                        "aceite",
                        20))
                .ReturnsAsync(
                    (IEnumerable<ProductOptionDto>)
                    products);

            // Act
            await _controller.SearchProducts(
                "aceite",
                -10);

            // Assert
            _productApplicationMock.Verify(
                x => x.SearchProducts(
                    "aceite",
                    20),
                Times.Once);
        }

        [Fact]
        public async Task SearchProducts_ShouldLimitTakeTo50_WhenTakeIsGreaterThan50()
        {
            // Arrange
            var products =
                new List<ProductOptionDto>
                {
                    new ProductOptionDto()
                };

            _productApplicationMock
                .Setup(x =>
                    x.SearchProducts(
                        "aceite",
                        50))
                .ReturnsAsync(
                    (IEnumerable<ProductOptionDto>)
                    products);

            // Act
            var result =
                await _controller.SearchProducts(
                    "aceite",
                    100);

            // Assert
            Assert.IsType<OkObjectResult>(
                result);

            _productApplicationMock.Verify(
                x => x.SearchProducts(
                    "aceite",
                    50),
                Times.Once);
        }

        [Fact]
        public async Task SearchProducts_ShouldReturnOkWithIsSuccessFalse_WhenNoProductsExist()
        {
            // Arrange
            var products =
                new List<ProductOptionDto>();

            _productApplicationMock
                .Setup(x =>
                    x.SearchProducts(
                        "aceite",
                        20))
                .ReturnsAsync(
                    (IEnumerable<ProductOptionDto>)
                    products);

            // Act
            var result =
                await _controller.SearchProducts(
                    "aceite",
                    20);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "No se encontraron productos.",
                response.Message);
        }

        [Fact]
        public async Task SearchProducts_ShouldReturnOk_WhenProductsExist()
        {
            // Arrange
            var products =
                new List<ProductOptionDto>
                {
                    new ProductOptionDto()
                };

            _productApplicationMock
                .Setup(x =>
                    x.SearchProducts(
                        "aceite",
                        20))
                .ReturnsAsync(
                    (IEnumerable<ProductOptionDto>)
                    products);

            // Act
            var result =
                await _controller.SearchProducts(
                    "aceite",
                    20);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Productos consultados correctamente.",
                response.Message);

            var resultProducts =
                Assert.IsType<List<ProductOptionDto>>(
                    response.Result);

            Assert.Single(
                resultProducts);
        }

        [Fact]
        public async Task SearchProducts_ShouldSendCorrectParametersToApplication()
        {
            // Arrange
            const string search =
                "0001904";

            const int take =
                15;

            var products =
                new List<ProductOptionDto>
                {
                    new ProductOptionDto()
                };

            _productApplicationMock
                .Setup(x =>
                    x.SearchProducts(
                        search,
                        take))
                .ReturnsAsync(
                    (IEnumerable<ProductOptionDto>)
                    products);

            // Act
            await _controller.SearchProducts(
                search,
                take);

            // Assert
            _productApplicationMock.Verify(
                x => x.SearchProducts(
                    search,
                    take),
                Times.Once);
        }

        [Fact]
        public async Task SearchProducts_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            _productApplicationMock
                .Setup(x =>
                    x.SearchProducts(
                        "aceite",
                        20))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.SearchProducts(
                    "aceite",
                    20);

            // Assert
            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    objectResult.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "Ocurrió un error al buscar los productos.",
                response.Message);
        }

        // =========================================================
        // HELPERS PARA EL RESULTADO DE SINCRONIZACIÓN
        // =========================================================

        /// <summary>
        /// Configura el valor retornado por SyncProducts usando
        /// el tipo real declarado actualmente en IProductApplication.
        ///
        /// Esto evita acoplar las pruebas al nombre concreto del DTO
        /// de sincronización.
        /// </summary>
        private void ConfigureSyncProductsResult(
            int processed,
            int created,
            int updated,
            int unchanged,
            bool addCreatedProduct = false,
            bool addUpdatedProduct = false)
        {
            var syncMethod =
                typeof(IProductApplication)
                    .GetMethod(
                        nameof(
                            IProductApplication.SyncProducts));

            if (syncMethod is null)
            {
                throw new InvalidOperationException(
                    "No se encontró IProductApplication.SyncProducts.");
            }

            var taskType =
                syncMethod.ReturnType;

            if (!taskType.IsGenericType)
            {
                throw new InvalidOperationException(
                    "SyncProducts debe retornar Task<T>.");
            }

            var resultType =
                taskType
                    .GetGenericArguments()[0];

            var syncResult =
                Activator.CreateInstance(
                    resultType);

            if (syncResult is null)
            {
                throw new InvalidOperationException(
                    "No fue posible crear el resultado de SyncProducts.");
            }

            SetProperty(
                syncResult,
                "Processed",
                processed);

            SetProperty(
                syncResult,
                "Created",
                created);

            SetProperty(
                syncResult,
                "Updated",
                updated);

            SetProperty(
                syncResult,
                "Unchanged",
                unchanged);

            // Los foreach del Controller esperan
            // colecciones no nulas.
            EnsureCollection(
                syncResult,
                "CreatedProducts");

            EnsureCollection(
                syncResult,
                "UpdatedProducts");

            if (addCreatedProduct)
            {
                AddSyncProduct(
                    syncResult,
                    "CreatedProducts",
                    "Producto Creado",
                    "0001001",
                    "x 1 Unidad",
                    "FAC");
            }

            if (addUpdatedProduct)
            {
                AddSyncProduct(
                    syncResult,
                    "UpdatedProducts",
                    "Producto Actualizado",
                    "0001002",
                    "x 2 Unidades",
                    "ECL");
            }

            var fromResultMethod =
                typeof(Task)
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Static)
                    .Single(method =>
                        method.Name ==
                            nameof(Task.FromResult)
                        &&
                        method.IsGenericMethodDefinition);

            var task =
                fromResultMethod
                    .MakeGenericMethod(
                        resultType)
                    .Invoke(
                        null,
                        new[]
                        {
                            syncResult
                        });

            if (task is null)
            {
                throw new InvalidOperationException(
                    "No fue posible crear el Task de SyncProducts.");
            }

            var setReturnsDefault =
                typeof(Mock<IProductApplication>)
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Instance)
                    .Single(method =>
                        method.Name ==
                            "SetReturnsDefault"
                        &&
                        method.IsGenericMethodDefinition);

            setReturnsDefault
                .MakeGenericMethod(
                    taskType)
                .Invoke(
                    _productApplicationMock,
                    new[]
                    {
                        task
                    });
        }

        private static void AddSyncProduct(
            object syncResult,
            string collectionPropertyName,
            string productName,
            string reference,
            string unitOfMeasure,
            string planId)
        {
            var property =
                syncResult
                    .GetType()
                    .GetProperty(
                        collectionPropertyName);

            if (property is null)
            {
                throw new InvalidOperationException(
                    $"No se encontró la propiedad {collectionPropertyName}.");
            }

            var elementType =
                GetCollectionElementType(
                    property.PropertyType);

            var product =
                Activator.CreateInstance(
                    elementType);

            if (product is null)
            {
                throw new InvalidOperationException(
                    "No fue posible crear el detalle del producto sincronizado.");
            }

            SetProperty(
                product,
                "ProductName",
                productName);

            SetProperty(
                product,
                "Reference",
                reference);

            SetProperty(
                product,
                "UnitOfMeasure",
                unitOfMeasure);

            SetProperty(
                product,
                "PlanId",
                planId);

            var collection =
                EnsureCollection(
                    syncResult,
                    collectionPropertyName);

            var addMethod =
                collection
                    .GetType()
                    .GetMethod(
                        "Add");

            if (addMethod is null)
            {
                throw new InvalidOperationException(
                    $"La colección {collectionPropertyName} no permite agregar elementos.");
            }

            addMethod.Invoke(
                collection,
                new[]
                {
                    product
                });
        }

        private static object EnsureCollection(
            object owner,
            string propertyName)
        {
            var property =
                owner
                    .GetType()
                    .GetProperty(
                        propertyName);

            if (property is null)
            {
                throw new InvalidOperationException(
                    $"No se encontró la propiedad {propertyName}.");
            }

            var currentValue =
                property.GetValue(
                    owner);

            if (currentValue is not null)
                return currentValue;

            var elementType =
                GetCollectionElementType(
                    property.PropertyType);

            var listType =
                typeof(List<>)
                    .MakeGenericType(
                        elementType);

            var list =
                Activator.CreateInstance(
                    listType);

            if (list is null)
            {
                throw new InvalidOperationException(
                    $"No fue posible crear la colección {propertyName}.");
            }

            property.SetValue(
                owner,
                list);

            return list;
        }

        private static Type GetCollectionElementType(
            Type collectionType)
        {
            if (collectionType.IsArray)
            {
                return collectionType
                    .GetElementType()
                    ?? throw new InvalidOperationException(
                        "No se pudo determinar el tipo de la colección.");
            }

            if (collectionType.IsGenericType)
            {
                return collectionType
                    .GetGenericArguments()[0];
            }

            var enumerableInterface =
                collectionType
                    .GetInterfaces()
                    .FirstOrDefault(type =>
                        type.IsGenericType &&
                        type.GetGenericTypeDefinition() ==
                        typeof(IEnumerable<>));

            if (enumerableInterface is null)
            {
                throw new InvalidOperationException(
                    $"No se pudo determinar el tipo de elemento de {collectionType.Name}.");
            }

            return enumerableInterface
                .GetGenericArguments()[0];
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object? value)
        {
            var property =
                target
                    .GetType()
                    .GetProperty(
                        propertyName);

            if (property is null)
            {
                throw new InvalidOperationException(
                    $"No se encontró la propiedad {propertyName} en {target.GetType().Name}.");
            }

            property.SetValue(
                target,
                value);
        }
    }
}
