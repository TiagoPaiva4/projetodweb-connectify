using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO para o processo de login padrão.
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "A password é obrigatória.")]
    public string Password { get; set; }
}