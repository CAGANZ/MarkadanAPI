namespace Markadan.Application.Abstractions;

public interface IPaymentService
{
    Task<PaymentInitiateResult> InitiateAsync(PaymentInitiateRequest request, CancellationToken ct = default);
    Task<PaymentConfirmResult> ConfirmAsync(string token, CancellationToken ct = default);
    Task<bool> CancelPaymentAsync(string paymentId, decimal amount, string ip, CancellationToken ct = default);
}

public record PaymentInitiateRequest(
    string ConversationId,
    decimal TotalAmount,
    int UserId,
    string UserName,
    string UserSurname,
    string UserEmail,
    string UserIp,
    string ShippingContactName,
    string ShippingCity,
    string ShippingCountry,
    string ShippingAddress,
    List<PaymentBasketItem> Items
);

public record PaymentBasketItem(string Id, string Name, string Category, decimal Price);

public record PaymentInitiateResult(bool Success, string? Token, string? CheckoutFormContent, string? Error);

public record PaymentConfirmResult(bool Success, string? PaymentId, string? ConversationId, string? Error);
