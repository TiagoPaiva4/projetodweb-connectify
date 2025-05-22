using Microsoft.AspNetCore.Identity;
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
    /// tabela Category na BD
    /// </summary>
    public DbSet<Category> Categories { get; set; }

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

    /// <summary>
    /// tabela Conversation na BD
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; }

    /// <summary>
    /// tabela Message na BD
    /// </summary>
    public DbSet<Message> Messages { get; set; }


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
        // 'importa' todo o comportamento do método, 
        // aquando da sua definição na SuperClasse
        base.OnModelCreating(modelBuilder); // Chama a configuração base para IdentityDbContext

        // --- Identity Seeding ---
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "a", Name = "admin", NormalizedName = "ADMIN", ConcurrencyStamp = Guid.NewGuid().ToString() } // Added ConcurrencyStamp
        );

        // criar um utilizador para funcionar como ADMIN
        // função para codificar a password
        var hasher = new PasswordHasher<IdentityUser>();


        // criação do utilizador
        modelBuilder.Entity<IdentityUser>().HasData(
            new IdentityUser
            {
                Id = "admin", // This should be a GUID string if not customized
                UserName = "admin@mail.pt",
                NormalizedUserName = "ADMIN@MAIL.PT",
                Email = "admin@mail.pt",
                NormalizedEmail = "ADMIN@MAIL.PT",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, "Aa0_aa"),
                SecurityStamp = Guid.NewGuid().ToString(), // Ensure these are consistent if data is re-seeded or use fixed values
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        );


        // Associar este utilizador à role ADMIN
        modelBuilder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string> { UserId = "admin", RoleId = "a" });


        // Configure the relationship for FriendshipsInitiated
        // A User (User1) can initiate many friendships
        // A Friendship has one User1
        modelBuilder.Entity<Users>()
            .HasMany(u => u.FriendshipsInitiated)
            .WithOne(f => f.User1)
            .HasForeignKey(f => f.User1Id)
            .OnDelete(DeleteBehavior.Restrict); // Or .Cascade if you want deleting a user to delete their initiated friendships.
                                                // Restrict is safer to prevent accidental data loss.

        // Configure the relationship for FriendshipsReceived
        // A User (User2) can receive many friendships
        // A Friendship has one User2
        modelBuilder.Entity<Users>()
            .HasMany(u => u.FriendshipsReceived)
            .WithOne(f => f.User2)
            .HasForeignKey(f => f.User2Id)
            .OnDelete(DeleteBehavior.Restrict); // Or .Cascade.

        // Optional: Add a unique constraint to prevent duplicate pending requests
        // or duplicate accepted friendships between the same two users.
        // This ensures UserA can't send multiple requests to UserB while one is pending,
        // or that they can't be "friends" twice.
        // You might want to handle this in application logic for more complex scenarios
        // (e.g., if a rejected request can be re-sent later).
        // For now, a basic unique constraint on the pair:
        modelBuilder.Entity<Friendship>()
            .HasIndex(f => new { f.User1Id, f.User2Id })
            .IsUnique();
        // Note: This simple unique index means (User1Id=1, User2Id=2) is different from (User1Id=2, User2Id=1).
        // Your application logic will need to decide if User1Id is always the requester,
        // or if you need to check for existing friendships in both directions (e.g., User1Id=A, User2Id=B OR User1Id=B, User2Id=A).
        // For a request system (User1 sends to User2), this index is fine.

        // Configuração para Conversation
        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.User1)
            .WithMany() // Users não precisa de uma coleção de Conversations aqui
            .HasForeignKey(c => c.User1Id)
            .OnDelete(DeleteBehavior.Restrict); // Ou .Cascade se fizer sentido

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.User2)
            .WithMany()
            .HasForeignKey(c => c.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuração para Message
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany() // Users não precisa de coleção de SentMessages
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Recipient)
            .WithMany() // Users não precisa de coleção de ReceivedMessages
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Relação entre Topic e Category (EF Core deve inferir, mas pode ser explícito)
        modelBuilder.Entity<Topic>()
            .HasOne(t => t.Category)      // Um tópico tem uma categoria
            .WithMany(c => c.Topics)      // Uma categoria tem muitos tópicos
            .HasForeignKey(t => t.CategoryId) // A chave estrangeira em Topic é CategoryId
            .OnDelete(DeleteBehavior.SetNull); // Ou .SetNull, ou .Cascade dependendo da sua política.
                                                // Restrict: Impede a exclusão de uma categoria se houver tópicos nela.
                                                // SetNull: Define CategoryId como null nos tópicos se a categoria for excluída (torna CategoryId anulável no Topic).
                                                // Cascade: Exclui todos os tópicos da categoria se a categoria for excluída (geralmente não recomendado para categorias).


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


