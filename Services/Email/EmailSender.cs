using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace projetodweb_connectify.Services.Email;

/// <summary>
/// Servi�o respons�vel pelo envio de emails utilizando as configura��es da aplica��o.
/// </summary>
public class EmailSender : ICustomEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    // Construtor que recebe as depend�ncias atrav�s de inje��o.
    public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Envia um email de confirma��o de registo para um novo utilizador.
    /// </summary>
    /// <param name="email">O endere�o de email do destinat�rio.</param>
    /// <param name="name">O nome do destinat�rio a ser usado no corpo do email.</param>
    /// <param name="confirmationLink">A liga��o para a confirma��o da conta.</param>
    public async Task SendEmailAsync(string email, string name, string confirmationLink)
    {
        Console.WriteLine("SendRegistrationConfirmationAsync called");
        try
        {
            // Cria a estrutura base da mensagem de email.
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(name, email));
            message.Subject = "Confirm Your Connectify Registration";

            // Utiliza um BodyBuilder para criar facilmente um email com vers�es HTML e texto simples.
            var builder = new BodyBuilder();

            // Define a vers�o HTML do email.
            builder.HtmlBody = GetConfirmationEmailHtml(name, confirmationLink);

            // Define a vers�o em texto simples, para clientes de email que n�o suportam HTML.
            builder.TextBody = GetConfirmationEmailText(name, confirmationLink);

            message.Body = builder.ToMessageBody();

            // Configura e utiliza o cliente SMTP para enviar o email.
            using var client = new SmtpClient();

            await client.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Confirmation email sent to {email}");
        }
        catch (Exception ex)
        {
            // Em caso de erro, regista a exce��o para diagn�stico.
            _logger.LogError(ex, $"Failed to send confirmation email to {email}");
            throw; // Relan�a a exce��o para que o c�digo que chamou este m�todo possa lidar com o erro.
        }
    }

    /// <summary>
    /// Gera o conte�do HTML para o email de confirma��o.
    /// </summary>
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

    /// <summary>
    /// Gera o conte�do em texto simples para o email de confirma��o.
    /// </summary>
    private string GetConfirmationEmailText(string name, string link)
    {
        return $@"
            Welcome to Connectify, {name}!

            Please confirm your email by visiting this link:
            {link}

            If you didn't request this, please ignore this email.";
    }
}