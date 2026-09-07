using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class ProductApplication : IProductApplication
    {
        private readonly ISiesaRepository _siesaRepository;
        private readonly IProductRepository _productRepository;

        public ProductApplication(
            ISiesaRepository siesaRepository,
            IProductRepository productRepository)
        {
            _siesaRepository = siesaRepository;
            _productRepository = productRepository;
        }

        /// <summary>
        /// Consulta los productos autorizados en SIESA y los crea
        /// o actualiza en la base de datos local de Inventarios.
        /// </summary>
        /// <returns>
        /// Type: int - Cantidad de registros procesados.
        /// </returns>
        public async Task<int> SyncProducts()
        {
            try
            {
                var siesaProducts = await _siesaRepository.GetProducts();

                var products = siesaProducts
                    .Where(product =>
                        !string.IsNullOrWhiteSpace(product.reference) &&
                        !string.IsNullOrWhiteSpace(product.unit_of_measure) &&
                        !string.IsNullOrWhiteSpace(product.plan_id))
                    .ToList();

                if (!products.Any())
                    return 0;

                var processedProducts =
                    await _productRepository.UpsertRange(products);

                return processedProducts;
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