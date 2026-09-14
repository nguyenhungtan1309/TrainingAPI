using Microsoft.EntityFrameworkCore;

namespace TrainingAPI.Models
{
    public class CompanyContext : DbContext
    {
        public CompanyContext(DbContextOptions<CompanyContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<ConversationItem> ConversationItems { get; set; }

        public DbSet<ChatHistoryItem> ChatHistoryItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConversationItem>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_ConversationList");
            });

            modelBuilder.Entity<ChatHistoryItem>(entity =>
            {
                entity.HasNoKey();
            });
        }
    }
}
