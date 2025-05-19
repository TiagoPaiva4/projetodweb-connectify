using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa o perfil de um utilizador na rede social.
    /// contém informações como tipo de perfil, biografia, foto de perfil e data de criação.
    /// </summary>  
    public class Profile
    {
        /// <summary>
        /// identificador único do perfil.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// identificador do utilizador associado ao perfil.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// referência para o utilizador dono do perfil.
        /// </summary>
        [ValidateNever]
        [ForeignKey("UserId")]
        public Users User { get; set; } = null!;

        /// <summary>
        /// nome do utilizador associado ao perfil.
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// tipo de perfil: pessoal ou público
        /// </summary>
        [MaxLength(50)]
        public string Type { get; set; } = "Pessoal";

        /// <summary>
        /// biografia do utilizador, onde pode adicionar uma breve descrição sobre si.
        /// </summary>
        [MaxLength(1000)]
        public string? Bio { get; set; }

        /// <summary>
        /// url da foto de perfil do utilizador.
        /// </summary>
        public string ProfilePicture { get; set; } = "/images/defaultuser.png";

        /// <summary>
        /// data e hora de criação do perfil. 
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// lista de tópicos criados por este perfil. 
        /// não é mapeada para a base de dados e serve apenas para transporte de dados para a view.
        /// </summary>
        [NotMapped]
        public List<Topic> CreatedTopics { get; set; } = new List<Topic>();

        /// <summary>
        /// tópico pessoal associado ao perfil.
        /// não é mapeado para a base de dados, apenas usado na lógica da aplicação.
        /// </summary>
        [NotMapped] 
        public Topic? PersonalTopic { get; set; } 

        /// <summary>
        /// lista de publicações feitas no tópico pessoal.
        /// não é mapeada para a base de dados, usada apenas para carregar dados na aplicação.
        /// </summary>
        [NotMapped] 
        public ICollection<TopicPost> PersonalTopicPosts { get; set; } = new List<TopicPost>();

        /// <summary>
        /// Propriedade de navegação de coleção para os tópicos salvos por este perfil.
        /// </summary>
        public virtual ICollection<SavedTopic> SavedTopics { get; set; } = new List<SavedTopic>();

        /// <summary>
        /// Lista de tópicos que este perfil salvou.
        /// Preenchido no controlador, não mapeado para o banco de dados.
        /// </summary>
        [NotMapped]
        public List<Topic> DisplaySavedTopics { get; set; } = new List<Topic>();

        /// <summary>
        /// Lista de amigos associados a este perfil (utilizador).
        /// Não é mapeada para a base de dados, populada no controller para a view MyProfile.
        /// </summary>
        [NotMapped]
        public List<Users> Friends { get; set; } = new List<Users>();
    }
}
