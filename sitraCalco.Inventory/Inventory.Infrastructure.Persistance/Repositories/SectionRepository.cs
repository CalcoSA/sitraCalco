using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class SectionRepository : ISectionRepository
    {
        private readonly SitraCalcoContext _context;

        public SectionRepository(
            SitraCalcoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Section>> GetAll()
        {
            return await _context.Sections
                .AsNoTracking()
                .OrderBy(section =>
                    section.section_name)
                .ToListAsync();
        }

        public async Task<Section?> GetById(
            long sectionId)
        {
            return await _context.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(section =>
                    section.section_id == sectionId);
        }

        public async Task<bool> ExistsByName(
            string sectionName,
            long? excludeSectionId = null)
        {
            var normalizedName =
                sectionName
                    .Trim()
                    .ToUpper();

            var query =
                _context.Sections
                    .AsNoTracking()
                    .Where(section =>
                        section.section_name
                            .ToUpper() == normalizedName);

            if (excludeSectionId.HasValue)
            {
                query = query.Where(section =>
                    section.section_id !=
                    excludeSectionId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<long> CreateSection(
            Section section)
        {
            await _context.Sections
                .AddAsync(section);

            await _context.SaveChangesAsync();

            return section.section_id;
        }

        public async Task<bool> UpdateSection(
            Section section)
        {
            var currentSection =
                await _context.Sections
                    .FirstOrDefaultAsync(current =>
                        current.section_id ==
                        section.section_id);

            if (currentSection is null)
                return false;

            currentSection.section_name =
                section.section_name;

            currentSection.is_active =
                section.is_active;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HasAssociations(
            long sectionId)
        {
            // Asociación existente de la sección
            // con centros y productos.
            var hasSolutionCenterProducts =
                await _context.SolutionCenterProducts
                    .AsNoTracking()
                    .AnyAsync(item =>
                        item.section_id ==
                        sectionId);

            if (hasSolutionCenterProducts)
                return true;

            // Asociación con las nuevas
            // configuraciones de inventario.
            var hasInventoryConfigurations =
                await _context
                    .InventoryConfigurationAssignments
                    .AsNoTracking()
                    .AnyAsync(item =>
                        item.section_id ==
                        sectionId);

            return hasInventoryConfigurations;
        }

        public async Task<bool> DeleteSection(
            long sectionId)
        {
            var section =
                await _context.Sections
                    .FirstOrDefaultAsync(section =>
                        section.section_id ==
                        sectionId);

            if (section is null)
                return false;

            _context.Sections.Remove(section);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}