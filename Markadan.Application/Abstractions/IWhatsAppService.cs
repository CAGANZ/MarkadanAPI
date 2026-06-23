namespace Markadan.Application.Abstractions;

public interface IWhatsAppService
{
    Task SendAsync(string toPhone, string message, CancellationToken ct = default);
}
