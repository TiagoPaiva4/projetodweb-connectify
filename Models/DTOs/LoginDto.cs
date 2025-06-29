using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO (Data Transfer Object) para o processo de login padrão.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// O email do utilizador utilizado para a autenticação.
    /// </summary>
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
    public string Email { get; set; }

    /// <summary>
    /// A palavra-passe do utilizador para a autenticação.
    /// </summary>
    [Required(ErrorMessage = "A password é obrigatória.")]
    public string Password { get; set; }
}