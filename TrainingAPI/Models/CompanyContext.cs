using Microsoft.EntityFrameworkCore;
using TrainingAPI.Models.Chat;
using TrainingAPI.Models.Interactions;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Models
{
    public class CompanyContext : DbContext
    {
        public CompanyContext(DbContextOptions<CompanyContext> options) : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; } = null!;
        public DbSet<UserDevice> UserDevices { get; set; } = null!;
        public DbSet<BlockedUser> BlockedUsers { get; set; } = null!;
        public DbSet<ChatThread> ChatThreads { get; set; } = null!;
        public DbSet<ThreadParticipant> ThreadParticipants { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<MessageAttachment> MessageAttachments { get; set; } = null!;
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;
        public DbSet<PinnedMessage> PinnedMessages { get; set; } = null!;
        public DbSet<DeletedThreadMessage> DeletedThreadMessages { get; set; } = null!;
        public DbSet<HiddenThread> HiddenThreads { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BlockedUser>()
                .HasKey(b => new { b.UserId, b.BlockedUserId });

            modelBuilder.Entity<BlockedUser>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlockedUser>()
                .HasOne(b => b.BlockedAppUser)
                .WithMany()
                .HasForeignKey(b => b.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThreadParticipant>()
                .HasKey(tp => new { tp.ThreadId, tp.UserId });

            modelBuilder.Entity<MessageReaction>()
                .HasKey(r => new { r.MessageId, r.UserId, r.ReactionType });

            modelBuilder.Entity<PinnedMessage>()
                .HasKey(p => new { p.ThreadId, p.MessageId });

            modelBuilder.Entity<DeletedThreadMessage>()
                .HasKey(d => new { d.UserId, d.MessageId });

            modelBuilder.Entity<HiddenThread>()
                .HasKey(h => new { h.UserId, h.ThreadId });
        }
    }
}