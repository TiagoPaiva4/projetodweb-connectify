using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace projetodweb_connectify.Services.Email;

/// <summary>
/// Serviço responsável pelo envio de emails utilizando as configurações da aplicação.
/// </summary>
public class EmailSender : ICustomEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    // Construtor que recebe as dependências através de injeção.
    public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Envia um email de confirmação de registo para um novo utilizador.
    /// </summary>
    /// <param name="email">O endereço de email do destinatário.</param>
    /// <param name="name">O nome do destinatário a ser usado no corpo do email.</param>
    /// <param name="confirmationLink">A ligação para a confirmação da conta.</param>
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

            // Utiliza um BodyBuilder para criar facilmente um email com versões HTML e texto simples.
            var builder = new BodyBuilder();

            // Define a versão HTML do email.
            builder.HtmlBody = GetConfirmationEmailHtml(name, confirmationLink);

            // Define a versão em texto simples, para clientes de email que não suportam HTML.
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
            // Em caso de erro, regista a exceção para diagnóstico.
            _logger.LogError(ex, $"Failed to send confirmation email to {email}");
            throw; // Relança a exceção para que o código que chamou este método possa lidar com o erro.
        }
    }

    /// <summary>
    /// Gera o conteúdo HTML para o email de confirmação.
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
    /// Gera o conteúdo em texto simples para o email de confirmação.
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