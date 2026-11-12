using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ProductTemperatureConfigurationRepository : Repository<ProductTemperatureConfiguration>, IProductTemperatureConfigurationRepository
    {
        private readonly SitraCalcoContext _context;

        public ProductTemperatureConfigurationRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener una configuración de temperatura por Id.
        /// </summary>
        /// <param name="configurationId">Type: long - Id de la configuración.</param>
        /// <returns>Type: ProductTemperatureConfiguration - Configuración encontrada.</returns>
        public async Task<ProductTemperatureConfiguration?> GetByTemperatureConfigurationId(long configurationId)
        {
            return await _context.ProductTemperatureConfigurations
                .FirstOrDefaultAsync(x => x.temperature_configuration_id == configurationId);
        }

        /// <summary>
        /// Método para obtener la configuración de temperatura de un producto.
        /// </summary>
        /// <param name="productId">Type: long - Id del producto.</param>
        /// <returns>Type: ProductTemperatureConfiguration - Configuración encontrada.</returns>
        public async Task<ProductTemperatureConfiguration?> GetByProductId(long productId)
        {
            return await _context.ProductTemperatureConfigurations
                .FirstOrDefaultAsync(x => x.product_id == productId);
        }
    }
}