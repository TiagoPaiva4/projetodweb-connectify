namespace projetodweb_connectify.Services.Email
{
    /// <summary>
    /// Representa as configurações necessárias para o serviço de envio de emails.
    /// Esta classe é geralmente preenchida a partir de um ficheiro de configuração (como appsettings.json).
    /// </summary>
    public class EmailSettings
    {
        /// <summary>
        /// O endereço do servidor SMTP a ser utilizado para o envio de emails (ex: "smtp.gmail.com").
        /// </summary>
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
        /// A porta do servidor SMTP (ex: 587 para TLS).
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// O nome de utilizador para autenticação no servidor SMTP.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// A palavra-passe (ou 'app password') para autenticação no servidor SMTP.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// O nome que será exibido como remetente nos emails enviados (ex: "Equipa Connectify").
        /// </summary>
        public string FromName { get; set; } = string.Empty;

        /// <summary>
        /// O endereço de email que será utilizado como remetente (ex: "nao-responder@connectify.pt").
        /// </summary>
        public string FromAddress { get; set; } = string.Empty;
    }
}