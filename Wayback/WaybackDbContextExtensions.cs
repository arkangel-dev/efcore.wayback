using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine;
using WaybackMachine.Entities;
using WaybackMachine.FilterAttributes;

namespace WaybackMachine {

    public static class SoftDeleteHelper<T> {
        public static LambdaExpression ExcludeSoftDeleted() =>
            (T o) => !EF.Property<bool>(o, "IsDeleted");
    }
    public static class WaybackDbContextExtensions {
        internal static AuditTable GetTableEnitity(this Type type, IWaybackContext context, bool ReadOnly = false) {
            var typeName = type.GetBase().Name;
            var result = context.AuditTables.FirstOrDefault(s => s.Name == typeName);
            if (result == null) result = context._tempAuditTables.FirstOrDefault(s => s.Name == typeName);
            if (result == null && !ReadOnly) {
                result = new AuditTable() { Name = typeName };
                context._tempAuditTables.Add(result);
                context.AuditTables.Add(result);
            }
            return result;
        }

        internal static AuditProperty GetPropertyEntity(this PropertyEntry property, Type type, IWaybackContext context, bool ReadOnly = false) {
            var propertyName = property.Metadata.Name;

            var tableentity = type.GetTableEnitity(context);
            var result = context.AuditProperties.FirstOrDefault(s => s.Name == propertyName && s.ParentTable.ID == tableentity.ID);
            if (result == null) result = context._tempAuditProperties.FirstOrDefault(s => s.Name == propertyName);
            if (result == null && !ReadOnly) {
                result = new AuditProperty() {
                    Name = propertyName,
                    ParentTable = tableentity
                };
                context._tempAuditProperties.Add(result);
                context.AuditProperties.Add(result);
            }
            return result;
        }

        internal static Type GetBase(this Type type) {
            Type currentType = type;
            while (true) {
                if (currentType.BaseType == typeof(object) || currentType.BaseType == null) return currentType;
                currentType = currentType.BaseType;
            }
        }


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

            //foreach (var type in types) {


            //    LambdaExpression lambda = (LambdaExpression)
            //        (typeof(SoftDeleteHelper<>).MakeGenericType(type).GetMethod("ExcludeSoftDeleted") ?? throw new Exception()).Invoke(null, null);


            //    modelBuilder.Entity(type)
            //        .HasQueryFilter(lambda)
            //        .Property(typeof(bool), "IsDeleted")
            //        .IsRequired(true);
            //}
            //modelBuilder.Entity()
        }


        private static Dictionary<Type, string?> TypeToTableCache = new Dictionary<Type, string?>();
        private static Dictionary<string, Type?> TableToTypeCache = new Dictionary<string, Type?>();

        internal static string? GetTableNameFromType(this DbContext dbcontext, Type t) {
            string? result = null;

            if (TypeToTableCache.TryGetValue(t, out result))
                return result;

            result = dbcontext.Model.GetEntityTypes()
                .FirstOrDefault(s => s.ClrType == t)?
                .GetAnnotation("Relational:TableName")
                .Value?.ToString();

            TypeToTableCache.Add(t, result);
            return result;
        }

        internal static Type? GetTypeFromTableName(this DbContext dbcontext, string t) {
            Type? result = null;

            if (TableToTypeCache.TryGetValue(t, out result))
                return result;

            result = dbcontext.Model.GetEntityTypes()
            .FirstOrDefault(s => s.ClrType.GetTableEnitity((IWaybackContext)dbcontext).Name == t)
            ?.ClrType;

            TableToTypeCache.Add(t, result);
            return result;
        }

        internal static int GetKey<T>(this DbContext dbcontext, T entity) {
            var keyName = dbcontext.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties
                .Select(x => x.Name).Single();
            return (int)entity.GetType().GetProperty(keyName).GetValue(entity, null);
        }

        internal static dynamic FindEntity(this DbContext dbcontext, string table, object Id) {
            Type TableType = dbcontext.GetTypeFromTableName(table)
                ?? throw new Exception($"Failed to get type for table {table}");

            Type DbSetType = typeof(DbSet<>).MakeGenericType(TableType);
            PropertyInfo prop = dbcontext.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(s => s.PropertyType == DbSetType)
                ?? throw new Exception($"Failed to get the DbSet property for typeof {TableType.Name} from DatabaseContext");

            dynamic dbSet = prop.GetValue(dbcontext, null)
                ?? throw new Exception($"DbSet property returned null");
            return dbSet.Find(Id);
        }

