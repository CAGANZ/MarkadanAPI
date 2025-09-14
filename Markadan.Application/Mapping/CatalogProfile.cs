using AutoMapper;
using Markadan.Application.DTOs.Brands;
using Markadan.Application.DTOs.Categories;
using Markadan.Application.DTOs.Products;
using Markadan.Domain.Models;

namespace Markadan.Application.Mapping;

public sealed class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<Product, ProductListDTO>()
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand!.Name))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category!.Name));

        CreateMap<Product, ProductDetailDTO>()
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand!.Name))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category!.Name));

        CreateMap<Brand, BrandDTO>();
        CreateMap<Category, CategoryDTO>();
    }
}
