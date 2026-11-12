using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Domain.Options;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace Inventory.Test.Controllers
{
    public class ProductGetImageUrlTests
    {
        private readonly Mock<IProductRepository> _repository = new(MockBehavior.Strict);
        private readonly Mock<IStorageService> _storage = new(MockBehavior.Strict);
        private readonly GoogleCloudStorageOptions _options = new() { SignedUrlExpirationMinutes = 37 };

        [Theory]
        [InlineData(true, null)]
        [InlineData(true, "")]
        [InlineData(true, "   ")]
        [InlineData(false, null)]
        [InlineData(false, "")]
        [InlineData(false, "   ")]
        public async Task Gets_ShouldPreserveResponseAndSignOnlyReturnedImages(bool paged, string? missingPath)
        {
            var products = new[]
            {
                CreateProduct(21, $"{_options.ProductImagePrefix}/21/photo.jpg"),
                CreateProduct(22, missingPath),
                CreateProduct(23, $"{_options.ProductImagePrefix}/23/photo.png")
            };
            SetupRepository(paged, products, total: 12);
            var expirations = new List<DateTimeOffset>();
            foreach (var product in products.Where(p => !string.IsNullOrWhiteSpace(p.image_path)))
            {
                _storage.Setup(s => s.GenerateSignedUrlAsync(product.image_path!, It.IsAny<DateTimeOffset>()))
                    .Callback<string, DateTimeOffset>((_, expiresAt) => expirations.Add(expiresAt))
                    .ReturnsAsync($"https://storage.example.test/{product.product_id}?test-signature=stub");
            }

            var before = DateTimeOffset.UtcNow;
            var response = await InvokeGet(paged);
            var after = DateTimeOffset.UtcNow;
            Assert.True(response.IsSuccess);
            Assert.Equal("Productos consultados correctamente.", response.Message);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var result = json.RootElement.GetProperty("result");
            var items = paged ? result.GetProperty("items") : result;
            Assert.Equal(3, items.GetArrayLength());
            if (paged)
            {
                Assert.Equal(12, result.GetProperty("total").GetInt32());
                Assert.Equal(2, result.GetProperty("page").GetInt32());
                Assert.Equal(3, result.GetProperty("take").GetInt32());
                Assert.Equal(4, result.GetProperty("pages").GetInt32());
            }

            for (var i = 0; i < products.Length; i++)
            {
                var product = products[i];
                var item = items[i];
                Assert.Equal(product.product_id, item.GetProperty(paged ? "product_id" : "productId").GetInt64());
                Assert.Equal(product.product_name, item.GetProperty(paged ? "product_name" : "productName").GetString());
                Assert.Equal(product.reference, item.GetProperty("reference").GetString());
                Assert.Equal(product.unit_of_measure, item.GetProperty(paged ? "unit_of_measure" : "unitOfMeasure").GetString());
                Assert.Equal(product.plan_id, item.GetProperty(paged ? "plan_id" : "planId").GetString());
                Assert.Equal(product.image_path, item.GetProperty("image_path").GetString());
                if (i == 1)
                    Assert.Equal(JsonValueKind.Null, item.GetProperty("imageUrl").ValueKind);
                else
                    Assert.Equal($"https://storage.example.test/{product.product_id}?test-signature=stub", item.GetProperty("imageUrl").GetString());
            }

            Assert.Equal(2, expirations.Count);
            Assert.All(expirations, expiresAt => Assert.InRange(expiresAt,
                before.AddMinutes(_options.SignedUrlExpirationMinutes),
                after.AddMinutes(_options.SignedUrlExpirationMinutes)));
            foreach (var product in products.Where(p => !string.IsNullOrWhiteSpace(p.image_path)))
                _storage.Verify(s => s.GenerateSignedUrlAsync(product.image_path!, It.IsAny<DateTimeOffset>()), Times.Once);
            _storage.VerifyNoOtherCalls();
            VerifyRepository(paged);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Gets_ShouldPreserveEmptyResponseWithoutCallingStorage(bool paged)
        {
            SetupRepository(paged, Array.Empty<Product>(), total: 0);

            var response = await InvokeGet(paged);

            Assert.False(response.IsSuccess);
            Assert.Equal("No se encontraron productos.", response.Message);
            if (paged)
                Assert.Empty(Assert.IsType<PagedDto<ProductListDto>>(response.Result).Items);
            else
                Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ProductOptionDto>>(response.Result));
            _storage.VerifyNoOtherCalls();
            VerifyRepository(paged);
        }

        private static Product CreateProduct(long id, string? imagePath) => new()
        {
            product_id = id,
            product_name = $"Aceite {id}",
            reference = $"REF-{id}",
            unit_of_measure = "Unidad",
            plan_id = "FAC",
            image_path = imagePath
        };

        private void SetupRepository(bool paged, Product[] products, int total)
        {
            if (paged)
                _repository.Setup(r => r.GetPaged(2, 3, " aceite ")).ReturnsAsync(new PagedDto<Product>
                {
                    Items = products, Total = total, Page = 2, Take = 3, Pages = total / 3
                });
            else
                _repository.Setup(r => r.SearchProducts(" aceite ", 3)).ReturnsAsync(products);
        }

        private void VerifyRepository(bool paged)
        {
            if (paged)
                _repository.Verify(r => r.GetPaged(2, 3, " aceite "), Times.Once);
            else
                _repository.Verify(r => r.SearchProducts(" aceite ", 3), Times.Once);
            _repository.VerifyNoOtherCalls();
        }

        private async Task<ResponseApi> InvokeGet(bool paged)
        {
            var logs = new Mock<ILogApplication>(MockBehavior.Strict);
            var application = new ProductApplication(
                Mock.Of<ISiesaRepository>(), _repository.Object, _storage.Object, logs.Object,
                Options.Create(_options), NullLogger<ProductApplication>.Instance);
            var controller = new ProductController(application, logs.Object,
                NullLogger<ProductController>.Instance, Options.Create(_options));

            var result = paged
                ? await controller.GetPaged(2, 3, " aceite ")
                : await controller.SearchProducts(" aceite ", 3);

            return Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
        }
    }
}
