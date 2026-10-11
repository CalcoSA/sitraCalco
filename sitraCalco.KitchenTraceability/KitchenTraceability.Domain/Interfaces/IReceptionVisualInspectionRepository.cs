using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IReceptionVisualInspectionRepository : IRepository<ReceptionVisualInspection>
    {
        Task<ReceptionVisualInspection?> GetByReceptionLotId(long receptionLotId);
    }
}