using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface IReceptionLotApplication
    {
        Task<CreateReceptionLotResultDto> CreateLot(CreateReceptionLotDto request, string user);
        Task<CreateReceptionLotEvaluationResultDto> Create(CreateReceptionLotEvaluationDto request, string user);
    }
}