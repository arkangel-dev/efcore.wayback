using CastleProxiesTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine;
using WaybackMachine.Entities;
using WaybackMachine.FilterAttributes;

namespace WaybackMachine {
    public static class WaybackDbContextExtensions {
        public static void ConfigureWaybackModel(this IWaybackContext context, ModelBuilder modelBuilder) {
            modelBuilder.Entity<AuditTransactionRecord>()
                .HasMany(s => s.Changes)
                .WithOne(s => s.ParentTransaction);

            var contextType = context.InternalDbContext.GetType().GetProperties();
            var types = context.InternalDbContext.GetType()
                .GetProperties()
                .Where(s => s.PropertyType.IsGenericType)
                .Where(s => s.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(s => s.PropertyType.GenericTypeArguments.First())
                .Where(s => s.GetCustomAttribute(typeof(SoftDelete)) != null)
                .ToList();


            foreach (var type in types) {

                if (type.GetProperty("IsDeleted") == null)
                    throw new Exception($"Type of `{type.FullName}` does not have the required `IsDeleted` property. This is need for SoftDelete capabilities");

                ParameterExpression param = Expression.Parameter(type, "s");
                MemberExpression memberExpression = Expression.PropertyOrField(param, "IsDeleted");
                var returnExpression = Expression.Lambda(Expression.Equal(memberExpression, Expression.Constant(true)));
                modelBuilder.Entity(type)
                    .Property(typeof(bool), "IsDeleted")
                    .IsRequired(true);
            }
            //modelBuilder.Entity()
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

            var Changes = context.InternalDbContext.ChangeTracker.Entries().ToList();
            var AddedEntities = new List<EntityEntry>();
            var TransactionRecord = new AuditTransactionRecord() {
                TransactionID = Guid.NewGuid(),
                ChangeDate = DateTime.Now
            };

            foreach (var entry in Changes) {
                var entryType = entry.Entity.GetType();
                var IsJunction = entry.Entity.GetType().GetCustomAttribute(typeof(JunctionTable), true) != null;

                if (entry.Entity is AuditRecord || entry.Entity is AuditTransactionRecord) continue;
                if (entry.State == EntityState.Deleted && IsJunction) {
                    var fks = entry.Metadata.GetForeignKeys().ToList();

                    var table_a = context.InternalDbContext.GetTableNameFromType(fks[0].PrincipalEntityType.ClrType);
                    var table_b = context.InternalDbContext.GetTableNameFromType(fks[1].PrincipalEntityType.ClrType);
                    var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                    var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                    TransactionRecord.Changes.Add(new AuditRecord() {
                        PropertyName = "",
                        EntityID = entry.Entity.GetPrimaryKeyValue(),
                        TableName = context.InternalDbContext.GetTableNameFromType(entryType.BaseType) ?? "unknown",
                        J1 = index_a,
                        J2 = index_b,
                        J1Table = table_a,
                        J2Table = table_b,
                        ChangeType = AuditEntryType.CollectionRemove
                    });

                }

                if (entry.State == EntityState.Deleted && (entryType.BaseType?.GetCustomAttribute(typeof(SoftDelete)) ?? null) != null) {
                    var deleteProperty = entryType.BaseType?.GetProperty("IsDeleted")
                        ?? throw new Exception($"The IsDeleted Property is not defined but the SoftDeleted attribute is defined for type {entryType.BaseType.FullName}");

                    deleteProperty.SetValue(entry.Entity, true);
                    entry.State = EntityState.Modified;
                    continue;
                }

                if (entry.Entity is AuditRecord || entry.Entity is AuditTransactionRecord ||
                    entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;

                if (entry.State == EntityState.Added) {
                    AddedEntities.Add(entry);
                    continue;
                }

                if (entry.State != EntityState.Modified) continue;

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
                        var propInfo = entryType.GetProperty(propertyName);
                    }

                    var IDProperty = entry.Entity.GetType()
                        .GetProperties()
                        .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));

                    ChangeCount++;
                    TransactionRecord.Changes.Add(new AuditRecord() {
                        PropertyName = property.Metadata.Name,
                        EntityID = (int)(IDProperty.GetValue(entry.Entity) ?? -1),
                        TableName = context.InternalDbContext.GetTableNameFromType(IsJunction ? entryType.BaseType : entryType) ?? "unknown",
                        OldValue = property.OriginalValue?.ToString(),
                        NewValue = property.CurrentValue?.ToString(),
                    }); ;
                }
            }

            context.BaseSaveChanges();

            foreach (var entry in AddedEntities) {
                var entryType = entry.Entity.GetType();

                var entityPrimaryKeyProperty = entry.Entity.GetType()
                            .GetProperties()
                            .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));
                var id = (int)(entityPrimaryKeyProperty.GetValue(entry.Entity) ?? -1);

                var IsJunction = entryType.GetCustomAttribute(typeof(JunctionTable), true) != null;
                if (IsJunction) {
                    var fks = entry.Metadata.GetForeignKeys().ToList();

                    var table_a = context.InternalDbContext.GetTableNameFromType(fks[0].PrincipalEntityType.ClrType);
                    var table_b = context.InternalDbContext.GetTableNameFromType(fks[1].PrincipalEntityType.ClrType);

                    var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                    var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);



                    TransactionRecord.Changes.Add(new AuditRecord() {
                        PropertyName = "",
                        EntityID = entry.Entity.GetPrimaryKeyValue(),
                        TableName = context.InternalDbContext.GetTableNameFromType(entryType.BaseType) ?? "unknown",

                        J1 = index_a,
                        J2 = index_b,

                        J1Table = table_a,
                        J2Table = table_b,
                        ChangeType = AuditEntryType.CollectionAdd
                    });

                    continue;
                }

                var ChangeCount = 0;
                foreach (var property in entry.Properties) {
                    if (property == null) continue;
                    if (!property.Metadata.IsForeignKey()) continue;
                    var propertyName = property.Metadata.Name;

                    var CurrentValue = property.CurrentValue;
                    if (!property.Metadata.IsShadowProperty()) {
                        var propInfo = entryType.GetProperty(propertyName);
                    }

                    var IDProperty = entry.Entity.GetType()
                        .GetProperties()
                        .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));

                    ChangeCount++;
                    TransactionRecord.Changes.Add(new AuditRecord() {
                        PropertyName = property.Metadata.Name,
                        EntityID = (int)(IDProperty.GetValue(entry.Entity) ?? -1),
                        TableName = context.InternalDbContext.GetTableNameFromType(entryType) ?? "unknown",
                        NewValue = property.CurrentValue?.ToString(),
                        ChangeType = AuditEntryType.CollectionAdd
                    });
                }




            }
            context.AuditTransactions.Add(TransactionRecord);
            return context.BaseSaveChanges();
        }
    }
}
