using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Models;
using System;

namespace projetodweb_connectify.Data
{
    /// <summary>
    /// Representa o contexto da base de dados da aplicação, funcionando como a ponte
    /// entre os modelos C# (entidades) e a base de dados relacional.
    /// Herda de IdentityDbContext para incluir as tabelas do sistema de autenticação ASP.NET Core Identity.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // --- Definição das Tabelas ---

        /// <summary>
        /// Tabela Users na BD.
        /// </summary>
        public DbSet<Users> Users { get; set; }

        /// <summary>
        /// Tabela Profiles na BD.
        /// </summary>
        public DbSet<Profile> Profiles { get; set; }

        /// <summary>
        /// Tabela Categories na BD.
        /// </summary>
        public DbSet<Category> Categories { get; set; }

        /// <summary>
        /// Tabela Topics na BD.
        /// </summary>
        public DbSet<Topic> Topics { get; set; }

        /// <summary>
        /// Tabela TopicPosts na BD.
        /// </summary>
        public DbSet<TopicPost> TopicPosts { get; set; }

        /// <summary>
        /// Tabela TopicComments na BD.
        /// </summary>
        public DbSet<TopicComment> TopicComments { get; set; }

        /// <summary>
        /// Tabela TopicPostLikes na BD.
        /// </summary>
        public DbSet<TopicPostLike> TopicPostLikes { get; set; }

        /// <summary>
        /// Tabela TopicCommentLikes na BD.
        /// </summary>
        public DbSet<TopicCommentLike> TopicCommentLikes { get; set; }

        /// <summary>
        /// Tabela SavedTopics na BD.
        /// </summary>
        public DbSet<SavedTopic> SavedTopics { get; set; }

        /// <summary>
        /// Tabela Conversations na BD.
        /// </summary>
        public DbSet<Conversation> Conversations { get; set; }

        /// <summary>
        /// Tabela Messages na BD.
        /// </summary>
        public DbSet<Message> Messages { get; set; }

        /// <summary>
        /// Tabela Friendships na BD.
        /// </summary>
        public DbSet<Friendship> Friendships { get; set; }

        /// <summary>
        /// Tabela Events na BD.
        /// </summary>
        public DbSet<Event> Events { get; set; }

        /// <summary>
        /// Tabela UserEventAttendances na BD.
        /// </summary>
        public DbSet<UserEventAttendance> UserEventAttendances { get; set; }

        /// <summary>
        /// Configura o modelo de dados, definindo chaves primárias, chaves estrangeiras,
        /// relações, índices e dados iniciais (seeding).
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // É crucial chamar a implementação base primeiro para que o Identity seja configurado corretamente.
            base.OnModelCreating(modelBuilder);

            // --- SEEDING (Dados Iniciais) ---
            SeedInitialData(modelBuilder);

            // --- CONFIGURAÇÃO DAS RELAÇÕES ENTRE ENTIDADES ---

            // Relação 1-para-1 entre Users e Profile.
            modelBuilder.Entity<Profile>()
                .HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Se um utilizador for apagado, o seu perfil também deve ser.

