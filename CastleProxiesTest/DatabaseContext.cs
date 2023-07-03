using Castle.Core.Internal;
using CastleProxiesTest.DbEntities;
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

namespace CastleProxiesTest {
    public class DatabaseContext : DbContext {
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<DbEntities.Message>()
                .HasOne(s => s.Sender)
                .WithMany(s => s.Sent)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DbEntities.Message>()
                .HasOne(s => s.Recipient)
                .WithMany(s => s.Inbox)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DbEntities.AuditEntry>()
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

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            if (!optionsBuilder.IsConfigured) {
                optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=ProxyTestDb;Integrated Security=true;encrypt=false;")
                    .UseLazyLoadingProxies()
                    .EnableSensitiveDataLogging();
            }
            base.OnConfiguring(optionsBuilder);
        }

        public override int SaveChanges() {

            string TransactionID = Guid.NewGuid().ToString();
            var AddedEntities = new List<EntityEntry>();
            var changes = ChangeTracker.Entries().ToList();
            var addedEntities = new List<EntityEntry>();


            foreach (var entry in changes) {
                var entityPrimaryKeyProperty = entry.Entity.GetType()
                            .GetProperties()
                            .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));
                var entityPrimaryKey = (int)(entityPrimaryKeyProperty.GetValue(entry.Entity) ?? -1);
                var IsJunction = entry.Metadata.GetForeignKeys().Count() == 2 && entry.Members.Count() == 5;



