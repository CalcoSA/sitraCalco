using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface IReceptionTypeApplication
    {
        Task<Page<ReceptionTypeDto>> GetAll(int page, int take, string? search = null);
        Task<IEnumerable<ReceptionTypeDto>> GetOptions(string? search = null);
        Task<ReceptionTypeDto?> GetById(long id);
        Task<bool> Create(CreateReceptionTypeDto request);
        Task<bool> Update(long id, UpdateReceptionTypeDto request);
        Task<bool> Delete(long id);
    }
}