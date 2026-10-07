using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;
using AutoMapper;

namespace KitchenTraceability.Application.Services
{
    public class ProductTemperatureConfigurationApplication : IProductTemperatureConfigurationApplication
    {
        private readonly IProductTemperatureConfigurationRepository _configurationRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public ProductTemperatureConfigurationApplication(IProductTemperatureConfigurationRepository configurationRepository,
            IProductRepository productRepository,
            IMapper mapper)
        {
            _configurationRepository = configurationRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Método que crea la configuración de temperatura de un producto.
        /// </summary>
        /// <param name="request">Type: CreateProductTemperatureConfigurationDto - Información a registrar.</param>
        /// <param name="user">Type: string - Usuario que realiza la operación.</param>
        /// <returns>Type: bool - True si fue creada correctamente.</returns>
        public async Task<bool> Create(CreateProductTemperatureConfigurationDto request, string user)
        {
            try
            {
                var product = await _productRepository.GetProductById(request.product_id);

                if (product is null)
                {
                    throw new KeyNotFoundException("El producto no existe.");
                }

                var existing = await _configurationRepository.GetByProductId(request.product_id);

                if (existing is not null)
                {
                    throw new InvalidOperationException("El producto ya tiene una configuración de temperatura.");
                }

                var configuration = _mapper.Map<ProductTemperatureConfiguration>(request);

                configuration.created_by = user;

                await _configurationRepository.Add(configuration);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que actualiza la configuración de temperatura de un producto.
        /// </summary>
        /// <param name="id">Type: long - Id de la configuración.</param>
        /// <param name="request">Type: UpdateProductTemperatureConfigurationDto - Información a actualizar.</param>
        /// <param name="user">Type: string - Usuario que realiza la operación.</param>
        /// <returns>Type: bool - True si fue actualizada; false si no existe.</returns>
        public async Task<bool> Update(long id, UpdateProductTemperatureConfigurationDto request, string user)
        {
            try
            {
                var configuration = await _configurationRepository.GetByTemperatureConfigurationId(id);

                if (configuration is null)
                {
                    return false;
                }

                _mapper.Map(request, configuration);

                configuration.updated_at = DateTime.UtcNow;
                configuration.updated_by = user;

                await _configurationRepository.Update(configuration);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que elimina una configuración de temperatura.
        /// </summary>
        /// <param name="id">Type: long - Id de la configuración.</param>
        /// <returns>Type: bool - True si fue eliminada; false si no existe.</returns>
        public async Task<bool> Delete(long id)
        {
            try
            {
                var configuration = await _configurationRepository.GetByTemperatureConfigurationId(id);

                if (configuration is null)
                {
                    return false;
                }

                await _configurationRepository.Delete(configuration);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }
    }
}