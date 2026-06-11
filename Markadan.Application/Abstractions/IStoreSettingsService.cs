using Markadan.Application.DTOs.Settings;

namespace Markadan.Application.Abstractions;

public interface IStoreSettingsService
{
    Task<StoreSettingsDTO> GetAsync(CancellationToken ct = default);
    Task<StoreSettingsDTO> UpdateAsync(UpdateStoreSettingsDTO dto, CancellationToken ct = default);
}
