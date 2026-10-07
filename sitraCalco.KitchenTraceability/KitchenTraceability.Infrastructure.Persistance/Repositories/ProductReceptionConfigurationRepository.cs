using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ProductReceptionConfigurationRepository : Repository<ProductReceptionConfiguration>, IProductReceptionConfigurationRepository
    {
        private readonly SitraCalcoContext _context;

        public ProductReceptionConfigurationRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener una configuración de recepción por Id.
        /// </summary>
        /// <param name="configurationId">Type: long - Id de la configuración.</param>
        /// <returns>Type: ProductReceptionConfiguration - Configuración encontrada.</returns>
        public async Task<ProductReceptionConfiguration?> GetByProductConfigurationId(long configurationId)
        {
            return await _context.ProductReceptionConfigurations
                .FirstOrDefaultAsync(x => x.product_configuration_id == configurationId);
        }

        /// <summary>
        /// Método para obtener la configuración de recepción de un producto.
        /// </summary>
        /// <param name="productId">Type: long - Id del producto.</param>
        /// <returns>Type: ProductReceptionConfiguration - Configuración encontrada.</returns>
        public async Task<ProductReceptionConfiguration?> GetByProductId(long productId)
        {
            return await _context.ProductReceptionConfigurations
                .FirstOrDefaultAsync(x => x.product_id == productId);
        }
    }
}