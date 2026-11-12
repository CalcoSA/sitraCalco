using Authentication.Application.Interfaces;
using Authentication.Domain.Dtos;
using Authentication.Domain.Interfaces;
using AutoMapper;

namespace Authentication.Application.Services
{
    public class SolutionCenterApplication : ISolutionCenterApplication
    {
        private readonly ISolutionCenterRepository _repository;
        private readonly IMapper _mapper;

        public SolutionCenterApplication(ISolutionCenterRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SolutionCenterDto>> GetAll()
            => _mapper.Map<IEnumerable<SolutionCenterDto>>(await _repository.GetAll());
    }
}
