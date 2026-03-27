using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;

namespace DataAccess
{
    public class MyDbContext : DbContext
    {
        public MyDbContext() { }
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                IConfigurationRoot configurationRoot = builder.Build();
                optionsBuilder.UseSqlServer(configurationRoot.GetConnectionString("MyStoreDB"));
            }
        }
        public DbSet<User> Users { get; set; } 
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationMember> ConversationMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<MessageHidden> MessageHiddens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.User)
                .WithMany(u => u.MessageReactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageReaction>()
                .HasIndex(r => new { r.MessageId, r.UserId, r.ReactionType })
                .IsUnique();

            modelBuilder.Entity<MessageHidden>()
                .HasOne(h => h.Message)
                .WithMany(m => m.HiddenByUsers)
                .HasForeignKey(h => h.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageHidden>()
                .HasOne(h => h.User)
                .WithMany(u => u.MessageHiddens)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MessageHidden>()
                .HasIndex(h => new { h.MessageId, h.UserId })
                .IsUnique();
        }
    }
}
