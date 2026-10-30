using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class InventoryExportRepository : IInventoryExportRepository
    {
        private readonly SitraCalcoContext _context;
        public InventoryExportRepository(SitraCalcoContext context) => _context = context;

        public Task<List<InventoryExportCenterAccessDto>> GetCenters(
            IReadOnlyCollection<long> centerIds, DateTime start, DateTime end, CancellationToken cancellationToken = default) =>
            _context.SolutionCenters.AsNoTracking()
                .Where(center => centerIds.Contains(center.solution_center_id))
                .Select(center => new InventoryExportCenterAccessDto
                {
                    SolutionCenterId = center.solution_center_id,
                    SolutionCenterTypeId = center.solution_center_type_id,
                    HasNonWarehouseRecords = _context.InventoryRecords.Any(row =>
                        row.solution_center_id == center.solution_center_id && row.created_at >= start && row.created_at < end &&
                        row.solution_center_type_id != 1)
                }).ToListAsync(cancellationToken);

        public async Task<InventoryExportReadDto> Read(
            DateTime start, DateTime end, IReadOnlyCollection<long> centerIds, bool warehousesOnly,
            CancellationToken cancellationToken = default)
        {
            // Una instantánea consistente para cabeceras, mínimos globales y últimos conteos.
            await using var transaction = await _context.Database.BeginTransactionAsync(
                _context.Database.IsMySql() ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.Serializable,
                cancellationToken);
            var allRecords = _context.InventoryRecords.AsNoTracking();
            var dayRecords = allRecords.Where(row => row.created_at >= start && row.created_at < end);
            if (centerIds.Count > 0)
                dayRecords = dayRecords.Where(row => centerIds.Contains(row.solution_center_id));
            if (warehousesOnly)
                dayRecords = dayRecords.Where(row => row.solution_center_type_id == 1);

            var result = new InventoryExportReadDto
            {
                Headers = await dayRecords.Select(row => new InventoryExportHeaderRow
                {
                    ExecutionId = row.inventory_execution_id,
                    CenterId = row.solution_center_id,
                    CenterTypeId = row.solution_center_type_id,
                    CenterCode = row.solution_center_code,
                    CenterName = row.solution_center_name,
                    ConfigurationId = row.inventory_configuration_id,
                    ConfigurationName = row.inventory_configuration_name,
                    SectionId = row.section_id,
                    SectionName = row.section_name
                }).Distinct().ToListAsync(cancellationToken)
            };

            if (result.Headers.Count > 0)
            {
                var executionIds = result.Headers.Select(row => row.ExecutionId).Distinct().ToList();
                // Sin filtro de fecha/centro: tanto F410 como la integridad abarcan toda la ejecución.
                result.Summaries = await allRecords.Where(row => executionIds.Contains(row.inventory_execution_id))
                    .GroupBy(row => row.inventory_execution_id)
                    .Select(group => new InventoryExportExecutionSummary
                    {
                        ExecutionId = group.Key,
                        BaseF410 = group.Min(row => row.inventory_id),
                        HasMixedContext = group.Min(row => row.solution_center_id) != group.Max(row => row.solution_center_id) ||
                            group.Min(row => row.solution_center_type_id) != group.Max(row => row.solution_center_type_id) ||
                            group.Min(row => row.inventory_configuration_id) != group.Max(row => row.inventory_configuration_id) ||
                            group.Min(row => row.section_id) != group.Max(row => row.section_id)
                    }).ToListAsync(cancellationToken);

                // No leer cantidades de ejecuciones que mezclen contextos, incluidos otros centros no autorizados.
                var consistentIds = result.Summaries.Where(row => !row.HasMixedContext).Select(row => row.ExecutionId).ToList();
                if (consistentIds.Count > 0)
                {
                    var candidates = dayRecords.Where(row => consistentIds.Contains(row.inventory_execution_id));
                    result.SelectedRows = await SelectLatestRows(candidates).ToListAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        private static IQueryable<InventoryExportSelectedRow> SelectLatestRows(IQueryable<InventoryRecord> candidates)
        {
            var latest = candidates.GroupBy(row => new { row.inventory_execution_id, row.product_id, row.unit_of_measure })
                .Select(group => new
                {
                    group.Key.inventory_execution_id,
                    group.Key.product_id,
                    group.Key.unit_of_measure,
                    count_number = group.Max(row => row.count_number),
                    HasDuplicates = group.Count() != group.Select(row => row.count_number).Distinct().Count()
                });
            return from row in candidates
                   join selected in latest
                       on new { row.inventory_execution_id, row.product_id, row.unit_of_measure, row.count_number }
                       equals new { selected.inventory_execution_id, selected.product_id, selected.unit_of_measure, selected.count_number }
                   select new InventoryExportSelectedRow { Record = row, HasDuplicateCounts = selected.HasDuplicates };
        }
    }
}
