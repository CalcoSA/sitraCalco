using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Data;

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

        public async Task<IEnumerable<Section>> GetAll(long? solutionCenterTypeId = null)
        {
            var query = _context.Sections.AsNoTracking();
            if (solutionCenterTypeId.HasValue)
                query = query.Where(section => section.solution_center_type_id == solutionCenterTypeId.Value);

            return await query
                .Include(section => section.SolutionCenterType)
                .OrderBy(section =>
                    section.section_name)
                .ToListAsync();
        }

        public async Task<Section?> GetById(
            long sectionId)
        {
            return await _context.Sections
                .AsNoTracking()
                .Include(section => section.SolutionCenterType)
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
            Section section, long? solutionCenterTypeId = null)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var currentSection =
                await _context.Sections
                    .FirstOrDefaultAsync(current =>
                        current.section_id ==
                        section.section_id);

            if (currentSection is null)
                return false;

            // Comprobar las dependencias antes de modificar cualquier propiedad.
            // Omitir el tipo o enviar el actual permite mantener las relaciones existentes.
            if (solutionCenterTypeId.HasValue &&
                solutionCenterTypeId.Value != currentSection.solution_center_type_id)
            {
                if (await HasAssociations(section.section_id))
                    throw new ValidationException(
                        "No se puede cambiar el tipo de la sección porque tiene centros de solución, productos o configuraciones de inventario asociados.");

                currentSection.solution_center_type_id = solutionCenterTypeId.Value;
            }

            currentSection.section_name =
                section.section_name;

            currentSection.is_active =
                section.is_active;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return true;
        }

        public async Task<bool> HasAssociations(
            long sectionId)
        {
            // Una asignación sigue existiendo aunque no tenga productos o esté inactiva.
            if (await _context.SolutionCenterSections
                .AsNoTracking()
                .AnyAsync(item => item.section_id == sectionId))
                return true;

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
