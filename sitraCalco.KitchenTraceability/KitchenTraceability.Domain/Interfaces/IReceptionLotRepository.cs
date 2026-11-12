using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IReceptionLotRepository : IRepository<ReceptionLot>
    {
        Task<ReceptionLot?> GetByReceptionLotId(long receptionLotId);
    }
}