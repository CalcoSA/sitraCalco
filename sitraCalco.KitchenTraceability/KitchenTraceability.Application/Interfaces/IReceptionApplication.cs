using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface IReceptionApplication
    {
        Task<ReceptionDetailDto?> GetById(long receptionId);
        Task<CreateReceptionResultDto> Create(CreateReceptionDto request, string user);
    }
}