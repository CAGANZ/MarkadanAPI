namespace Markadan.Application.DTOs.Products.Variants;

// ── Opsiyon (eksen) ──────────────────────────────────────────────────────────

public record AdminProductOptionDTO(
    int Id,
    string Name,
    int SortOrder,
    IReadOnlyList<AdminOptionValueDTO> Values
);

public record AdminOptionValueDTO(
    int Id,
    string Value,
    int SortOrder
);

public record CreateOptionDTO(
    string Name,
    int SortOrder,
    IReadOnlyList<CreateOptionValueDTO>? Values
);

public record CreateOptionValueDTO(
    string Value,
    int SortOrder
);

// ── Varyant ──────────────────────────────────────────────────────────────────

public record AdminVariantDTO(
    int Id,
    string? Sku,
    decimal? Price,           // null → ürün fiyatı geçerli
    int Stock,
    string? ImageUrl,
    bool IsActive,
    IReadOnlyList<int> OptionValueIds
);

public record CreateVariantDTO(
    string? Sku,
    decimal? Price,
    int Stock,
    string? ImageUrl,
    bool IsActive,
    IReadOnlyList<int> OptionValueIds
);

public record UpdateVariantDTO(
    string? Sku,
    decimal? Price,
    int Stock,
    string? ImageUrl,
    bool IsActive,
    IReadOnlyList<int> OptionValueIds
);
