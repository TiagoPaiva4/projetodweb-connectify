using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Data;
/// <summary>
/// Esta classe representa a Base de Dados associada ao projeto
/// Se houver mais bases de dados, irão haver tantas classes
/// deste tipo, quantas as necessárias
/// 
/// esta classe é equivalente a CREATE SCHEMA no SQL
/// </summary>
public class ApplicationDbContext : IdentityDbContext
{
    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="options"></param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Especificar as tabelas associadas à BD
    /// <summary>
    /// tabela Users na BD
    /// </summary>
    public DbSet<Users> Users { get; set; }

    /// <summary>
    /// tabela Profiles na BD
    /// </summary>
    public DbSet<Profile> Profiles { get; set; }

    /// <summary>
    /// tabela Topics na BD
    /// </summary>
    public DbSet<Topic> Topics { get; set; }

    /// <summary>
    /// tabela TopicPosts na BD
    /// </summary>
    public DbSet<TopicPost> TopicPosts { get; set; }

    /// <summary>
    /// tabela TopicComments na BD
    /// </summary>
    public DbSet<TopicComment> TopicComments { get; set; }

    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageRecipient> MessageRecipients { get; set; }


    /// <summary>
    /// tabela Friendships na BD
    /// </summary>
    public DbSet<Friendship> Friendships { get; set; }

    /// <summary>
    /// tabela DigitalLibrary na BD
    /// </summary>
    public DbSet<DigitalLibrary> DigitalLibrary { get; set; }

    /// <summary>
    /// Método para configurar as relações entre as entidades.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Chama a configuração base para IdentityDbContext

        // Configurar o relacionamento entre User e Friendship para User1Id
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.User1)
            .WithMany(u => u.FriendshipsInitiated)
            .HasForeignKey(f => f.User1Id)
            .OnDelete(DeleteBehavior.Restrict); // Impede que as amizades sejam excluídas se o User1 for excluído

        // Configurar o relacionamento entre User e Friendship para User2Id
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.User2)
            .WithMany(u => u.FriendshipsReceived)
            .HasForeignKey(f => f.User2Id)
            .OnDelete(DeleteBehavior.Restrict); // Impede que as amizades sejam excluídas se o User2 for excluído

        // Definir chave primária composta para MessageRecipient
        modelBuilder.Entity<MessageRecipient>()
            .HasKey(mr => new { mr.MessageId, mr.RecipientId });
    }
}


