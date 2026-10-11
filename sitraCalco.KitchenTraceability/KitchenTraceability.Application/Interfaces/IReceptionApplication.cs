using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface IReceptionApplication
    {
        Task<CreateReceptionResultDto> Create(CreateReceptionDto request, string user);
    }
}