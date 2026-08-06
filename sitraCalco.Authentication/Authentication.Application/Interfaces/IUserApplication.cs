using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface IUserApplication
    {
        Task<PagedDto<UserDto>> GetAll(int page, int take, string? search);
        Task<UserDto> GetById(int idUser);
        Task<bool> Create(CreateUserDto entity);
        Task<bool> Update(UpdateUserDto entity);
        Task<bool> Delete(int idUser);
    }
}