        public static int SaveChangesWithTracking(this IWaybackContext context) {

            var transaction = context.InternalDbContext.Database.BeginTransaction();
            try {
                var changes = context.InternalDbContext.ChangeTracker.Entries().ToList();
                var explicitClassMode = (context.WaybackConfiguration.TrackingMode & WaybackConfig.TrackingModes.ExplicitClasses) != 0;
                var explicitPropertyMode = (context.WaybackConfiguration.TrackingMode & WaybackConfig.TrackingModes.ExplicitProperties) != 0;
                var addedEntities = new List<EntityEntry>();
                var temporaryProperties = new List<Tuple<PropertyEntry, AuditRecord>>();
                var transactionRecord = new AuditTransactionRecord() {
                    TransactionID = Guid.NewGuid(),
                    ChangeDate = DateTime.Now
                };

                foreach (var entry in changes) {
                    var entryType = entry.Entity.GetType();
                    var isAuditable = entryType.GetCustomAttribute<Audit>() != null;
                    var isSoftDeletable = entryType.GetCustomAttribute<SoftDelete>() != null;
                    var isJunction = entryType.GetCustomAttribute<JunctionTable>() != null;

                    if (entry.Entity is AuditRecord || entry.Entity is AuditTransactionRecord) continue;
                    if (entry.State == EntityState.Deleted && isJunction) {
                        var fks = entry.Metadata.GetForeignKeys().ToList();

                        var table_a = fks[0].PrincipalEntityType.ClrType.GetTableEnitity(context);
                        var table_b = fks[1].PrincipalEntityType.ClrType.GetTableEnitity(context);
                        var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                        var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                        transactionRecord.Changes.Add(new AuditRecord() {
                            EntityID = entry.Entity.GetPrimaryKeyValue(),
                            Table = entryType.BaseType.GetTableEnitity(context),
                            J1 = index_a,
                            J2 = index_b,
                            J1Table = table_a,
                            J2Table = table_b,
                            ChangeType = AuditEntryType.CollectionRemove
                        });
                    }
                    if (entry.State == EntityState.Deleted && isSoftDeletable) {
                        var deleteProperty = entryType.BaseType?.GetProperty("IsDeleted")
                            ?? throw new Exception($"The IsDeleted Property is not defined but the SoftDeleted attribute is defined for type {entryType.BaseType.FullName}");

                        deleteProperty.SetValue(entry.Entity, true);
                        entry.State = EntityState.Modified;
                        continue;
                    }

                    if (entry.Entity is AuditRecord || entry.Entity is AuditTransactionRecord || entry.State == EntityState.Unchanged) continue;

                    if (entry.State == EntityState.Added) {
                        if (explicitClassMode) {
                            if (isAuditable) continue;
                        }
                        addedEntities.Add(entry);
                        continue;
                    }
                    if (entry.State != EntityState.Modified) continue;

                    var IDProperty = entry.Entity.GetPrimaryKeyField(context.WaybackConfiguration.PropertyPrimaryFieldTrackingCache);
                    var IDValue = IDProperty.GetValue(entry.Entity);
                    var tableEntity = entryType.GetTableEnitity(context);

                    foreach (var property in entry.Properties) {
                        if (property == null) continue;
                        if (!property.IsModified) continue;
                        if (explicitPropertyMode && !(explicitClassMode && isAuditable))
                            if (property.Metadata.FieldInfo.GetCustomAttribute(typeof(Audit)) == null) continue;

                        object? CurrentValue = property.CurrentValue;
                        object? OriginalValue = property.OriginalValue;

                        object? CurrentValue_converted = null;
                        object? OriginalValue_converted = null;
                        var converter = property.Metadata.GetValueConverter();

                        if (converter != null) {
                            CurrentValue_converted = converter.ConvertToProvider(CurrentValue);
                            OriginalValue_converted = converter.ConvertToProvider(OriginalValue);
                        } 

                        var changeRecord = new AuditRecord() {
                            Property = property.GetPropertyEntity(entryType, context),
                            EntityID = (int)(IDValue ?? -1),
                            Table = tableEntity,
                            OldValue = (converter != null ? OriginalValue_converted : OriginalValue)?.ToString(),
                            NewValue = (converter != null ? CurrentValue_converted : CurrentValue)?.ToString(),
                            ChangeType = AuditEntryType.PropertyOrReferenceChange
                        };

                        if (property.IsTemporary)
                            temporaryProperties.Add(Tuple.Create(property, changeRecord));

                        transactionRecord.Changes.Add(changeRecord);
                    }
                }

                context.BaseSaveChanges();

                foreach (var entry in temporaryProperties) {
                    dynamic? CurrentValue = entry.Item1.CurrentValue;
                    var converter = entry.Item1.Metadata.GetValueConverter();
                    if (converter != null) CurrentValue = converter.ConvertToProvider(CurrentValue);
                    entry.Item2.NewValue = CurrentValue?.ToString();
                }

                foreach (var entry in addedEntities) {
                    Type entryType = entry.Entity.GetType().GetBase()
                        ?? throw new Exception("Failed to get the damn type");

                    var entityPrimaryKeyProperty = entry.Entity.GetPrimaryKeyField(context.WaybackConfiguration.PropertyPrimaryFieldTrackingCache);
                    var id = (int)(entityPrimaryKeyProperty.GetValue(entry.Entity) ?? -1);

                    var IsJunction = entryType.GetCustomAttribute(typeof(JunctionTable), true) != null;
                    if (IsJunction) {
                        var fks = entry.Metadata.GetForeignKeys().ToList();

                        var table_a = fks[0].PrincipalEntityType.ClrType.GetTableEnitity(context);
                        var table_b = fks[1].PrincipalEntityType.ClrType.GetTableEnitity(context);

                        var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                        var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                        transactionRecord.Changes.Add(new AuditRecord() {
                            EntityID = entry.Entity.GetPrimaryKeyValue(),
                            Table = entryType.GetTableEnitity(context),

                            J1 = index_a,
                            J2 = index_b,

                            J1Table = table_a,
                            J2Table = table_b,
                            ChangeType = AuditEntryType.CollectionAdd
                        });
                        continue;
                    }
                    transactionRecord.Changes.Add(new AuditRecord() {
                        EntityID = id,
                        Table = entryType.GetTableEnitity(context),
                        ChangeType = AuditEntryType.Created
                    });
                }
                context.AuditTransactions.Add(transactionRecord);
                transaction.Commit();
                return context.BaseSaveChanges();
            } catch (Exception) {
                transaction.Rollback();
                throw;
            }
        }
    }
}
