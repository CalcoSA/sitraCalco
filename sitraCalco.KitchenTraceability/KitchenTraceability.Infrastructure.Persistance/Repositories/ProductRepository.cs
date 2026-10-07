using KitchenTraceability.Infrastructure.Persistance.Extensions;
using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly SitraCalcoContext _context;

        public ProductRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener los productos registrados la tabla Product.
        /// </summary>
        /// <param name="page">Type: int - Número de página a consultar.</param>
        /// <param name="take">Type: int - Cantidad de registros por página.</param>
        /// <param name="search">Type: string - Texto opcional para buscar por referencia o nombre.</param>
        /// <returns>Type: Page - Lista paginada de productos.</returns>
        public async Task<Page<Product>> GetAllProduct(int page, int take, string? search = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(product => product.productReceptionConfigurations)
                .Include(product => product.productTemperatureConfigurations)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(product => product.product_reference.Contains(search) || product.product_name.Contains(search));
            }

            query = query.OrderBy(product => product.product_id);

            return await query.ToPagedAsync(page, take);
        }

        /// <summary>
        /// Método para obtener productos para una lista desplegable.
        /// Permite buscar por nombre o referencia y retorna máximo 20 registros.
        /// </summary>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o referencia.</param>
        /// <param name="take">Type: int - Cantidad máxima de registros a retornar.</param>
        /// <returns>Type: IEnumerable - Lista de productos encontrados.</returns>
        public async Task<IEnumerable<Product>> GetOptions(string? search = null, int take = 20)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(product => product.productReceptionConfigurations)
                .Include(product => product.productTemperatureConfigurations)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(product =>
                    product.product_name.Contains(search) || product.product_reference.Contains(search));
            }

            return await query
                .OrderBy(product => product.product_name)
                .ThenBy(product => product.product_reference)
                .ThenBy(product => product.unit_of_measure)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Método para obtener un producto por Id incluyendo la configuración de recepción y temperatura.
        /// </summary>
        /// <param name="productId">Type: long - Id del producto a consultar.</param>
        /// <returns>Type: Product - Producto con sus configuraciones relacionadas.</returns>
        public async Task<Product?> GetProductById(long productId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(x => x.productReceptionConfigurations)
                .Include(x => x.productTemperatureConfigurations)
                .FirstOrDefaultAsync(x => x.product_id == productId);
        }

        /// <summary>
        /// Método para registrar una colección de productos en la base de datos.
        /// </summary>
        /// <param name="products">Type: IEnumerable - Productos que se desean registrar.</param>
        /// <returns>Type: Task - Operación asíncrona.</returns>
        public async Task AddRange(IEnumerable<Product> products)
        {
            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();
        }
    }
}