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

    /// <summary>
    /// tabela SavedTopic na BD
    /// </summary>
    public DbSet<SavedTopic> SavedTopics { get; set; }

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

        // Guarda automaticamente a data e hora em que o utilizador se registou 
        modelBuilder.Entity<Users>()
        .Property(u => u.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP"); // Para SQL Server ou SQLite

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Friendship>()
        .HasKey(f => new { f.User1Id, f.User2Id });

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.User1)
            .WithMany(u => u.FriendshipsInitiated)
            .HasForeignKey(f => f.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.User2)
            .WithMany(u => u.FriendshipsReceived)
            .HasForeignKey(f => f.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Profile>()
            .HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<Profile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict); // evitar que o User apague o perfil

        // --- Configure Composite Key for SavedTopic ---
        modelBuilder.Entity<SavedTopic>()
            .HasKey(st => new { st.ProfileId, st.TopicId }); 

        // Profile -> SavedTopic (One-to-Many)
        modelBuilder.Entity<SavedTopic>()
            .HasOne(st => st.SaverProfile)
            .WithMany(p => p.SavedTopics)
            .HasForeignKey(st => st.ProfileId)
            .OnDelete(DeleteBehavior.Cascade); // If profile deleted, remove their saved records

        // Topic -> SavedTopic (One-to-Many)
        modelBuilder.Entity<SavedTopic>()
            .HasOne(st => st.Topic) 
            .WithMany(t => t.Savers)
            .HasForeignKey(st => st.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Optional: Configure other relationships if needed ---
        modelBuilder.Entity<TopicPost>()
           .HasOne(tp => tp.Profile) // Assuming Profile is the navigation property name in TopicPost
           .WithMany() // Profile doesn't need a collection of all TopicPosts it ever made across all topics here.
           .HasForeignKey(tp => tp.ProfileId)
           .OnDelete(DeleteBehavior.Restrict); // Prevent profile deletion if they have posts? Or Cascade? Decide based on your rules. Often Restrict is safer here initially.

        modelBuilder.Entity<TopicPost>()
           .HasOne(tp => tp.Topic)
           .WithMany(t => t.Posts)
           .HasForeignKey(tp => tp.TopicId)
           .OnDelete(DeleteBehavior.Cascade); // If topic deleted, delete its posts

        modelBuilder.Entity<Topic>()
            .HasOne(t => t.Creator) // Assuming Creator is the navigation property name in Topic
            .WithMany() // Profile doesn't necessarily need a direct collection of Topics it created here if already handled elsewhere or not needed.
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict); // Prevent profile deletion if they created topics? Or Cascade? Often Restrict is safer.
    }


}


