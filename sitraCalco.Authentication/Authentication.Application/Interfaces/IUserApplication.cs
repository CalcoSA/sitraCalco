using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface IUserApplication
    {
        Task<PagedDto<UserDto>> GetAll(int page, int take, string? search);
        Task<UserDto> GetById(int idUser);
        Task<AuditResultDto> Create(CreateUserDto entity);
        Task<AuditResultDto> Update(UpdateUserDto entity);
        Task<AuditResultDto> Delete(int idUser);
    }
}
