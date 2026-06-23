using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Markadan.Application.Abstractions;
using Markadan.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Markadan.Infrastructure.Services;

public sealed class TelnyxWhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _opts;
    private readonly ILogger<TelnyxWhatsAppService> _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TelnyxWhatsAppService(HttpClient http, IOptions<WhatsAppOptions> opts, ILogger<TelnyxWhatsAppService> log)
    {
        _http = http;
        _opts = opts.Value;
        _log  = log;
    }

    public async Task SendAsync(string toPhone, string message, CancellationToken ct = default)
    {
        if (!_opts.Enabled)
        {
            _log.LogInformation("WhatsApp devre dışı — mesaj loglanıyor. To: {To} | {Message}", toPhone, message);
            return;
        }

        var body = JsonSerializer.Serialize(new
        {
            from = _opts.FromNumber,
            to   = toPhone,
            text = message,
            type = "SMS" // Telnyx: WhatsApp channel için messaging_profile_id gerekir
        }, JsonOpts);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.telnyx.com/v2/messages");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);

        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _log.LogWarning("Telnyx WhatsApp başarısız: {Status} | {Error}", (int)response.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Telnyx WhatsApp gönderim hatası. To: {To}", toPhone);
        }
    }
}
