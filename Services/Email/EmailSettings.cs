namespace projetodweb_connectify.Services.Email
{
    /// <summary>
    /// Representa as configura��es necess�rias para o servi�o de envio de emails.
    /// Esta classe � geralmente preenchida a partir de um ficheiro de configura��o (como appsettings.json).
    /// </summary>
    public class EmailSettings
    {
        /// <summary>
        /// O endere�o do servidor SMTP a ser utilizado para o envio de emails (ex: "smtp.gmail.com").
        /// </summary>
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
        /// A porta do servidor SMTP (ex: 587 para TLS).
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// O nome de utilizador para autentica��o no servidor SMTP.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// A palavra-passe (ou 'app password') para autentica��o no servidor SMTP.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// O nome que ser� exibido como remetente nos emails enviados (ex: "Equipa Connectify").
        /// </summary>
        public string FromName { get; set; } = string.Empty;

        /// <summary>
        /// O endere�o de email que ser� utilizado como remetente (ex: "nao-responder@connectify.pt").
        /// </summary>
        public string FromAddress { get; set; } = string.Empty;
    }
}