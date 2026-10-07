using KitchenTraceability.Infrastructure.Persistance.Extensions;
using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        private readonly SitraCalcoContext _context;

        public SupplierRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener los proveedores registrados de forma paginada.
        /// </summary>
        /// <param name="page">Type: int - Número de página a consultar.</param>
        /// <param name="take">Type: int - Cantidad de registros por página.</param>
        /// <returns>Type: Page - Lista paginada de proveedores.</returns>
        public async Task<Page<Supplier>> GetAllSupplier(int page, int take)
        {
            var query = _context.Suppliers
                .AsNoTracking()
                .OrderBy(x => x.supplier_id);

            return await query.ToPagedAsync(page, take);
        }

        /// <summary>
        /// Método para obtener proveedores para una lista desplegable.
        /// Permite buscar por nombre o código y retorna máximo 20 registros.
        /// </summary>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o código.</param>
        /// <param name="take">Type: int - Cantidad máxima de registros a retornar.</param>
        /// <returns>Type: IEnumerable - Lista de proveedores encontrados.</returns>
        public async Task<IEnumerable<Supplier>> GetOptions(string? search = null, int take = 20)
        {
            var query = _context.Suppliers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(supplier =>
                    supplier.name.Contains(search) || supplier.supplier_code.Contains(search));
            }

            return await query
                .OrderBy(supplier => supplier.name)
                .ThenBy(supplier => supplier.supplier_code)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Método para un proveedor de la Tabla Supplier por el Id
        /// </summary>
        /// <param name="supplierId">Type: long - Id del proveedor a consultar</param>
        /// <returns>Type: Supplier - Entidad con la información solicitada</returns>
        public async Task<Supplier?> GetBySupplierId(long supplierId)
        {
            return await _context.Suppliers
                .FirstOrDefaultAsync(x => x.supplier_id == supplierId);
        }

        /// <summary>
        /// Método para verificar si ya existe un proveedor que tengan ese código
        /// </summary>
        /// <param name="supplierCode">Type: string - Código del proveedor que se desea validar.</param>
        /// <param name="excludeSupplierId">Type: long - Id de un proveedor que debe excluirse de la búsqueda.</param>
        /// <returns>Type: bool - indicando si fue existe o no el proveedor</returns>
        public async Task<bool> ExistsByCode(string supplierCode, long? excludeSupplierId = null)
        {
            var query = _context.Suppliers
                .AsNoTracking()
                .Where(x => x.supplier_code == supplierCode);

            if (excludeSupplierId.HasValue)
            {
                query = query.Where(x => x.supplier_id != excludeSupplierId.Value);
            }

            return await query.AnyAsync();
        }
    }
}