                if (entry.State == EntityState.Added) {
                    addedEntities.Add(entry);
                    continue;
                }
                if (!IsJunction) {
                    foreach (var property in entry.Members) {

                        //if (property is CollectionEntry) {
                        //    var collectionCast = (CollectionEntry)property;
                        //    collectionCast.Metadata.
                        //    continue;
                        //}

                        if (property is not PropertyEntry) continue;
                        var casted = (PropertyEntry)property;

                        var updatedCollectionFields = casted.Metadata
                            .GetContainingForeignKeys()
                            .Select(s => s.GetNavigation(false));


                        foreach (var field in updatedCollectionFields) {
                            if (field == null) continue;
                            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted) {
                                var type = field.DeclaringEntityType.ClrType ?? throw new Exception();
                                var newID = (int)(casted.CurrentValue ?? casted.OriginalValue ?? -1);
                                var oldID = (int)(casted.OriginalValue ?? -1);

                                if (!(entry.State != EntityState.Deleted || newID != oldID)) {
                                    AuditEntries.Add(new AuditEntry() {
                                        PropertyName = field.Name,
                                        EntityID = oldID,
                                        TableName = GetTableNameFromType(field.DeclaringEntityType.ClrType) ?? "unknown",
                                        NewValue = entityPrimaryKey.ToString(),
                                        ChangeDate = DateTime.Now,
                                        TransactionID = TransactionID,
                                        ChangeType = AuditEntryType.CollectionAdd
                                    });
                                }

                                AuditEntries.Add(new AuditEntry() {
                                    PropertyName = field.Name,
                                    EntityID = newID,
                                    TableName = GetTableNameFromType(field.DeclaringEntityType.ClrType) ?? "unknown",
                                    NewValue = entityPrimaryKey.ToString(),
                                    ChangeDate = DateTime.Now,
                                    TransactionID = TransactionID,
                                    ChangeType = AuditEntryType.CollectionRemove
                                });
                            }

                        }
                    }
                } else {
                    if (entry.State != EntityState.Deleted) continue;
                    var fks = entry.Metadata.GetForeignKeys().ToList();
                    var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                    var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                    foreach (var prop in fks[0].GetReferencingSkipNavigations()) {
                        AuditEntries.Add(new AuditEntry() {
                            PropertyName = prop.Name,
                            EntityID = index_a,
                            TableName = GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_b.ToString(),
                            ChangeDate = DateTime.Now,
                            TransactionID = TransactionID,
                            ChangeType = AuditEntryType.CollectionRemove
                        });
                    }

                    foreach (var prop in fks[1].GetReferencingSkipNavigations()) {
                        AuditEntries.Add(new AuditEntry() {
                            PropertyName = prop.Name,
                            EntityID = index_b,
                            TableName = GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_a.ToString(),
                            ChangeDate = DateTime.Now,
                            TransactionID = TransactionID,
                            ChangeType = AuditEntryType.CollectionRemove
                        });
                    }
                }
                if (entry.Entity is AuditEntry || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;
                var EntryTypeName = entry.Entity.GetType().ToString();
                var EntryType = entry.Entity.GetType();





                if (entry.State == EntityState.Modified) {
                    var ChangeCount = 0;

                    foreach (var property in entry.Properties) {
                        if (property == null) continue;
                        if (!property.IsModified) continue;
                        var propertyName = property.Metadata.Name;
                        if (property.Metadata.IsForeignKey()) {
                            propertyName = propertyName.Substring(0, propertyName.Length - 2);
                        }



                        var CurrentValue = property.CurrentValue;
                        if (!property.Metadata.IsShadowProperty()) {
                            var propInfo = EntryType.GetProperty(propertyName);
                        }

                        var IDProperty = entry.Entity.GetType()
                            .GetProperties()
                            .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));


                        //var entTypes = Model.GetEntityTypes().Select(s => s.ClrType);
                        //var entryType = entry.GetType();
                        ChangeCount++;
                        AuditEntries.Add(new AuditEntry() {
                            PropertyName = property.Metadata.Name,
                            EntityID = (int)(IDProperty.GetValue(entry.Entity) ?? -1),
                            TableName = GetTableNameFromType(EntryType) ?? "unknown",
                            OldValue = property.OriginalValue?.ToString(),
                            NewValue = property.CurrentValue?.ToString(),
                            ChangeDate = DateTime.Now,
                            TransactionID = TransactionID
                        }); ;
                    }
                    continue;
                }
            }

            base.SaveChanges();

            foreach (var entry in addedEntities) {
                var entityPrimaryKeyProperty = entry.Entity.GetType()
                            .GetProperties()
                            .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));


                var id = (int)(entityPrimaryKeyProperty.GetValue(entry.Entity) ?? -1);
                var IsJunction = entry.Metadata.GetForeignKeys().Count() == 2 && entry.Members.Count() == 5;

                if (!IsJunction) {

                    foreach (var property in entry.Members) {
                        if (property is not PropertyEntry) continue;
                        var casted = (PropertyEntry)property;

                        var updatedCollectionFields = casted.Metadata
                            .GetContainingForeignKeys()
                            .Select(s => s.GetNavigation(false))
                            .ToList();




                        foreach (var field in updatedCollectionFields) {
                            if (field == null) continue;
                            var type = field.DeclaringEntityType.ClrType ?? throw new Exception();
                            AuditEntries.Add(new AuditEntry() {
                                PropertyName = field.Name,
                                EntityID = (int)(property.CurrentValue ?? -1),
                                TableName = GetTableNameFromType(field.DeclaringEntityType.ClrType) ?? "unknown",
                                NewValue = id.ToString(),
                                ChangeDate = DateTime.Now,
                                TransactionID = TransactionID,
                                ChangeType = AuditEntryType.CollectionAdd
                            });
                        }
                    }
                } else {
                    var fks = entry.Metadata.GetForeignKeys().ToList();
                    var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                    var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                    
                    foreach (var prop in fks[0].GetReferencingSkipNavigations()) {
                        AuditEntries.Add(new AuditEntry() {
                            PropertyName = prop.Name,
                            EntityID = index_a,
                            TableName = GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_b.ToString(),
                            ChangeDate = DateTime.Now,
                            TransactionID = TransactionID,
                            ChangeType = AuditEntryType.CollectionAdd
                        });
                    }

                    foreach (var prop in fks[1].GetReferencingSkipNavigations()) {
                        AuditEntries.Add(new AuditEntry() {
                            PropertyName = prop.Name,
                            EntityID = index_b,
                            TableName = GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_a.ToString(),
                            ChangeDate = DateTime.Now,
                            TransactionID = TransactionID,
                            ChangeType = AuditEntryType.CollectionAdd
                        });
                    }



                }
            }

            return base.SaveChanges();
        }


        public DbSet<DbEntities.Message> Messages { get; set; }
        public DbSet<DbEntities.User> Users { get; set; }
        public DbSet<DbEntities.Interest> Interests { get; set; }
        public DbSet<DbEntities.JUser_Interest> Junction_Interests_Users { get; set; }
        public DbSet<DbEntities.AuditEntry> AuditEntries { get; set; }

        public string? GetTableNameFromType(Type t) =>
            Model.GetEntityTypes()
            .FirstOrDefault(s => s.ClrType == t)?
            .GetAnnotation("Relational:TableName")
            .Value?.ToString();

        public Type? GetTypeFromTableName(string t) =>
            Model.GetEntityTypes()
            .FirstOrDefault(s => s.GetAnnotation("Relational:TableName").Value?.ToString() == t)
            ?.ClrType;

        public virtual int GetKey<T>(T entity) {
            var keyName = Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties
                .Select(x => x.Name).Single();
            return (int)entity.GetType().GetProperty(keyName).GetValue(entity, null);
        }

        public dynamic FindEntity(string table, object Id) {
            PropertyInfo prop = this.GetType().GetProperty(table, BindingFlags.Instance | BindingFlags.Public);
            dynamic dbSet = prop.GetValue(this, null);
            return dbSet.Find(Id);
        }
    }
}
