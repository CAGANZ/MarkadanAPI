using Markadan.Application.Abstractions;
using Markadan.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Markadan.Infrastructure.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailService> _log;

    public SmtpEmailService(IOptions<EmailOptions> opts, ILogger<SmtpEmailService> log)
    {
        _opts = opts.Value;
        _log  = log;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_opts.SmtpHost, _opts.SmtpPort, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_opts.SmtpUser, _opts.SmtpPassword, ct);
            await client.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Mail gönderilemedi: {To} — {Subject}", toEmail, subject);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
