using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DemoMVC.Services;

public sealed class SmtpEmailSender(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailSender> logger) : IAppEmailSender
{
    private readonly EmailSettings _settings = options.Value;

    public async Task<bool> SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            logger.LogWarning("Email was not sent because SMTP Host or FromAddress is not configured.");
            return false;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(_settings.UserName))
        {
            client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (SmtpException exception)
        {
            logger.LogError(exception, "SMTP delivery failed.");
            return false;
        }
    }
}
