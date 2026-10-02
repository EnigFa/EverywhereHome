using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Project.Application.Auth;

namespace Project.Infrastructure.Auth;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:Host"];
        var user = _configuration["Email:User"];
        var password = _configuration["Email:Password"];
        var from = _configuration["Email:From"];
        var port = int.TryParse(_configuration["Email:Port"], out var parsed) ? parsed : 587;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var message = new MailMessage(string.IsNullOrWhiteSpace(from) ? user : from, to, subject, body);
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
