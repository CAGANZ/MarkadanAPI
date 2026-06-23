namespace Markadan.Application.Options;

public sealed class WhatsAppOptions
{
    public bool Enabled { get; init; } = false;
    public string ApiKey { get; init; } = null!;
    public string FromNumber { get; init; } = null!;
}
