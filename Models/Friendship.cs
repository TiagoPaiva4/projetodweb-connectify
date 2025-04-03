using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace projetodweb_connectify.Models
{
    /// <summary>
    /// representa a relação de amizade entre dois utilizadores na rede social.
    /// contém os identificadores dos utilizadores e o estado da amizade.
    /// </summary>
    public class Friendship
    {
        /// <summary>
        // chaves estrangeiras representando os dois utilizadores na amizade
        /// </summary>
        [ForeignKey("User1")]
        public int User1Id { get; set; }

        [ForeignKey("User2")]
        public int User2Id { get; set; }

        /// <summary>
        // propriedades de navegação para os utilizadores
        /// </summary>
        public Users User1 { get; set; }
        public Users User2 { get; set; }

        /// <summary>
        // data de criação da amizade
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        // chave primária composta (User1Id, User2Id)
        /// </summary>
        [Key]
        public int Id
        {
            get { return 0; }
            set { }
        }

        /// <summary>
        // garante que User1Id e User2Id não são iguais
        /// </summary>
        public static bool IsValidFriendship(int user1Id, int user2Id)
        {
            return user1Id != user2Id;
        }
    }
}
