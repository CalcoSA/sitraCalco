using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface ISectionRepository
    {
        Task<IEnumerable<Section>> GetAll(long? solutionCenterTypeId = null);
        Task<IEnumerable<AvailableInventorySectionDto>> GetSections(long solutionCenterId, long inventoryConfigurationId);
        Task<Section?> GetById(long sectionId);
        Task<bool> ExistsByName(string sectionName, long? excludeSectionId = null);
        Task<long> CreateSection(Section section);
        Task<bool> UpdateSection(Section section, long? solutionCenterTypeId = null);
        Task<bool> HasAssociations(long sectionId);
        Task<bool> DeleteSection(long sectionId);
    }
}