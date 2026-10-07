using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly SitraCalcoContext _context;

        public InventoryRepository(SitraCalcoContext context)
        {
            _context = context;
        }

        public async Task<InventoryCountsDto?> GetCountsContext(string inventoryExecutionId)
        {
            // Leer solo el contexto histórico, sin materializar valores de otros conteos.
            var snapshot = await _context.InventoryRecords.AsNoTracking()
                .Where(record => record.inventory_execution_id == inventoryExecutionId)
                .OrderBy(record => record.inventory_id)
                .Select(record => new
                {
                    record.inventory_execution_id,
                    record.solution_center_id,
                    record.solution_center_type_id,
                    record.solution_center_code,
                    record.solution_center_name,
                    record.inventory_configuration_id,
                    record.inventory_configuration_name,
                    record.section_id,
                    record.section_name,
                    HasMixedContext = _context.InventoryRecords.Any(other =>
                        other.inventory_execution_id == record.inventory_execution_id &&
                        (other.solution_center_id != record.solution_center_id ||
                         other.solution_center_type_id != record.solution_center_type_id ||
                         other.inventory_configuration_id != record.inventory_configuration_id ||
                         other.section_id != record.section_id))
                })
                .FirstOrDefaultAsync();

            if (snapshot is null)
                return null;
            if (snapshot.HasMixedContext)
                throw new InvalidOperationException($"La ejecución {inventoryExecutionId} contiene contextos de inventario inconsistentes.");

            return new InventoryCountsDto
            {
                InventoryExecutionId = snapshot.inventory_execution_id,
                SolutionCenterId = snapshot.solution_center_id,
                SolutionCenterTypeId = snapshot.solution_center_type_id,
                SolutionCenterCode = snapshot.solution_center_code,
                SolutionCenterName = snapshot.solution_center_name,
                InventoryConfigurationId = snapshot.inventory_configuration_id,
                InventoryConfigurationName = snapshot.inventory_configuration_name,
                SectionId = snapshot.section_id,
                SectionName = snapshot.section_name
            };
        }

        public async Task<List<InventoryProductCountsDto>> GetCounts(string inventoryExecutionId, int? countNumber)
        {
            var query = _context.InventoryRecords.AsNoTracking()
                .Where(record => record.inventory_execution_id == inventoryExecutionId);
            if (countNumber.HasValue)
                query = query.Where(record => record.count_number == countNumber.Value);

            var rows = await query
                .OrderBy(record => record.product_id)
                .ThenBy(record => record.count_number)
                .ThenBy(record => record.inventory_id)
                .Select(record => new
                {
                    record.product_id,
                    record.count_number,
                    record.count_value,
                    record.open,
                    record.closed,
                    record.multiplication_value
                })
                .ToListAsync();

            return rows.GroupBy(record => record.product_id)
                .Select(group => new InventoryProductCountsDto
                {
                    ProductId = group.Key,
                    Counts = group.Select(record => new InventoryCountDto
                    {
                        CountNumber = record.count_number,
                        Value = record.count_value,
                        Open = record.open,
                        Closed = record.closed,
                        MultiplicationValue = record.multiplication_value
                    }).ToList()
                }).ToList();
        }

        public async Task<InventorySaveContextDto?> GetContext(
            long solutionCenterId, long inventoryConfigurationId, long sectionId)
        {
            return await (
                from center in _context.SolutionCenters.AsNoTracking()
                from configuration in _context.InventoryConfigurations.AsNoTracking()
                from section in _context.Sections.AsNoTracking()
                where center.solution_center_id == solutionCenterId &&
                    configuration.inventory_configuration_id == inventoryConfigurationId &&
                    section.section_id == sectionId
                select new InventorySaveContextDto
                {
                    SolutionCenter = center,
                    Configuration = configuration,
                    Section = section
                }).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Product>> GetProducts(
            long solutionCenterId, long sectionId, IEnumerable<long> productIds)
        {
            var ids = productIds.Distinct().ToList();
            return await _context.Products.AsNoTracking()
                .Where(product => ids.Contains(product.product_id) &&
                    _context.SolutionCenterProducts.Any(assignment =>
                        assignment.solution_center_id == solutionCenterId &&
                        assignment.section_id == sectionId &&
                        assignment.product_id == product.product_id))
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryExecutionContextDto>> GetExecutionContexts(string inventoryExecutionId)
        {
            return await _context.InventoryRecords.AsNoTracking()
                .Where(record => record.inventory_execution_id == inventoryExecutionId)
                .Select(record => new
                {
                    record.solution_center_id,
                    record.inventory_configuration_id,
                    record.section_id,
                    record.count_number
                })
                .Distinct()
                .Select(record => new InventoryExecutionContextDto
                {
                    SolutionCenterId = record.solution_center_id,
                    InventoryConfigurationId = record.inventory_configuration_id,
                    SectionId = record.section_id,
                    CountNumber = record.count_number
                })
                .ToListAsync();
        }

        public async Task<bool> HasDuplicates(IReadOnlyCollection<InventoryRecord> records)
        {
            var first = records.First();
            var productIds = records.Select(record => record.product_id).Distinct().ToList();
            var existing = await _context.InventoryRecords.AsNoTracking()
                .Where(record => record.inventory_execution_id == first.inventory_execution_id &&
                    record.count_number == first.count_number && productIds.Contains(record.product_id))
                .Select(record => new { record.product_id, record.unit_of_measure })
                .ToListAsync();

            var requested = records.ToDictionary(record => record.product_id, record => record.unit_of_measure);
            return existing.Any(record => string.Equals(
                requested[record.product_id], record.unit_of_measure, StringComparison.Ordinal));
        }

        // False significa duplicado funcional; cualquier otro fallo se propaga.
        public async Task<bool> Save(IReadOnlyCollection<InventoryRecord> records, bool isExistingExecution)
        {
            var first = records.First();
            var protectFirstCount = isExistingExecution && first.count_number == 1;
            // El UNIQUE no protege dos Conteos 1 con productos distintos en una ejecución existente.
            await using var transaction = protectFirstCount
                ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
                : await _context.Database.BeginTransactionAsync();
            try
            {
                if (protectFirstCount && await _context.InventoryRecords.AnyAsync(record =>
                    record.inventory_execution_id == first.inventory_execution_id && record.count_number == 1))
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await _context.InventoryRecords.AddRangeAsync(records);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 } mysql &&
                mysql.Message.Contains("uq_inventory_execution_count_product_uom", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync();
                foreach (var record in records)
                    _context.Entry(record).State = EntityState.Detached;
                return false;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
