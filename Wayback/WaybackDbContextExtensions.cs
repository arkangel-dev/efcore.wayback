using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine;
using WaybackMachine.Entities;

namespace WaybackMachine {
    public static class WaybackDbContextExtensions {
        public static void ConfigureWaybackModel(this IWaybackContext context, ModelBuilder modelBuilder) {
            modelBuilder.Entity<AuditTransactionRecord>()
                .HasMany(s => s.Changes)
                .WithOne(s => s.ParentTransaction);
        }
        internal static string? GetTableNameFromType(this DbContext dbcontext, Type t) =>
            dbcontext.Model.GetEntityTypes()
                .FirstOrDefault(s => s.ClrType == t)?
                .GetAnnotation("Relational:TableName")
                .Value?.ToString();

        internal static Type? GetTypeFromTableName(this DbContext dbcontext, string t) =>
            dbcontext.Model.GetEntityTypes()
            .FirstOrDefault(s => s.GetAnnotation("Relational:TableName").Value?.ToString() == t)
            ?.ClrType;

        internal static int GetKey<T>(this DbContext dbcontext, T entity) {
            var keyName = dbcontext.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties
                .Select(x => x.Name).Single();
            return (int)entity.GetType().GetProperty(keyName).GetValue(entity, null);
        }

        internal static dynamic FindEntity(this DbContext dbcontext, string table, object Id) {
            PropertyInfo prop = dbcontext.GetType().GetProperty(table, BindingFlags.Instance | BindingFlags.Public);
            dynamic dbSet = prop.GetValue(dbcontext, null);
            return dbSet.Find(Id);
        }

        public static int SaveChangesWithTracking(this IWaybackContext context) {


            var transactionRecord = new AuditTransactionRecord() {
                TransactionID = Guid.NewGuid(),
                ChangeDate = DateTime.Now
            };
            var AddedEntities = new List<EntityEntry>();
            var changes = context.InternalDbContext.ChangeTracker.Entries().ToList();
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
                                    transactionRecord.Changes.Add(new AuditRecord() {
                                        PropertyName = field.Name,
                                        EntityID = oldID,
                                        TableName = context.InternalDbContext.GetTableNameFromType(field.DeclaringEntityType.ClrType) ?? "unknown",
                                        NewValue = entityPrimaryKey.ToString(),
                                        ChangeType = AuditEntryType.CollectionAdd
                                    });
                                }

                                transactionRecord.Changes.Add(new AuditRecord() {
                                    PropertyName = field.Name,
                                    EntityID = newID,
                                    TableName = context.InternalDbContext.GetTableNameFromType(field.DeclaringEntityType.ClrType) ?? "unknown",
                                    NewValue = entityPrimaryKey.ToString(),
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
                        transactionRecord.Changes.Add(new AuditRecord() {
                            PropertyName = prop.Name,
                            EntityID = index_a,
                            TableName = context.InternalDbContext.GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_b.ToString(),
                            ChangeType = AuditEntryType.CollectionRemove
                        });
                    }

                    foreach (var prop in fks[1].GetReferencingSkipNavigations()) {
                        transactionRecord.Changes.Add(new AuditRecord() {
                            PropertyName = prop.Name,
                            EntityID = index_b,
                            TableName = context.InternalDbContext.GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_a.ToString(),
                            ChangeType = AuditEntryType.CollectionRemove
                        });
                    }
                }
                if (entry.Entity is AuditRecord || entry.Entity is AuditTransactionRecord || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;
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

                        ChangeCount++;
                        transactionRecord.Changes.Add(new AuditRecord() {
                            PropertyName = property.Metadata.Name,
                            EntityID = (int)(IDProperty.GetValue(entry.Entity) ?? -1),
                            TableName = context.InternalDbContext.GetTableNameFromType(EntryType) ?? "unknown",
                            OldValue = property.OriginalValue?.ToString(),
                            NewValue = property.CurrentValue?.ToString(),
                        }); ;
                    }
                    continue;
                }
            }

            context.BaseSaveChanges();

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
                            transactionRecord.Changes.Add(new AuditRecord() {
                                PropertyName = field.Name,
                                EntityID = (int)(property.CurrentValue ?? -1),
                                TableName = context.InternalDbContext.GetTableNameFromType(field.DeclaringEntityType.ClrType) ?? "unknown",
                                NewValue = id.ToString(),
                                ChangeType = AuditEntryType.CollectionAdd
                            });
                        }
                    }
                } else {
                    var fks = entry.Metadata.GetForeignKeys().ToList();
                    var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                    var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);


                    foreach (var prop in fks[0].GetReferencingSkipNavigations()) {
                        transactionRecord.Changes.Add(new AuditRecord() {
                            PropertyName = prop.Name,
                            EntityID = index_a,
                            TableName = context.InternalDbContext.GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_b.ToString(),
                            ChangeType = AuditEntryType.CollectionAdd
                        });
                    }

                    foreach (var prop in fks[1].GetReferencingSkipNavigations()) {
                        transactionRecord.Changes.Add(new AuditRecord() {
                            PropertyName = prop.Name,
                            EntityID = index_b,
                            TableName = context.InternalDbContext.GetTableNameFromType(prop.DeclaringEntityType.ClrType) ?? "unknown",
                            NewValue = index_a.ToString(),
                            ChangeType = AuditEntryType.CollectionAdd
                        });
                    }
                }
            }
            context.AuditTransactions.Add(transactionRecord);
            return context.BaseSaveChanges();
        }
    }
}
