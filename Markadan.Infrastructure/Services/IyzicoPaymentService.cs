using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Markadan.Application.Abstractions;
using Markadan.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Markadan.Infrastructure.Services;

// iyzico REST API entegrasyonu — SDK kullanmadan HttpClient ile
public sealed class IyzicoPaymentService : IPaymentService
{
    private readonly HttpClient _http;
    private readonly IyzicoOptions _opts;
    private readonly ILogger<IyzicoPaymentService> _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public IyzicoPaymentService(HttpClient http, IOptions<IyzicoOptions> opts, ILogger<IyzicoPaymentService> log)
    {
        _http = http;
        _opts = opts.Value;
        _log  = log;
    }

    public async Task<PaymentInitiateResult> InitiateAsync(PaymentInitiateRequest req, CancellationToken ct = default)
    {
        var body = new
        {
            locale              = "tr",
            conversationId      = req.ConversationId,
            price               = FormatPrice(req.TotalAmount),
            paidPrice           = FormatPrice(req.TotalAmount),
            currency            = "TRY",
            basketId            = req.ConversationId,
            paymentGroup        = "PRODUCT",
            callbackUrl         = _opts.CallbackUrl,
            enabledInstallments = new[] { 1, 2, 3, 6, 9 },
            buyer = new
            {
                id                  = req.UserId.ToString(),
                name                = req.UserName,
                surname             = req.UserSurname,
                gsmNumber           = "+905000000000",
                email               = req.UserEmail,
                identityNumber      = "11111111111",  // TC kimlik — production'da kullanıcıdan alınmalı
                registrationAddress = req.ShippingAddress,
                ip                  = req.UserIp,
                city                = req.ShippingCity,
                country             = req.ShippingCountry
            },
            shippingAddress = new
            {
                contactName = req.ShippingContactName,
                city        = req.ShippingCity,
                country     = req.ShippingCountry,
                address     = req.ShippingAddress
            },
            billingAddress = new
            {
                contactName = req.ShippingContactName,
                city        = req.ShippingCity,
                country     = req.ShippingCountry,
                address     = req.ShippingAddress
            },
            basketItems = req.Items.Select(i => new
            {
                id       = i.Id,
                name     = i.Name,
                category1 = i.Category,
                itemType  = "PHYSICAL",
                price     = FormatPrice(i.Price)
            }).ToList()
        };

        try
        {
            var response = await PostAsync<IyzicoCheckoutFormResponse>(
                "/payment/iyzipos/checkoutform/initialize", body, ct);

            if (response?.Status != "success")
            {
                _log.LogWarning("iyzico initiate başarısız: {Error}", response?.ErrorMessage);
                return new PaymentInitiateResult(false, null, null, response?.ErrorMessage ?? "Ödeme başlatılamadı.");
            }

            return new PaymentInitiateResult(true, response.Token, response.CheckoutFormContent, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "iyzico initiate exception");
            return new PaymentInitiateResult(false, null, null, "Ödeme başlatılamadı.");
        }
    }

    public async Task<PaymentConfirmResult> ConfirmAsync(string token, CancellationToken ct = default)
    {
        var body = new { locale = "tr", token };

        try
        {
            var response = await PostAsync<IyzicoCheckoutFormDetailResponse>(
                "/payment/iyzipos/checkoutform/detail", body, ct);

            if (response?.Status != "success" || response.PaymentStatus != "SUCCESS")
            {
                _log.LogWarning("iyzico confirm başarısız: {PaymentStatus}", response?.PaymentStatus);
                return new PaymentConfirmResult(false, null, null, response?.ErrorMessage ?? "Ödeme başarısız.");
            }

            return new PaymentConfirmResult(true, response.PaymentId, response.ConversationId, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "iyzico confirm exception");
            return new PaymentConfirmResult(false, null, null, "Ödeme doğrulanamadı.");
        }
    }

    public async Task<bool> CancelPaymentAsync(string paymentId, decimal amount, string ip, CancellationToken ct = default)
    {
        var body = new
        {
            locale         = "tr",
            conversationId = $"cancel-{paymentId}",
            paymentId,
            ip
        };

        try
        {
            var response = await PostAsync<IyzicoBaseResponse>("/payment/cancel", body, ct);
            if (response?.Status != "success")
            {
                _log.LogWarning("iyzico iptal başarısız: {Error}", response?.ErrorMessage);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "iyzico iptal exception");
            return false;
        }
    }

    // ── HTTP yardımcıları ────────────────────────────────────────────────────────

    private async Task<T?> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        var json           = JsonSerializer.Serialize(body, JsonOpts);
        var randomString   = Guid.NewGuid().ToString("N")[..8];
        var authorization  = BuildAuthorization(randomString, json);

        using var request  = new HttpRequestMessage(HttpMethod.Post, _opts.BaseUrl.TrimEnd('/') + path);
        request.Content    = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("IYZWS", authorization);
        request.Headers.Add("x-iyzi-rnd", randomString);
        request.Headers.Add("x-iyzi-client-version", "iyzipay-dotnet-custom");

        using var response = await _http.SendAsync(request, ct);
        var responseBody   = await response.Content.ReadAsStringAsync(ct);
        _log.LogDebug("iyzico {Path} → HTTP {Status}", path, (int)response.StatusCode);
        return JsonSerializer.Deserialize<T>(responseBody, JsonOpts);
    }

    private string BuildAuthorization(string randomString, string requestBody)
    {
        // iyzico imza: Base64(SHA256(apiKey + secretKey + randomString + PKIString))
        // PKIString = JSON body'nin [key=value,...] formatına dönüştürülmüş hali (anahtarlar alfabetik)
        using var doc = JsonDocument.Parse(requestBody);
        var pki  = ToPKIString(doc.RootElement);
        var data = _opts.ApiKey + _opts.SecretKey + randomString + pki;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return $"{_opts.ApiKey}:{Convert.ToBase64String(hash)}";
    }

    private static string ToPKIString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => "[" + string.Join(",",
            el.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)
              .Select(p => $"{p.Name}={ToPKIString(p.Value)}")) + "]",

        JsonValueKind.Array  => "[" + string.Join(", ",
            el.EnumerateArray().Select(ToPKIString)) + "]",

        JsonValueKind.String => el.GetString() ?? "",
        _                    => el.GetRawText()
    };

    private static string FormatPrice(decimal price) =>
        price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    // ── Response modelleri ───────────────────────────────────────────────────────

    private record IyzicoBaseResponse(
        [property: JsonPropertyName("status")]       string? Status,
        [property: JsonPropertyName("errorCode")]    string? ErrorCode,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage
    );

    private record IyzicoCheckoutFormResponse(
        [property: JsonPropertyName("status")]             string? Status,
        [property: JsonPropertyName("errorMessage")]       string? ErrorMessage,
        [property: JsonPropertyName("token")]              string? Token,
        [property: JsonPropertyName("checkoutFormContent")] string? CheckoutFormContent
    );

    private record IyzicoCheckoutFormDetailResponse(
        [property: JsonPropertyName("status")]          string? Status,
        [property: JsonPropertyName("errorMessage")]    string? ErrorMessage,
        [property: JsonPropertyName("paymentStatus")]   string? PaymentStatus,
        [property: JsonPropertyName("paymentId")]       string? PaymentId,
        [property: JsonPropertyName("conversationId")]  string? ConversationId
    );
}
