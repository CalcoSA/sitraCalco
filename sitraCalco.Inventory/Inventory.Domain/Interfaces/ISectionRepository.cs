using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface ISectionRepository
    {
        Task<IEnumerable<Section>> GetAll();

        Task<Section?> GetById(long sectionId);

        Task<bool> ExistsByName(
            string sectionName,
            long? excludeSectionId = null);

        Task<long> CreateSection(
            Section section);

        Task<bool> UpdateSection(
            Section section);

        Task<bool> HasAssociations(
            long sectionId);

        Task<bool> DeleteSection(
            long sectionId);
    }
}