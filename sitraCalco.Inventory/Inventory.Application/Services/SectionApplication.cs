using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class SectionApplication : ISectionApplication
    {
        private readonly ISectionRepository _sectionRepository;

        public SectionApplication(
            ISectionRepository sectionRepository)
        {
            _sectionRepository = sectionRepository;
        }

        /// <summary>
        /// Obtiene todas las secciones registradas.
        /// </summary>
        public async Task<IEnumerable<SectionDto>> GetAll()
        {
            try
            {
                var sections =
                    await _sectionRepository.GetAll();

                return sections.Select(section =>
                    new SectionDto
                    {
                        SectionId =
                            section.section_id,

                        SectionName =
                            section.section_name,

                        IsActive =
                            section.is_active
                    });
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Obtiene una sección por su identificador.
        /// </summary>
        public async Task<SectionDto?> GetById(
            long sectionId)
        {
            try
            {
                if (sectionId <= 0)
                    return null;

                var section =
                    await _sectionRepository
                        .GetById(sectionId);

                if (section is null)
                    return null;

                return new SectionDto
                {
                    SectionId =
                        section.section_id,

                    SectionName =
                        section.section_name,

                    IsActive =
                        section.is_active
                };
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva sección.
        /// Toda sección nueva se crea activa.
        /// </summary>
        public async Task<long> Create(
            CreateSectionDto request)
        {
            try
            {
                if (request is null)
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.SectionName))
                    return 0;

                var sectionName =
                    request.SectionName.Trim();

                var nameExists =
                    await _sectionRepository
                        .ExistsByName(sectionName);

                if (nameExists)
                    return 0;

                var section = new Section
                {
                    section_name =
                        sectionName,

                    // Toda sección nueva se crea activa.
                    is_active = true
                };

                return await _sectionRepository
                    .CreateSection(section);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Actualiza el nombre y el estado de una sección.
        /// </summary>
        public async Task<bool> Update(
            long sectionId,
            UpdateSectionDto request)
        {
            try
            {
                if (sectionId <= 0)
                    return false;

                if (request is null)
                    return false;

                if (string.IsNullOrWhiteSpace(
                    request.SectionName))
                    return false;

                var currentSection =
                    await _sectionRepository
                        .GetById(sectionId);

                if (currentSection is null)
                    return false;

                var sectionName =
                    request.SectionName.Trim();

                // Validar el nombre excluyendo
                // la misma sección que se actualiza.
                var nameExists =
                    await _sectionRepository
                        .ExistsByName(
                            sectionName,
                            sectionId);

                if (nameExists)
                    return false;

                var section = new Section
                {
                    section_id =
                        sectionId,

                    section_name =
                        sectionName,

                    is_active =
                        request.IsActive
                };

                return await _sectionRepository
                    .UpdateSection(section);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Elimina una sección únicamente cuando
        /// no tiene registros asociados.
        /// </summary>
        public async Task<bool> Delete(
            long sectionId)
        {
            try
            {
                if (sectionId <= 0)
                    return false;

                var section =
                    await _sectionRepository
                        .GetById(sectionId);

                if (section is null)
                    return false;

                var hasAssociations =
                    await _sectionRepository
                        .HasAssociations(sectionId);

                if (hasAssociations)
                    return false;

                return await _sectionRepository
                    .DeleteSection(sectionId);
            }
            catch
            {
                throw;
            }
        }
    }
}