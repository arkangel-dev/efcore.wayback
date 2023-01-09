using Microsoft.EntityFrameworkCore;
using WaybackMachine;

namespace Sample {
    public class DatabaseContext : DbContext, IWaybackContext  {


        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<DbEntities.Message>()
                .HasOne(s => s.Sender)
                .WithMany(s => s.Sent)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DbEntities.Message>()
                .HasOne(s => s.Recipient)
                .WithMany(s => s.Inbox)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DbEntities.Interest>()
                .HasMany(s => s.Users)
                .WithMany(s => s.Interests)
                .UsingEntity<DbEntities.JUser_Interest>(
                    x => x
                    .HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserID),

                    x => x
                    .HasOne(s => s.Interest)
                    .WithMany()
                    .HasForeignKey(s => s.InterestID)
                );
            this.ConfigureWaybackModel(modelBuilder);

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            if (!optionsBuilder.IsConfigured) {
                optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=ProxyTestDb;Integrated Security=true;encrypt=false;")
                    .UseLazyLoadingProxies()
                    .EnableSensitiveDataLogging();
            }
            base.OnConfiguring(optionsBuilder);
        }

        public override int SaveChanges() => this.SaveChangesWithTracking();
        public int BaseSaveChanges() => base.SaveChanges();

        public DbSet<DbEntities.Message> Messages { get; set; }
        public DbSet<DbEntities.User> Users { get; set; }
        public DbSet<DbEntities.Interest> Interests { get; set; }
        public DbSet<DbEntities.JUser_Interest> Junction_Interests_Users { get; set; }
        public DbContext InternalDbContext => this;


        public WaybackConfig WaybackConfiguration { get; set; } = new WaybackConfig();
    }
}
