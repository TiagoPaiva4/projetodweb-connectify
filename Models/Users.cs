using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa um utilizador da rede social.
    /// contém informações essenciais como nome de utilizador, email, password e relações com outras entidades.
    /// </summary>
    public class Users
    {
        /// <summary>
        /// identificador único do utilizador.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// nome de utilizador escolhido pelo utilizador.
        /// este atributo servirá para fazer a 'ponte'
        /// entre a tabela dos Utilizadores e a 
        /// tabela da Autenticação da Microsoft Identity
        /// </summary>
        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// endereço de email do utilizador. 
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// número de telemóvel do utilizador
        /// </summary>
        [Display(Name = "Phone")]
        [StringLength(18)]
        [RegularExpression("(([+]|00)[0-9]{1,5})?[1-9][0-9]{5,10}", ErrorMessage = "Escreva um nº de telefone. Pode adicionar indicativo do país.")]
        public string? Phone { get; set; }
        /*  9[1236][0-9]{7}  --> nºs telemóvel nacional
         *  (([+]|00)[0-9]{1,5})?[1-9][0-9]{5,10}  -->  nºs telefone internacionais
        */

        /// <summary>
        /// hash da password do utilizador para garantir segurança.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// data e hora de criação do utilizador. 
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        /// <summary>
        /// lista de perfis associados ao utilizador.
        /// </summary>
        public ICollection<Profile> Profiles { get; set; } = new List<Profile>();

        /// <summary>
        // propriedades de navegação para as amizades iniciadas (User1Id)
        /// </summary>
        public ICollection<Friendship> FriendshipsInitiated { get; set; } = new List<Friendship>();

        /// <summary>
        // propriedades de navegação para as amizades recebidas (User2Id)
        /// </summary>
        public ICollection<Friendship> FriendshipsReceived { get; set; } = new List<Friendship>();
    }
}
