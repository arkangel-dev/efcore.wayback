using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine.Entities;

namespace WaybackMachine {
    public class WaybackDbContext : DbContext {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            var _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            if (!optionsBuilder.IsConfigured) {
                optionsBuilder.UseSqlServer(_config.GetConnectionString("WaybackTracking"))
                    .UseLazyLoadingProxies()
                    .EnableSensitiveDataLogging();
            }
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<AuditRecord>()
                .Property(s => s.ChangeType)
                .HasConversion(
                    x => (int)x,
                    x => (AuditEntryType)x
                )
                .IsUnicode(false);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<AuditRecord> AuditEntries { get; set; }
        public DbSet<AuditTransactionRecord> AuditTransactions { get; set; }
        public DbSet<AuditTable> AuditTables { get; set; }
        public DbSet<AuditProperty> AuditProperties { get; set; }

        public List<AuditTable> _tempAuditTables { get; set; } = new List<AuditTable>();
        public List<AuditProperty> _tempAuditProperties { get; set; } = new List<AuditProperty>();

    }
}
