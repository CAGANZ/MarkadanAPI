using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products.Variants;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class ProductVariantService : IProductVariantService
{
    private readonly MarkadanDbContext _db;

    public ProductVariantService(MarkadanDbContext db) => _db = db;

    // ── Opsiyonlar ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AdminProductOptionDTO>> GetOptionsAsync(int productId, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(productId, ct);

        return await _db.ProductOptions
            .AsNoTracking()
            .Where(o => o.ProductId == productId)
            .OrderBy(o => o.SortOrder)
            .Select(o => new AdminProductOptionDTO(
                o.Id,
                o.Name,
                o.SortOrder,
                o.Values.OrderBy(v => v.SortOrder)
                    .Select(v => new AdminOptionValueDTO(v.Id, v.Value, v.SortOrder)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<AdminProductOptionDTO> AddOptionAsync(int productId, CreateOptionDTO dto, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(productId, ct);

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessRuleException("Seçenek adı boş olamaz.");

        if (await _db.ProductOptions.AnyAsync(o => o.ProductId == productId && o.Name == dto.Name, ct))
            throw new BusinessRuleException($"'{dto.Name}' adlı seçenek bu üründe zaten var.");

        var option = new ProductOption
        {
            ProductId = productId,
            Name      = dto.Name.Trim(),
            SortOrder = dto.SortOrder,
        };

        if (dto.Values is not null)
        {
            foreach (var v in dto.Values)
            {
                if (string.IsNullOrWhiteSpace(v.Value)) continue;
                option.Values.Add(new ProductOptionValue { Value = v.Value.Trim(), SortOrder = v.SortOrder });
            }
        }

        _db.ProductOptions.Add(option);
        await _db.SaveChangesAsync(ct);

        return new AdminProductOptionDTO(
            option.Id, option.Name, option.SortOrder,
            option.Values.OrderBy(v => v.SortOrder)
                .Select(v => new AdminOptionValueDTO(v.Id, v.Value, v.SortOrder)).ToList());
    }

    public async Task<bool> DeleteOptionAsync(int productId, int optionId, CancellationToken ct = default)
    {
        var option = await _db.ProductOptions
            .FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId, ct);
        if (option is null) return false;

        // Bu eksenin değerlerinden biri bir varyantta kullanılıyorsa engelle
        bool inUse = await _db.ProductVariantValues
            .AnyAsync(vv => vv.OptionValue.OptionId == optionId, ct);
        if (inUse)
            throw new BusinessRuleException("Bu seçenek bir veya daha fazla varyantta kullanılıyor; önce varyantları silin.");

        _db.ProductOptions.Remove(option);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AdminOptionValueDTO> AddOptionValueAsync(int productId, int optionId, CreateOptionValueDTO dto, CancellationToken ct = default)
    {
        var option = await _db.ProductOptions
            .FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId, ct)
            ?? throw new KeyNotFoundException("Seçenek bulunamadı.");

        if (string.IsNullOrWhiteSpace(dto.Value))
            throw new BusinessRuleException("Değer boş olamaz.");

        if (await _db.ProductOptionValues.AnyAsync(v => v.OptionId == optionId && v.Value == dto.Value, ct))
            throw new BusinessRuleException($"'{dto.Value}' değeri bu seçenekte zaten var.");

        var value = new ProductOptionValue
        {
            OptionId  = option.Id,
            Value     = dto.Value.Trim(),
            SortOrder = dto.SortOrder,
        };
        _db.ProductOptionValues.Add(value);
        await _db.SaveChangesAsync(ct);

        return new AdminOptionValueDTO(value.Id, value.Value, value.SortOrder);
    }

    public async Task<bool> DeleteOptionValueAsync(int productId, int valueId, CancellationToken ct = default)
    {
        var value = await _db.ProductOptionValues
            .FirstOrDefaultAsync(v => v.Id == valueId && v.Option.ProductId == productId, ct);
        if (value is null) return false;

        bool inUse = await _db.ProductVariantValues.AnyAsync(vv => vv.OptionValueId == valueId, ct);
        if (inUse)
            throw new BusinessRuleException("Bu değer bir varyantta kullanılıyor; önce varyantı silin.");

        _db.ProductOptionValues.Remove(value);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── Varyantlar ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AdminVariantDTO>> GetVariantsAsync(int productId, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(productId, ct);

        return await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Id)
            .Select(v => new AdminVariantDTO(
                v.Id, v.Sku, v.Price, v.Stock, v.ImageUrl, v.IsActive,
                v.SelectedValues.Select(sv => sv.OptionValueId).ToList()))
            .ToListAsync(ct);
    }

    public async Task<AdminVariantDTO> AddVariantAsync(int productId, CreateVariantDTO dto, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(productId, ct);
        await ValidateOptionValuesAsync(productId, dto.OptionValueIds, ct);
        await EnsureCombinationUniqueAsync(productId, dto.OptionValueIds, excludeVariantId: null, ct);

        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku       = dto.Sku?.Trim(),
            Price     = dto.Price,
            Stock     = dto.Stock,
            ImageUrl  = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            IsActive  = dto.IsActive,
        };
        foreach (var ovId in dto.OptionValueIds.Distinct())
            variant.SelectedValues.Add(new ProductVariantValue { OptionValueId = ovId });

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync(ct);

        return new AdminVariantDTO(
            variant.Id, variant.Sku, variant.Price, variant.Stock, variant.ImageUrl, variant.IsActive,
            variant.SelectedValues.Select(sv => sv.OptionValueId).ToList());
    }

    public async Task<AdminVariantDTO?> UpdateVariantAsync(int productId, int variantId, UpdateVariantDTO dto, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.SelectedValues)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, ct);
        if (variant is null) return null;

        await ValidateOptionValuesAsync(productId, dto.OptionValueIds, ct);
        await EnsureCombinationUniqueAsync(productId, dto.OptionValueIds, excludeVariantId: variantId, ct);

        variant.Sku      = dto.Sku?.Trim();
        variant.Price    = dto.Price;
        variant.Stock    = dto.Stock;
        variant.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        variant.IsActive = dto.IsActive;

        // Seçili değerleri senkronla
        var desired = dto.OptionValueIds.Distinct().ToHashSet();
        var current = variant.SelectedValues.Select(sv => sv.OptionValueId).ToHashSet();

        foreach (var sv in variant.SelectedValues.Where(sv => !desired.Contains(sv.OptionValueId)).ToList())
            variant.SelectedValues.Remove(sv);
        foreach (var add in desired.Where(id => !current.Contains(id)))
            variant.SelectedValues.Add(new ProductVariantValue { VariantId = variant.Id, OptionValueId = add });

        await _db.SaveChangesAsync(ct);

        return new AdminVariantDTO(
            variant.Id, variant.Sku, variant.Price, variant.Stock, variant.ImageUrl, variant.IsActive,
            variant.SelectedValues.Select(sv => sv.OptionValueId).ToList());
    }

    public async Task<bool> DeleteVariantAsync(int productId, int variantId, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, ct);
        if (variant is null) return false;

        // Siparişte (CartItem) geçen varyant silinemez — FK Restrict; dostça mesaj ver
        bool inOrder = await _db.CartItems.AnyAsync(i => i.ProductVariantId == variantId, ct);
        if (inOrder)
            throw new BusinessRuleException("Bu varyant bir sepette/siparişte kullanılıyor; silinemez. Pasife alabilirsiniz.");

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task EnsureProductExistsAsync(int productId, CancellationToken ct)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new KeyNotFoundException("Ürün bulunamadı.");
    }

    // Seçilen değerler bu ürüne ait olmalı ve her biri farklı eksenden olmalı
    private async Task ValidateOptionValuesAsync(int productId, IReadOnlyList<int> optionValueIds, CancellationToken ct)
    {
        var ids = optionValueIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new BusinessRuleException("En az bir seçenek değeri belirtilmeli.");

        var values = await _db.ProductOptionValues
            .Where(v => ids.Contains(v.Id) && v.Option.ProductId == productId)
            .Select(v => new { v.Id, v.OptionId })
            .ToListAsync(ct);

        if (values.Count != ids.Count)
            throw new BusinessRuleException("Bir veya daha fazla seçenek değeri bu ürüne ait değil.");

        // Aynı eksenden iki değer seçilemez (ör. iki Beden)
        if (values.Select(v => v.OptionId).Distinct().Count() != values.Count)
            throw new BusinessRuleException("Her seçenekten (eksen) yalnızca bir değer seçilebilir.");
    }

    // Aynı kombinasyon başka bir varyantta olmamalı
    private async Task EnsureCombinationUniqueAsync(int productId, IReadOnlyList<int> optionValueIds, int? excludeVariantId, CancellationToken ct)
    {
        var target = optionValueIds.Distinct().OrderBy(x => x).ToList();

        var variants = await _db.ProductVariants
            .Where(v => v.ProductId == productId && (excludeVariantId == null || v.Id != excludeVariantId))
            .Select(v => new { v.Id, ValueIds = v.SelectedValues.Select(sv => sv.OptionValueId).ToList() })
            .ToListAsync(ct);

        foreach (var v in variants)
        {
            var existing = v.ValueIds.Distinct().OrderBy(x => x).ToList();
            if (existing.SequenceEqual(target))
                throw new BusinessRuleException("Bu seçenek kombinasyonuna sahip bir varyant zaten var.");
        }
    }
}
