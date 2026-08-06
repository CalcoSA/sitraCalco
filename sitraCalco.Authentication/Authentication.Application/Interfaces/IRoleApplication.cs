using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface IRoleApplication
    {
        Task<IEnumerable<RoleDto>> GetAll();
        Task<RoleDetailDto> GetById(int idRole);
        Task<bool> Create(CreateRoleDto entity);
        Task<bool> Update(UpdateRoleDto entity);
        Task<bool> Delete(int Id);
    }
}