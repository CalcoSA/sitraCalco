using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Domain.Dtos;
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

        public async Task<List<int>> GetSavedCountNumbers(string inventoryExecutionId)
        {
            var savedNumbers = await _context.InventoryRecords
                .AsNoTracking()
                .Where(record => record.inventory_execution_id == inventoryExecutionId)
                .Select(record => record.count_number)
                .Distinct()
                .OrderBy(number => number)
                .ToListAsync();

            return savedNumbers
                .Select(number => (int)number)
                .ToList();
        }

        public async Task<string?> GetTodayExecutionId(long solutionCenterId, long inventoryConfigurationId, long sectionId, DateTime startDate, DateTime endDate)
        {
            var executionIds = await _context.InventoryRecords
                .AsNoTracking()
                .Where(record =>
                    record.solution_center_id == solutionCenterId &&
                    record.inventory_configuration_id == inventoryConfigurationId &&
                    record.section_id == sectionId &&
                    record.count_number == 1 &&
                    record.created_at >= startDate &&
                    record.created_at < endDate)
                .Select(record => record.inventory_execution_id)
                .Distinct()
                .Take(2)
                .ToListAsync();

            if (executionIds.Count > 1)
            {
                throw new InvalidOperationException("Existen varias ejecuciones del primer conteo para el mismo centro, inventario, sección y día.");
            }

            return executionIds.SingleOrDefault();
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

        /// <summary>
        /// Método para obtener el tipo de un SolutionCenter desde los registros históricos.
        /// </summary>
        /// <param name="solutionCenterId">Identificador del SolutionCenter</param>
        /// <returns>Type: long - Identificador del tipo de centro</returns>
        public async Task<long?> GetHistorySolutionCenterType(long solutionCenterId)
        {
            return await _context.InventoryRecords
                .AsNoTracking()
                .Where(record => record.solution_center_id == solutionCenterId)
                .Select(record => (long?)record.solution_center_type_id)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Método para consultar el historial de inventario por rango de fechas, SolutionCenter, InventoryConfiguration y Section.
        /// </summary>
        /// <param name="fromDate">Fecha inicial</param>
        /// <param name="toDate">Fecha final inclusiva</param>
        /// <param name="solutionCenterId">Identificador del SolutionCenter</param>
        /// <param name="inventoryConfigurationId">Identificador del InventoryConfiguration</param>
        /// <param name="sectionId">Identificador de la sección</param>
        /// <param name="solutionCenterTypeId">Filtro opcional del tipo de centro autorizado</param>
        /// <returns>Type: InventoryHistoryDto - Lista con el historial solicitado</returns>
        public async Task<List<InventoryHistoryDto>> GetHistory(DateOnly fromDate, DateOnly toDate, long solutionCenterId, long inventoryConfigurationId, long sectionId, long? solutionCenterTypeId = null)
        {
            var startDate = fromDate.ToDateTime(TimeOnly.MinValue);

            DateTime? endExclusive = toDate == DateOnly.MaxValue ? null : toDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var firstCountQuery = _context.InventoryRecords
                .AsNoTracking()
                .Where(record =>
                    record.solution_center_id == solutionCenterId &&
                    record.inventory_configuration_id == inventoryConfigurationId &&
                    record.section_id == sectionId &&
                    record.count_number == 1);

            if (solutionCenterTypeId.HasValue)
            {
                firstCountQuery = firstCountQuery.Where(record => record.solution_center_type_id == solutionCenterTypeId.Value);
            }

            var executionsQuery = firstCountQuery
                .GroupBy(record => record.inventory_execution_id)
                .Select(group => new
                {
                    InventoryExecutionId = group.Key,
                    FirstCountAt = group.Min(record => record.created_at)
                })
                .Where(execution => execution.FirstCountAt >= startDate);

            if (endExclusive.HasValue)
            {
                executionsQuery = executionsQuery.Where(execution => execution.FirstCountAt < endExclusive.Value);
            }

            var executions = await executionsQuery.ToListAsync();

            if (executions.Count == 0)
                return new List<InventoryHistoryDto>();

            var executionDates = executions.ToDictionary(
                execution => execution.InventoryExecutionId,
                execution => DateOnly.FromDateTime(execution.FirstCountAt));

            var executionIds = executionDates.Keys.ToList();

            var recordsQuery = _context.InventoryRecords
                .AsNoTracking()
                .Where(record =>
                    executionIds.Contains(record.inventory_execution_id) &&
                    record.solution_center_id == solutionCenterId &&
                    record.inventory_configuration_id == inventoryConfigurationId &&
                    record.section_id == sectionId);

            if (solutionCenterTypeId.HasValue)
            {
                recordsQuery = recordsQuery.Where(record => record.solution_center_type_id == solutionCenterTypeId.Value);
            }

            var records = await recordsQuery
                .OrderBy(record => record.inventory_execution_id)
                .ThenBy(record => record.product_id)
                .ThenBy(record => record.count_number)
                .ThenBy(record => record.inventory_id)
                .Select(record => new
                {
                    record.inventory_id,
                    record.inventory_execution_id,
                    record.product_id,
                    record.reference,
                    record.product_name,
                    record.unit_of_measure,
                    record.count_number,
                    record.count_value,
                    record.open,
                    record.closed,
                    record.multiplication_value,
                    record.siesa_stock,
                    record.observations,
                    record.entered_by
                })
                .ToListAsync();

            return records
                .GroupBy(record => record.inventory_execution_id)
                .Select(execution => new InventoryHistoryDto
                {
                    Date = executionDates[execution.Key],
                    InventoryExecutionId = execution.Key,

                    Products = execution
                        .GroupBy(record => record.product_id)
                        .Select(productGroup =>
                        {
                            var product = productGroup.First();

                            return new InventoryHistoryProductDto
                            {
                                Reference = product.reference,
                                ProductName = product.product_name,
                                UnitOfMeasure = product.unit_of_measure,

                                Counts = productGroup
                                    .OrderBy(record => record.count_number)
                                    .Select(record => new InventoryHistoryCountDto
                                    {
                                        InventoryId = record.inventory_id,
                                        CountNumber = record.count_number,
                                        CountValue = record.count_value,
                                        Open = record.open,
                                        Closed = record.closed,
                                        MultiplicationValue = record.multiplication_value,
                                        SiesaStock = record.siesa_stock,
                                        Observations = record.observations,
                                        EnteredBy = record.entered_by
                                    })
                                    .ToList()
                            };
                        })
                        .OrderBy(product => product.Reference)
                        .ThenBy(product => product.UnitOfMeasure)
                        .ToList()
                })
                .OrderBy(execution => execution.Date)
                .ThenBy(execution => execution.InventoryExecutionId)
                .ToList();
        }

        /// <summary>
        /// Método para obtener un registro de inventario por su identificador.
        /// </summary>
        /// <param name="inventoryId">Identificador del registro de inventario</param>
        /// <returns>Type: InventoryRecord - Registro solicitado</returns>
        public async Task<InventoryRecord?> GetInventoryRecordById(long inventoryId)
        {
            return await _context.InventoryRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.inventory_id == inventoryId);
        }

        /// <summary>
        /// Método para actualizar únicamente las observaciones de un registro de inventario.
        /// </summary>
        /// <param name="inventoryId">Identificador del registro de inventario</param>
        /// <param name="observations">Observaciones del inventario</param>
        /// <returns>Type: bool - Resultado de la actualización</returns>
        public async Task<bool> UpdateObservations(long inventoryId, string observations, bool append)
        {
            if (!append)
            {
                var updated = await _context.InventoryRecords
                    .Where(record => record.inventory_id == inventoryId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(record => record.observations, observations));

                return updated > 0;
            }

            var appended = await _context.InventoryRecords
                .Where(record =>
                    record.inventory_id == inventoryId &&
                    (
                        record.observations == null ||
                        record.observations == "" ||
                        record.observations.Length + observations.Length + 1 <= 1000
                    ))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        record => record.observations,
                        record =>
                            record.observations == null ||
                            record.observations == ""
                                ? observations
                                : record.observations + "\n" + observations
                    ));

            return appended > 0;
        }
    }
}