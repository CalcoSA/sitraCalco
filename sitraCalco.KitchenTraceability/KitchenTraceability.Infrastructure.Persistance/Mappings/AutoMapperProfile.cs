using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;
using AutoMapper;

namespace KitchenTraceability.Infrastructure.Persistance.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Supplier, CreateSupplierDto>();
            CreateMap<CreateSupplierDto, Supplier>();
            CreateMap<Supplier, UpdateSupplierDto>();
            CreateMap<UpdateSupplierDto, Supplier>();
            CreateMap<Supplier, SupplierDto>();
            CreateMap<SupplierDto, Supplier>();
            CreateMap<Product, ProductDto>()
                .ForMember(
                    dest => dest.product_reception_configuration,
                    opt => opt.MapFrom(src => src.productReceptionConfigurations.FirstOrDefault()))
                .ForMember(
                    dest => dest.product_temperature_configuration,
                    opt => opt.MapFrom(src => src.productTemperatureConfigurations.FirstOrDefault()));
            CreateMap<ProductDto, Product>();
            CreateMap<Product, ProductDetailDto>();
            CreateMap<ProductDetailDto, Product>();
            CreateMap<ProductReceptionConfiguration, ProductDetailDto>();
            CreateMap<ProductDetailDto, ProductReceptionConfiguration>();
            CreateMap<ProductTemperatureConfiguration, ProductDetailDto>();
            CreateMap<ProductDetailDto, ProductTemperatureConfiguration>();
            CreateMap<ProductReceptionConfiguration, ProductReceptionConfigurationDto>();
            CreateMap<ProductReceptionConfigurationDto, ProductReceptionConfiguration>();
            CreateMap<ProductTemperatureConfiguration, ProductTemperatureConfigurationDto>();
            CreateMap<ProductTemperatureConfigurationDto, ProductTemperatureConfiguration>();
            CreateMap<ProductReceptionConfiguration, CreateProductReceptionConfigurationDto>();
            CreateMap<CreateProductReceptionConfigurationDto, ProductReceptionConfiguration>();
            CreateMap<ProductTemperatureConfiguration, CreateProductTemperatureConfigurationDto>();
            CreateMap<CreateProductTemperatureConfigurationDto, ProductTemperatureConfiguration>();
            CreateMap<ProductReceptionConfiguration, UpdateProductReceptionConfigurationDto>();
            CreateMap<UpdateProductReceptionConfigurationDto, ProductReceptionConfiguration>();
            CreateMap<ProductTemperatureConfiguration, UpdateProductTemperatureConfigurationDto>();
            CreateMap<UpdateProductTemperatureConfigurationDto, ProductTemperatureConfiguration>();
            CreateMap<ReceptionType, ReceptionTypeDto>();
            CreateMap<ReceptionTypeDto, ReceptionType>();
            CreateMap<ReceptionType, CreateReceptionTypeDto>();
            CreateMap<CreateReceptionTypeDto, ReceptionType>();
            CreateMap<ReceptionType, UpdateReceptionTypeDto>();
            CreateMap<UpdateReceptionTypeDto, ReceptionType>();
        }
    }
}