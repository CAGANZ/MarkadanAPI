using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Addresses;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class AddressService : IAddressService
{
    private readonly MarkadanDbContext _db;

    public AddressService(MarkadanDbContext db) => _db = db;

    public async Task<IReadOnlyList<AddressDTO>> GetAllAsync(int userId, CancellationToken ct = default)
        => await _db.Addresses
            .Where(a => a.AppUserId == userId)
            .Select(a => ToDTO(a))
            .ToListAsync(ct);

    public async Task<AddressDTO?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        var a = await _db.Addresses
            .Where(a => a.Id == id && a.AppUserId == userId)
            .FirstOrDefaultAsync(ct);
        return a is null ? null : ToDTO(a);
    }

    public async Task<AddressDTO> CreateAsync(AddressCreateDTO dto, int userId, CancellationToken ct = default)
    {
        var entity = new Address
        {
            AppUserId  = userId,
            AppUser    = default!,       // EF FK ile bağlar, navigation yüklenmez
            AddressName = dto.AddressName.Trim(),
            Street      = dto.Street?.Trim() ?? "",
            City        = dto.City?.Trim() ?? "",
            State       = dto.State?.Trim() ?? "",
            PostalCode  = dto.PostalCode?.Trim() ?? "",
            Country     = dto.Country?.Trim() ?? ""
        };

        _db.Addresses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToDTO(entity);
    }

    public async Task<AddressDTO?> UpdateAsync(AddressUpdateDTO dto, int userId, CancellationToken ct = default)
    {
        var entity = await _db.Addresses
            .Where(a => a.Id == dto.Id && a.AppUserId == userId)
            .FirstOrDefaultAsync(ct);

        if (entity is null) return null;

        if (dto.AddressName is not null) entity.AddressName = dto.AddressName.Trim();
        if (dto.Street     is not null) entity.Street      = dto.Street.Trim();
        if (dto.City       is not null) entity.City        = dto.City.Trim();
        if (dto.State      is not null) entity.State       = dto.State.Trim();
        if (dto.PostalCode is not null) entity.PostalCode  = dto.PostalCode.Trim();
        if (dto.Country    is not null) entity.Country     = dto.Country.Trim();

        await _db.SaveChangesAsync(ct);
        return ToDTO(entity);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var entity = await _db.Addresses
            .Where(a => a.Id == id && a.AppUserId == userId)
            .FirstOrDefaultAsync(ct);

        if (entity is null) return false;

        // Tamamlanmış siparişte kullanılıyorsa silinemez (snapshot alanlar korunsa da iş kuralı)
        var usedInOrder = await _db.Carts
            .AnyAsync(c => c.ShippingAddressId == id && c.Status == CartStatus.Ordered, ct);

        if (usedInOrder)
            throw new BusinessRuleException("Bu adres tamamlanmış bir siparişe ait olduğundan silinemez.");

        _db.Addresses.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static AddressDTO ToDTO(Address a) =>
        new(a.Id, a.AddressName, a.Street, a.City, a.State, a.PostalCode, a.Country);
}
