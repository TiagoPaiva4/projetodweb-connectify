namespace projetodweb_connectify.Services.Email;

public class EmailSettings
{
    public string Smtp = "EmailSettings";
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
}