using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly SitraCalcoContext _context;

        public ProductRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Crea los productos que no existen y actualiza los existentes.
        /// La llave lógica actual corresponde a:
        /// reference + unit_of_measure + plan_id.
        /// </summary>
        public async Task<int> UpsertRange(IEnumerable<Product> products)
        {
            var productsToProcess = products
                .Where(product =>
                    !string.IsNullOrWhiteSpace(product.reference) &&
                    !string.IsNullOrWhiteSpace(product.unit_of_measure) &&
                    !string.IsNullOrWhiteSpace(product.plan_id))
                .GroupBy(product => new
                {
                    Reference = product.reference.Trim().ToUpperInvariant(),
                    UnitOfMeasure = product.unit_of_measure.Trim().ToUpperInvariant(),
                    PlanId = product.plan_id!.Trim().ToUpperInvariant()
                })
                .Select(group => group.First())
                .ToList();

            if (productsToProcess.Count == 0)
                return 0;

            var references = productsToProcess
                .Select(product => product.reference.Trim())
                .Distinct()
                .ToList();

            var existingProducts = await _context.Products
                .Where(product => references.Contains(product.reference))
                .ToListAsync();

            var existingProductsDictionary = existingProducts
                .GroupBy(product => GetProductKey(
                    product.reference,
                    product.unit_of_measure,
                    product.plan_id))
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

            foreach (var product in productsToProcess)
            {
                var key = GetProductKey(
                    product.reference,
                    product.unit_of_measure,
                    product.plan_id);

                if (existingProductsDictionary.TryGetValue(
                    key,
                    out var existingProduct))
                {
                    existingProduct.product_name = product.product_name;
                }
                else
                {
                    var newProduct = new Product
                    {
                        product_name = product.product_name.Trim(),
                        reference = product.reference.Trim(),
                        unit_of_measure = product.unit_of_measure.Trim(),
                        plan_id = product.plan_id?.Trim(),
                        image_path = null
                    };

                    await _context.Products.AddAsync(newProduct);

                    existingProductsDictionary[key] = newProduct;
                }
            }

            await _context.SaveChangesAsync();

            return productsToProcess.Count;
        }

        /// <summary>
        /// Consulta paginadamente los productos almacenados
        /// en la base de datos de SITRA.
        /// Permite buscar opcionalmente por referencia o nombre.
        /// </summary>
        public async Task<PagedDto<Product>> GetPaged(
            int page,
            int take,
            string? search = null)
        {
            if (page < 1)
                page = 1;

            if (take < 1)
                take = 10;

            var query = _context.Products
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchValue = search.Trim();

                query = query.Where(product =>
                    product.reference.Contains(searchValue) ||
                    product.product_name.Contains(searchValue));
            }

            var total = await query.CountAsync();

            var products = await query
                .OrderBy(product => product.product_id)
                .Skip((page - 1) * take)
                .Take(take)
                .ToListAsync();

            var pages = total == 0
                ? 0
                : (int)Math.Ceiling(total / (double)take);

            return new PagedDto<Product>
            {
                Items = products,
                Total = total,
                Page = page,
                Take = take,
                Pages = pages
            };
        }

        /// <summary>
        /// Busca productos por referencia o nombre.
        /// Retorna una cantidad limitada de registros.
        /// </summary>
        public async Task<IEnumerable<Product>> SearchProducts(
            string search,
            int take = 20)
        {
            if (string.IsNullOrWhiteSpace(search))
                return new List<Product>();

            var searchValue = search.Trim();

            if (take <= 0)
                take = 20;

            if (take > 50)
                take = 50;

            return await _context.Products
                .AsNoTracking()
                .Where(product =>
                    product.reference.Contains(searchValue) ||
                    product.product_name.Contains(searchValue))
                .OrderBy(product => product.product_name)
                .ThenBy(product => product.reference)
                .ThenBy(product => product.unit_of_measure)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Genera la llave lógica utilizada durante
        /// la sincronización de productos.
        /// </summary>
        private static string GetProductKey(
            string reference,
            string unitOfMeasure,
            string? planId)
        {
            return string.Join(
                "|",
                reference.Trim().ToUpperInvariant(),
                unitOfMeasure.Trim().ToUpperInvariant(),
                (planId ?? string.Empty).Trim().ToUpperInvariant());
        }

        public async Task<IEnumerable<Product>> GetByIds(
    IEnumerable<long> productIds)
        {
            var ids = productIds
                .Distinct()
                .ToList();

            return await _context.Products
                .AsNoTracking()
                .Where(product =>
                    ids.Contains(product.product_id))
                .ToListAsync();
        }
    }
}