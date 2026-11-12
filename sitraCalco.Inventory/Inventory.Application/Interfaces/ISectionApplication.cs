using Inventory.Domain.Dtos;

namespace Inventory.Application.Interfaces
{
    public interface ISectionApplication
    {
        Task<IEnumerable<SectionDto>?> GetAll(long? solutionCenterTypeId = null);

        Task<SectionDto?> GetById(long sectionId);

        Task<long> Create(CreateSectionDto request);

        Task<bool> Update(
            long sectionId,
            UpdateSectionDto request);

        Task<bool> Delete(long sectionId);
    }
}
