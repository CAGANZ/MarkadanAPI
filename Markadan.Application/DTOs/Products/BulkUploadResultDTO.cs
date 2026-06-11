namespace Markadan.Application.DTOs.Products;

public record BulkUploadResultDTO(
    int Succeeded,
    int Failed,
    List<BulkUploadErrorDTO> Errors
);

public record BulkUploadErrorDTO(int Row, string Reason);
