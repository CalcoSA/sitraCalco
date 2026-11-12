using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IReceptionTemperatureMeasurementRepository : IRepository<ReceptionTemperatureMeasurement>
    {
        Task<IEnumerable<ReceptionTemperatureMeasurement>> GetByReceptionLotId(long receptionLotId);
    }
}