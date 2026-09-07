using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class SolutionCenterRepository : ISolutionCenterRepository
    {
        private readonly SitraCalcoContext _context;

        public SolutionCenterRepository(
            SitraCalcoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SolutionCenterType>>
            GetSolutionCenterTypes()
        {
            return await _context.SolutionCenterTypes
                .AsNoTracking()
                .OrderBy(x => x.solution_center_type_id)
                .ToListAsync();
        }

        public async Task<bool> SolutionCenterTypeExists(
    long solutionCenterTypeId)
        {
            return await _context.SolutionCenterTypes
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_type_id ==
                    solutionCenterTypeId);
        }

        public async Task<bool> SolutionCenterCodeExists(string solutionCenterCode)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_code ==
                    solutionCenterCode);
        }

        public async Task<long> CreateSolutionCenter(SolutionCenter solutionCenter)
        {
            await _context.SolutionCenters
                .AddAsync(solutionCenter);

            await _context.SaveChangesAsync();

            return solutionCenter.solution_center_id;
        }

        public async Task<bool> SolutionCenterExists(long solutionCenterId)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_id == solutionCenterId);
        }

        public async Task<long> CreateSectionConfiguration(long solutionCenterId,Section section,IEnumerable<SectionProductDto> products,string createdBy)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Crear la sección.
                await _context.Sections.AddAsync(section);

                await _context.SaveChangesAsync();

                // Después de SaveChanges, MySQL ya generó section_id.
                var sectionId = section.section_id;

                // 2. Crear la asociación de cada producto.
                var solutionCenterProducts = products
                    .Select(product => new SolutionCenterProduct
                    {
                        solution_center_id = solutionCenterId,

                        section_id = sectionId,

                        product_id = product.ProductId,

                        sort_order = product.SortOrder,

                        created_by = createdBy.Trim(),

                        created_at = DateTime.Now
                    })
                    .ToList();

                await _context.SolutionCenterProducts
                    .AddRangeAsync(solutionCenterProducts);

                await _context.SaveChangesAsync();

                // 3. Confirmar toda la operación.
                await transaction.CommitAsync();

                return sectionId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task<bool> SolutionCenterNameExists(string solutionCenterName)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_name == solutionCenterName);
        }

        public async Task<bool> SectionNameExists(long solutionCenterId,string sectionName)
        {
            return await (
                from solutionCenterProduct
                    in _context.SolutionCenterProducts.AsNoTracking()

                join section
                    in _context.Sections.AsNoTracking()
                    on solutionCenterProduct.section_id
                    equals section.section_id

                where
                    solutionCenterProduct.solution_center_id
                        == solutionCenterId
                    &&
                    section.section_name == sectionName

                select section.section_id
            )
            .AnyAsync();
        }
    }
}