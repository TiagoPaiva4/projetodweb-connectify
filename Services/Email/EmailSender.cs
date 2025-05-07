using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace projetodweb_connectify.Services.Email;

public class EmailSender : ICustomEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    // Constructor with dependency injection
    public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string name, string confirmationLink)
    {
        Console.WriteLine("SendRegistrationConfirmationAsync called");
        try 
        {
            var message = new MimeMessage();
            
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(name, email));
            message.Subject = "Confirm Your Connectify Registration";

            var builder = new BodyBuilder();
            
            // HTML version
            builder.HtmlBody = GetConfirmationEmailHtml(name, confirmationLink);
            
            // Plain text version
            builder.TextBody = GetConfirmationEmailText(name, confirmationLink);

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation($"Confirmation email sent to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send confirmation email to {email}");
            throw; // Re-throw for calling code to handle
        }
    }

    private string GetConfirmationEmailHtml(string name, string link)
    {
        return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h1 style='color: #4a6baf;'>Welcome to Connectify, {name}!</h1>
                <p>Please confirm your email:</p>
                <p style='text-align: center; margin: 30px 0;'>
                    <a href='{link}' style='background-color: #4a6baf; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>
                        Confirm Email
                    </a>
                </p>
                <p>If you didn't request this, please ignore this email.</p>
            </div>";
    }

    private string GetConfirmationEmailText(string name, string link)
    {
        return $@"
            Welcome to Connectify, {name}!

            Please confirm your email by visiting this link:
            {link}

            If you didn't request this, please ignore this email.";
    }
}