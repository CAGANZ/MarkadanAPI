namespace Markadan.Application.DTOs.Payment;

public record InitiateCheckoutResponseDTO(
    string ConversationId,
    string Token,
    string CheckoutFormContent
);
