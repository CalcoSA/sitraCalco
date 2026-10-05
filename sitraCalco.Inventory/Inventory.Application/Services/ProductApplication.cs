using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Domain.Helpers;
using Inventory.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Application.Services
{
    public class ProductApplication : IProductApplication
    {
        private readonly ISiesaRepository _siesaRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStorageService _storageService;
        private readonly ILogApplication _logApplication;
        private readonly GoogleCloudStorageOptions _storageOptions;
        private readonly ILogger<ProductApplication> _logger;

        public ProductApplication(
            ISiesaRepository siesaRepository,
            IProductRepository productRepository,
            IStorageService storageService,
            ILogApplication logApplication,
            IOptions<GoogleCloudStorageOptions> storageOptions,
            ILogger<ProductApplication> logger)
        {
            _siesaRepository = siesaRepository;
            _productRepository = productRepository;
            _storageService = storageService;
            _logApplication = logApplication;
            _storageOptions = storageOptions.Value;
            _logger = logger;
        }

        public Task<ProductImageResultDto> UploadImage(
            long productId, Stream stream, string fileName, string contentType, long length, string userLogin)
        {
            return SaveImage(productId, stream, fileName, contentType, length, userLogin, replace: false);
        }

        public Task<ProductImageResultDto> ReplaceImage(
            long productId, Stream stream, string fileName, string contentType, long length, string userLogin)
        {
            return SaveImage(productId, stream, fileName, contentType, length, userLogin, replace: true);
        }

        public async Task<ProductImageResultDto> GetImage(long productId)
        {
            var product = await _productRepository.GetProductById(productId);
            if (product is null)
                return new ProductImageResultDto { Status = ProductImageStatus.ProductNotFound };

            if (string.IsNullOrWhiteSpace(product.image_path))
                return new ProductImageResultDto { Status = ProductImageStatus.ImageNotFound };

            if (!IsProductImagePath(productId, product.image_path))
                throw new InvalidOperationException("La ruta de imagen no pertenece al prefijo del producto.");

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_storageOptions.SignedUrlExpirationMinutes);
            var imageUrl = await _storageService.GenerateSignedUrlAsync(product.image_path, expiresAt);

            return new ProductImageResultDto
            {
                Status = ProductImageStatus.Success,
                Data = new ProductImageDto
                {
                    ProductId = productId,
                    ImagePath = product.image_path,
                    ImageUrl = imageUrl,
                    ExpiresAt = expiresAt
                }
            };
        }

        public async Task<ProductImageResultDto> DeleteImage(long productId, string userLogin)
        {
            if (productId <= 0 || string.IsNullOrWhiteSpace(userLogin))
                throw new ArgumentException("El producto y el usuario son obligatorios.");

            var product = await _productRepository.GetProductById(productId);
            if (product is null)
                return new ProductImageResultDto { Status = ProductImageStatus.ProductNotFound };

            var imagePath = product.image_path;
            if (string.IsNullOrWhiteSpace(imagePath))
                return new ProductImageResultDto { Status = ProductImageStatus.ImageNotFound };

            if (!IsProductImagePath(productId, imagePath))
                throw new InvalidOperationException("La ruta de imagen no pertenece al prefijo del producto.");

            // Ante un fallo de GCS, no modificar la ruta que conserva MySQL.
            await _storageService.DeleteAsync(imagePath);

            try
            {
                var updated = await _productRepository.TryUpdateImagePath(productId, imagePath, null);
                if (!updated)
                    throw new InvalidOperationException("La ruta de imagen cambió o el producto dejó de existir durante la eliminación.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "El objeto {ImagePath} se eliminó de GCS, pero no se pudo limpiar image_path del producto {ProductId}. Se requiere revisión manual.",
                    imagePath, productId);
                throw;
            }

            await _logApplication.CreateLog(new CreateLogDto
            {
                Action = "Eliminar",
                Module = "Productos",
                Description = $"Se eliminó la imagen del producto {product.product_name}, referencia {product.reference}.",
                UserName = userLogin.Trim()
            });

            return new ProductImageResultDto { Status = ProductImageStatus.Success };
        }

        private async Task<ProductImageResultDto> SaveImage(
            long productId, Stream stream, string fileName, string contentType, long length, string userLogin, bool replace)
        {
            if (productId <= 0 || string.IsNullOrWhiteSpace(userLogin))
                throw new ArgumentException("El producto y el usuario son obligatorios.");

            var validationError = ProductImageFiles.Validate(fileName, contentType, length, _storageOptions.MaxImageSizeBytes);
            if (validationError is not null)
                throw new ArgumentException(validationError);

            var product = await _productRepository.GetProductById(productId);
            if (product is null)
                return new ProductImageResultDto { Status = ProductImageStatus.ProductNotFound };

            var oldImagePath = product.image_path;
            if (!replace && !string.IsNullOrWhiteSpace(oldImagePath))
                return new ProductImageResultDto { Status = ProductImageStatus.ImageAlreadyExists };

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var objectName = $"{_storageOptions.ProductImagePrefix}/{productId}/{Guid.NewGuid():N}{extension}";
            await _storageService.UploadAsync(stream, objectName, contentType.ToLowerInvariant());

            bool updated;
            try
            {
                updated = await _productRepository.TryUpdateImagePath(productId, oldImagePath, objectName);
            }
            catch
            {
                // Solo compensar el archivo nuevo si no se pudo persistir la ruta. No tocar la imagen anterior.
                await DeleteImageBestEffort(objectName, productId);
                throw;
            }

            if (!updated)
            {
                await DeleteImageBestEffort(objectName, productId);
                var currentProduct = await _productRepository.GetProductById(productId);
                return new ProductImageResultDto
                {
                    Status = currentProduct is null
                        ? ProductImageStatus.ProductNotFound
                        : !replace && !string.IsNullOrWhiteSpace(currentProduct.image_path)
                            ? ProductImageStatus.ImageAlreadyExists
                            : ProductImageStatus.Conflict
                };
            }

            // MySQL ya apunta al objeto nuevo. Un fallo posterior no debe eliminarlo.
            try
            {
                await _logApplication.CreateLog(new CreateLogDto
                {
                    Action = "Actualizar",
                    Module = "Productos",
                    Description = $"Se {(replace ? "actualizó" : "cargó")} la imagen del producto {product.product_name}, referencia {product.reference}.",
                    UserName = userLogin.Trim()
                });
            }
            finally
            {
                if (replace && !string.IsNullOrWhiteSpace(oldImagePath))
                {
                    if (IsProductImagePath(productId, oldImagePath))
                        await DeleteImageBestEffort(oldImagePath, productId);
                    else
                        _logger.LogWarning("No se eliminó la imagen anterior del producto {ProductId}: está fuera del prefijo administrado.", productId);
                }
            }

            return new ProductImageResultDto
            {
                Status = ProductImageStatus.Success,
                Data = new ProductImageDto { ProductId = productId, ImagePath = objectName }
            };
        }

        private bool IsProductImagePath(long productId, string objectName)
        {
            return objectName.StartsWith($"{_storageOptions.ProductImagePrefix}/{productId}/", StringComparison.Ordinal);
        }

        private async Task DeleteImageBestEffort(string objectName, long productId)
        {
            try
            {
                await _storageService.DeleteAsync(objectName);
            }
            catch (Exception ex)
            {
                // Conservar el resultado de la escritura y registrar el objeto pendiente de limpieza.
                _logger.LogError(ex, "No se pudo eliminar el objeto {ObjectName} del producto {ProductId}.", objectName, productId);
            }
        }

        /// <summary>
        /// Consulta los productos autorizados en SIESA y los crea
        /// o actualiza en la base de datos local de Inventarios.
        /// </summary>
        /// <returns>
        /// Type: int - Cantidad de registros procesados.
        /// </returns>
        public async Task<ProductSyncResultDto> SyncProducts()
        {
            try
            {
                var siesaProducts =
                    await _siesaRepository.GetProducts();

                var products = siesaProducts
                    .Where(product =>
                        !string.IsNullOrWhiteSpace(product.reference) &&
                        !string.IsNullOrWhiteSpace(product.unit_of_measure) &&
                        !string.IsNullOrWhiteSpace(product.plan_id))
                    .ToList();

                return await _productRepository
                    .UpsertRange(products);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Consulta paginadamente los productos almacenados
        /// en la base de datos local de Inventarios.
        /// </summary>
        /// <param name="page">
        /// Type: int - Número de página.
        /// </param>
        /// <param name="take">
        /// Type: int - Cantidad de registros por página.
        /// </param>
        /// <returns>
        /// Type: PagedDto Product - Resultado paginado.
        /// </returns>
        public async Task<PagedDto<Product>> GetPaged(int page,int take,string? search = null)
        {
            try
            {
                if (page <= 0)
                    page = 1;

                if (take <= 0)
                    take = 10;

                var products = await _productRepository.GetPaged(
                    page,
                    take,
                    search);

                return products;
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<ProductOptionDto>> SearchProducts(
    string search,
    int take = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                    return new List<ProductOptionDto>();

                if (take <= 0)
                    take = 20;

                if (take > 50)
                    take = 50;

                var products =
                    await _productRepository.SearchProducts(
                        search,
                        take);

                return products.Select(product =>
                    new ProductOptionDto
                    {
                        ProductId = product.product_id,
                        ProductName = product.product_name,
                        Reference = product.reference,
                        UnitOfMeasure = product.unit_of_measure,
                        PlanId = product.plan_id
                    })
                    .ToList();
            }
            catch
            {
                throw;
            }
        }

    }

}
