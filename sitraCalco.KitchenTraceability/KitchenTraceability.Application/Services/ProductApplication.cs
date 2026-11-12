using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;
using AutoMapper;

namespace KitchenTraceability.Application.Services
{
    public class ProductApplication : IProductApplication
    {
        private readonly IProductRepository _productRepository;
        private readonly ISiesaRepository _siesaRepository;
        private readonly IMapper _mapper;

        public ProductApplication(IProductRepository productRepository,
            ISiesaRepository siesaRepository,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _siesaRepository = siesaRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener todos los productos registrados de forma paginada.
        /// </summary>
        /// <param name="page">Type: int - Número de página a consultar.</param>
        /// <param name="take">Type: int - Cantidad de registros por página.</param>
        /// <param name="search">Type: string - Texto opcional para buscar por referencia o nombre.</param>
        /// <returns>Type: Page - Lista paginada con la información solicitada.</returns>
        public async Task<Page<ProductDto>> GetAll(int page, int take, string? search = null)
        {
            try
            {
                var products = await _productRepository.GetAllProduct(page, take, search);

                return new Page<ProductDto>
                {
                    Items = _mapper.Map<IEnumerable<ProductDto>>(products.Items),
                    PageNumber = products.PageNumber,
                    PageSize = products.PageSize,
                    TotalRecords = products.TotalRecords,
                    TotalPages = products.TotalPages
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener productos
        /// disponibles para una lista desplegable.
        /// </summary>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o referencia.</param>
        /// <returns>Type: IEnumerable - Lista de productos encontrados.</returns>
        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener productos
        /// disponibles para una lista desplegable, incluyendo su configuración
        /// de recepción y temperatura.
        /// </summary>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o referencia.</param>
        /// <returns>Type: IEnumerable - Lista de productos encontrados.</returns>
        public async Task<IEnumerable<ProductDto>> GetOptions(string? search = null)
        {
            try
            {
                var products = await _productRepository.GetOptions(search, 20);
                return _mapper.Map<IEnumerable<ProductDto>>(products);
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener un producto por Id
        /// con su configuración de recepción y temperatura.
        /// </summary>
        /// <param name="id">Type: long - Id del producto a consultar.</param>
        /// <returns>Type: ProductDetailDto - Producto con sus configuraciones.</returns>
        public async Task<ProductDetailDto?> GetById(long id)
        {
            try
            {
                var product = await _productRepository.GetProductById(id);

                if (product is null)
                {
                    return null;
                }

                var receptionConfiguration = product.productReceptionConfigurations.FirstOrDefault();
                var temperatureConfiguration = product.productTemperatureConfigurations.FirstOrDefault();

                return new ProductDetailDto
                {
                    product_id = product.product_id,
                    product_name = product.product_name,
                    product_reference = product.product_reference,
                    unit_of_measure = product.unit_of_measure,
                    product_reception_configuration =
                        receptionConfiguration is null
                            ? null
                            : _mapper.Map<ProductReceptionConfigurationDto>(receptionConfiguration),

                    product_temperature_configuration =
                        temperatureConfiguration is null
                            ? null
                            : _mapper.Map<ProductTemperatureConfigurationDto>(temperatureConfiguration)
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que consulta los productos registrados en SIESA, identifica los que aún no existen
        /// en KitchenTraceability y registra los productos nuevos.
        /// La comparación se realiza por referencia y unidad de medida.
        /// </summary>
        /// <returns>Type: ProductSyncResultDto - Resultado de la sincronización de productos.</returns>
        public async Task<ProductResultDto> GetSiesaProducts()
        {
            try
            {
                var siesaProducts = await _siesaRepository.GetProducts();
                var sitraProducts = await _productRepository.GetAll();
                var existingProducts = new HashSet<string>(sitraProducts.Select(BuildProductKey), StringComparer.OrdinalIgnoreCase);

                var newProducts = siesaProducts
                    .Where(product => !existingProducts.Contains(BuildProductKey(product)))
                    .GroupBy(BuildProductKey, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();

                if (!newProducts.Any())
                {
                    return new ProductResultDto
                    {
                        NewProductsSaved = false,
                        NewProductsCount = 0,
                    };
                }

                await _productRepository.AddRange(newProducts);

                return new ProductResultDto
                {
                    NewProductsSaved = true,
                    NewProductsCount = newProducts.Count,
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método para construir la llave de comparación de un producto.
        /// </summary>
        /// <param name="product">Type: Product - Producto a comparar.</param>
        /// <returns>Type: string - Llave compuesta por nombre, referencia y unidad de medida.</returns>
        private static string BuildProductKey(Product product)
        {
            return string.Join(
                "|",
                product.product_reference,
                product.unit_of_measure);
        }
    }
}