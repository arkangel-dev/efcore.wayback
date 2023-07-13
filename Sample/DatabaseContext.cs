using Castle.Core.Internal;
using Sample.DbEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WaybackMachine;
using WaybackMachine.Entities;

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

            modelBuilder.Entity<AuditRecord>()
                .Property(s => s.ChangeType)
                .HasConversion(
                    x => (int)x,
                    x => (AuditEntryType)x
                )
                .IsUnicode(false);

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
        public DbSet<AuditRecord> AuditEntries { get; set; }
        public DbSet<AuditTransactionRecord> AuditTransactions { get; set; }
        public DbSet<AuditTable> AuditTables { get; set; }
        public DbSet<AuditProperty> AuditProperties { get; set; }


        public WaybackConfig WaybackConfiguration { get; set; } = new WaybackConfig();
        public List<AuditTable> _tempAuditTables { get; set; } = new List<AuditTable>();
        public List<AuditProperty> _tempAuditProperties { get; set; } = new List<AuditProperty>();
    }
}