            // Relação 1-para-Muitos entre Category e Topic.
            modelBuilder.Entity<Topic>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Topics)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Impede que uma categoria seja apagada se tiver tópicos associados.

            // Relação 1-para-Muitos entre Profile (Criador) e Topic.
            modelBuilder.Entity<Topic>()
                .HasOne(t => t.Creator)
                .WithMany() // Um perfil pode criar muitos tópicos. Não é necessária uma lista de navegação inversa.
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Impede que um perfil seja apagado se for criador de tópicos.

            // Relação 1-para-Muitos entre Topic e TopicPost.
            modelBuilder.Entity<TopicPost>()
               .HasOne(tp => tp.Topic)
               .WithMany(t => t.Posts)
               .HasForeignKey(tp => tp.TopicId)
               .OnDelete(DeleteBehavior.Cascade); // Se um tópico for apagado, as suas publicações também são.

            // Relação 1-para-Muitos entre Profile (Autor) e TopicPost.
            modelBuilder.Entity<TopicPost>()
               .HasOne(tp => tp.Profile)
               .WithMany()
               .HasForeignKey(tp => tp.ProfileId)
               .OnDelete(DeleteBehavior.Restrict);

            // Tabela de Junção para Tópicos Guardados (SavedTopic).
            modelBuilder.Entity<SavedTopic>(entity =>
            {
                entity.HasKey(st => new { st.ProfileId, st.TopicId }); // Chave primária composta.

                entity.HasOne(st => st.SaverProfile)
                      .WithMany(p => p.SavedTopics)
                      .HasForeignKey(st => st.ProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(st => st.Topic)
                      .WithMany(t => t.Savers)
                      .HasForeignKey(st => st.TopicId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Tabela de Junção para Amizades (Friendship).
            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.HasKey(f => new { f.User1Id, f.User2Id }); // Chave primária composta.

                entity.HasOne(f => f.User1)
                      .WithMany(u => u.FriendshipsInitiated)
                      .HasForeignKey(f => f.User1Id)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.User2)
                      .WithMany(u => u.FriendshipsReceived)
                      .HasForeignKey(f => f.User2Id)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Tabela de Junção para Gostos em Publicações (TopicPostLike).
            modelBuilder.Entity<TopicPostLike>(entity =>
            {
                entity.HasKey(tpl => new { tpl.TopicPostId, tpl.ProfileId }); // Chave primária composta.

                entity.HasOne(tpl => tpl.TopicPost)
                      .WithMany(tp => tp.Likes)
                      .HasForeignKey(tpl => tpl.TopicPostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tpl => tpl.Profile)
                      .WithMany()
                      .HasForeignKey(tpl => tpl.ProfileId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Tabela de Junção para Gostos em Comentários (TopicCommentLike).
            modelBuilder.Entity<TopicCommentLike>(entity =>
            {
                entity.HasKey(tcl => new { tcl.TopicCommentId, tcl.ProfileId }); // Chave primária composta.

                entity.HasOne(tcl => tcl.TopicComment)
                      .WithMany(tc => tc.Likes)
                      .HasForeignKey(tcl => tcl.TopicCommentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tcl => tcl.Profile)
                      .WithMany()
                      .HasForeignKey(tcl => tcl.ProfileId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Relação para Conversas (Conversation).
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasIndex(c => new { c.Participant1Id, c.Participant2Id }).IsUnique();

                entity.HasOne(c => c.Participant1).WithMany().HasForeignKey(c => c.Participant1Id).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Participant2).WithMany().HasForeignKey(c => c.Participant2Id).OnDelete(DeleteBehavior.Restrict);
            });

            // Relação para Mensagens (Message).
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.Recipient).WithMany().HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.Restrict);
            });

            // Relação para Eventos e Criador.
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tabela de Junção para Participação em Eventos (UserEventAttendance).
            modelBuilder.Entity<UserEventAttendance>(entity =>
            {
                entity.HasKey(uea => new { uea.UserId, uea.EventId }); // Chave primária composta.

                entity.HasOne(uea => uea.User)
                      .WithMany(u => u.EventAttendances)
                      .HasForeignKey(uea => uea.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uea => uea.Event)
                      .WithMany(e => e.Attendees)
                      .HasForeignKey(uea => uea.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuração do valor padrão para a data de criação do utilizador.
            modelBuilder.Entity<Users>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()"); // Para SQL Server. Use "CURRENT_TIMESTAMP" para SQLite/PostgreSQL.
        }

        /// <summary>
        /// Método auxiliar para popular a base de dados com dados iniciais.
        /// </summary>
        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Criar a Role "admin".
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "a", Name = "admin", NormalizedName = "ADMIN", ConcurrencyStamp = Guid.NewGuid().ToString() }
            );

            // Criar um utilizador administrador.
            var hasher = new PasswordHasher<IdentityUser>();
            modelBuilder.Entity<IdentityUser>().HasData(
                new IdentityUser
                {
                    Id = "admin",
                    UserName = "admin@mail.pt",
                    NormalizedUserName = "ADMIN@MAIL.PT",
                    Email = "admin@mail.pt",
                    NormalizedEmail = "ADMIN@MAIL.PT",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "Aa0_aa"), 
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );

            // Associar o utilizador administrador à role "admin".
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = "admin", RoleId = "a" }
            );
        }
    }
}