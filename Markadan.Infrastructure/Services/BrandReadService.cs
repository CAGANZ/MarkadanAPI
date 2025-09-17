using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Brands;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class BrandReadService : IBrandReadService
{
    private readonly MarkadanDbContext _db;
    public BrandReadService(MarkadanDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<IReadOnlyList<BrandDTO>> GetAllAsync(CancellationToken ct = default)
        => await _db.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BrandDTO(b.Id, b.Name, b.Description, b.ImageUrl))
            .ToListAsync(ct);

    public async Task<BrandDTO?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Brands
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BrandDTO(b.Id, b.Name, b.Description, b.ImageUrl))
            .SingleOrDefaultAsync(ct);
}