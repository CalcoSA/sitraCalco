using Authentication.Domain.Models;
using Authentication.Domain.Dtos;
using AutoMapper;

namespace Authentication.Infrastructure.Persistance.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Menuoption, MenuOptionDto>();
            CreateMap<MenuOptionDto, Menuoption>();
            CreateMap<Role, RoleDto>();
            CreateMap<RoleDto, Role>();
            CreateMap<Role, CreateRoleDto>();
            CreateMap<CreateRoleDto, Role>();
            CreateMap<Role, UpdateRoleDto>();
            CreateMap<UpdateRoleDto, Role>();
            CreateMap<UserDto, User>();
            CreateMap<User, CreateUserDto>();
            CreateMap<CreateUserDto, User>();
            CreateMap<User, UpdateUserDto>();
            CreateMap<UpdateUserDto, User>();
            CreateMap<Role, RoleDetailDto>()
                .ForMember(
                    dest => dest.MenuOptions,
                    opt => opt.MapFrom(src => src.Rolemenuoptions
                        .Where(x => x.IdMenuOptionNavigation.StatusMenuOption == 1)
                        .OrderBy(x => x.IdMenuOptionNavigation.OrderMenuOption)
                        .Select(x => x.IdMenuOptionNavigation)
                    )
                );
            CreateMap<User, UserDto>()
                .ForMember(
                    dest => dest.IdRole,
                    opt => opt.MapFrom(src => src.Userroles.Any()
                        ? src.Userroles.First().IdRoleNavigation.IdRole
                        : 0)
                )
                .ForMember(
                    dest => dest.NameRole,
                    opt => opt.MapFrom(src => src.Userroles.Any()
                        ? src.Userroles.First().IdRoleNavigation.NameRole
                        : null)
                )
                .ForMember(
                    dest => dest.StatusRole,
                    opt => opt.MapFrom(src => src.Userroles.Any()
                        ? src.Userroles.First().IdRoleNavigation.StatusRole
                        : (sbyte)0)
                );
        }
    }
}