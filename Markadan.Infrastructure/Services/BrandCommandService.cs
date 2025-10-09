using System.Threading;
using Markadan.Application.Exceptions;
using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Brands;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class BrandCommandService : IBrandCommandService
{
    private readonly MarkadanDbContext _db;

    public BrandCommandService(MarkadanDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<BrandDTO> CreateAsync(BrandCreateDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Name cannot be empty.");


        var name = dto.Name.Trim();
        var exists = await _db.Brands.AnyAsync(b => b.Name == name, ct);
        if (exists)
            throw new BusinessRuleException($"Brand '{name}' already exists.");

        var entity = new Brand
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description!.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl!.Trim()
        };

        _db.Brands.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new BrandDTO(entity.Id, entity.Name, entity.Description, entity.ImageUrl);
    }

    public async Task<BrandDTO?> UpdateAsync(BrandUpdateDTO dto, CancellationToken ct = default)
    {
        var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == dto.Id, ct);
        if (entity is null) 
            return null;
        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0)
                throw new InvalidOperationException("Name cannot be empty.");

            var exists = await _db.Brands.AnyAsync(b => b.Id != dto.Id && b.Name == name, ct);
            if (exists) 
                throw new BusinessRuleException($"Brand '{name}' already exists.");
            entity.Name = name;
        }
        if (dto.Description is not null)
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (dto.ImageUrl is not null)
            entity.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();

        await _db.SaveChangesAsync(ct);
        return new BrandDTO(entity.Id, entity.Name, entity.Description, entity.ImageUrl);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var exists = await _db.Brands.AnyAsync(b => b.Id == id, ct);
        if (!exists)
            return false;

        var inUse = await _db.Products.AnyAsync(p => p.BrandId == id, ct);
        if (inUse)
            throw new BusinessRuleException("Brand has products and cannot be deletd.");
            
        await _db.Brands.Where(b => b.Id == id).ExecuteDeleteAsync(ct);
        return true;
    }
}
