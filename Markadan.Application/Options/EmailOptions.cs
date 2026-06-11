namespace Markadan.Application.Options;

public sealed class EmailOptions
{
    public string SmtpHost { get; init; } = null!;
    public int SmtpPort { get; init; } = 587;
    public string SmtpUser { get; init; } = null!;
    public string SmtpPassword { get; init; } = null!;
    public string FromAddress { get; init; } = null!;
    public string FromName { get; init; } = "Markadan";
}
