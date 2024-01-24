using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            (T o) => EF.Property<DateTime?>(o, "DeleteDate") == null;
    }
    public static class WaybackDbContextExtensions {
        internal static AuditTable? GetTableEnitity(this Type type, WaybackDbContext context, bool ReadOnly = false) {

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

        public static void Reload(this CollectionEntry source) {
            source.CurrentValue = null;
            source.IsLoaded = false;
            source.Load();
        }

        internal static AuditProperty? GetPropertyEntity(this PropertyEntry property, Type type, WaybackDbContext context, bool ReadOnly = false) {
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

            var contextType = context.InternalDbContext.GetType().GetProperties();
            var types = context.InternalDbContext.GetType()
                .GetProperties()
                .Where(s => s.PropertyType.IsGenericType)
                .Where(s => s.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(s => s.PropertyType.GenericTypeArguments.First())
                .Where(s => s.GetCustomAttribute(typeof(SoftDelete)) != null)
                .ToList();

            foreach (var type in types) {

                // TODO : Change this to a proper lamdba expression
                LambdaExpression? lambda = (LambdaExpression?)
                    (
                        typeof(SoftDeleteHelper<>)
                            .MakeGenericType(type)
                            .GetMethod("ExcludeSoftDeleted")
                                ?? throw new Exception("Failed to get soft delete generator method")
                    ).Invoke(null, null);

                modelBuilder.Entity(type)
                    .HasQueryFilter(lambda)
                    .Property(typeof(DateTime?), "DeleteDate")
                    .IsRequired(false);
            }
        }


        private static Dictionary<Type, string?> TypeToTableCache = new Dictionary<Type, string?>();
        private static Dictionary<string, Type?> TableToTypeCache = new Dictionary<string, Type?>();


        public static int FCount<T>(this IEnumerable<T> collection) {
            if (typeof(IWaybackSoftDeletable).IsAssignableFrom(typeof(T))) {
                return collection.Cast<IWaybackSoftDeletable>().Count(x => x.DeleteDate == null);
            }
            return collection.Count();
        }

        public static int FCount<T>(this IEnumerable<T> collection, Expression<Func<T, bool>> predicate) {
            if (typeof(IWaybackSoftDeletable).IsAssignableFrom(typeof(T))) {
                return collection.Cast<IWaybackSoftDeletable>()
                    .AsQueryable()
                    .Where(x => x.DeleteDate == null)
                    .Cast<T>()
                    .Count(predicate);
            }
            return collection.AsQueryable().Count(predicate);
        }

        public static T FFirst<T>(this IEnumerable<T> collection, Expression<Func<T, bool>> predicate) {
            if (typeof(IWaybackSoftDeletable).IsAssignableFrom(typeof(T))) {
                return collection.Cast<IWaybackSoftDeletable>()
                    .AsQueryable()
                    .Where(x => x.DeleteDate == null)
                    .Cast<T>()
                    .First(predicate);
            }
            return collection.AsQueryable().First(predicate);
        }

        public static T? FFirstOrDefault<T>(this IEnumerable<T> collection, Expression<Func<T, bool>> predicate) {
            if (typeof(IWaybackSoftDeletable).IsAssignableFrom(typeof(T))) {
                return collection.Cast<IWaybackSoftDeletable>()
                    .AsQueryable()
                    .Where(x => x.DeleteDate == null)
                    .Cast<T>()
                    .FirstOrDefault(predicate);
            }
            return collection.AsQueryable().FirstOrDefault(predicate);
        }

        public static T? FSingleOrDefault<T>(this IEnumerable<T> collection, Expression<Func<T, bool>> predicate) {
            if (typeof(IWaybackSoftDeletable).IsAssignableFrom(typeof(T))) {
                return collection.Cast<IWaybackSoftDeletable>()
                    .AsQueryable()
                    .Where(x => x.DeleteDate == null)
                    .Cast<T>()
                    .SingleOrDefault(predicate);
            }
            return collection.AsQueryable().SingleOrDefault(predicate);
        }

        public static IEnumerable<T> FWhere<T>(this IEnumerable<T> collection, Expression<Func<T, bool>> predicate) {
            if (typeof(IWaybackSoftDeletable).IsAssignableFrom(typeof(T))) {
                return collection.Cast<IWaybackSoftDeletable>()
                    .AsQueryable()
                    .Where(x => x.DeleteDate == null)
                    .Cast<T>()
                    .Where(predicate);
            }
            return collection.AsQueryable().Where(predicate);
        }

        internal static Type? GetTypeFromTableName(this DbContext dbcontext, WaybackDbContext trackingcontext, string t) {
            Type? result = null;


            if (TableToTypeCache.TryGetValue(t, out result))
                return result;

            result = dbcontext.Model.GetEntityTypes()
                .FirstOrDefault(s => s.ClrType.GetTableEnitity(trackingcontext).Name == t)?
                .ClrType;

            TableToTypeCache.Add(t, result);
            return result;
        }

        internal static dynamic FindSingleOrDefault(this DbContext dbcontext, string table, object Id) {
            var trackingDatabase = new WaybackDbContext();
            trackingDatabase.Database.EnsureCreated();
            Type TableType = dbcontext.GetTypeFromTableName(trackingDatabase, table)
               ?? throw new Exception($"Failed to get type for table {table}");

            var setMethod = typeof(DbContext).GetMethod("Set", new Type[] { }).MakeGenericMethod(TableType);
            object dbSet = setMethod.Invoke(dbcontext, null);

            var expressionParameter = Expression.Parameter(TableType);
            var expression = (Expression)Expression.Lambda(
                Expression.Equal(
                    Expression.Property(expressionParameter, TableType.GetPrimaryKeyField()),
                    Expression.Constant(Id)
                ), expressionParameter);

            var SingleOrDefaultIQFMethod = (typeof(WaybackDbContextExtensions)
                .GetMethod("IQF_SingleOrDefault")
                    ?? throw new Exception("Failed to get the IQF_SingleOrDefault Method"))
                .MakeGenericMethod(TableType);

            return SingleOrDefaultIQFMethod.Invoke(null, new[] { dbSet, expression });
        }

        internal static dynamic FindWhere(this DbContext dbcontext, string table, Expression expression) {
            var trackingDatabase = new WaybackDbContext();
            trackingDatabase.Database.EnsureCreated();
            Type TableType = dbcontext.GetTypeFromTableName(trackingDatabase, table)
               ?? throw new Exception($"Failed to get type for table {table}");

            var setMethod = typeof(DbContext).GetMethod("Set", new Type[] { }).MakeGenericMethod(TableType);
            object dbSet = setMethod.Invoke(dbcontext, null);

            var IQF_WhereMethod = (typeof(WaybackDbContextExtensions)
                .GetMethod("IQF_Where")
                    ?? throw new Exception("Failed to get the IQF_Where Method"))
                .MakeGenericMethod(TableType);

            return IQF_WhereMethod.Invoke(null, new[] { dbSet, expression });
        }


        internal static int GetPrimaryKeyValue(this object o) {
            var entity_id = (int)(o.GetType()
                          .GetProperties()
                          .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)))
                          .GetValue(o) ?? throw new Exception("Failed to get the KeyAttribute of the entity"));
            return entity_id;
        }



        internal static PropertyInfo GetPrimaryKeyField(this object o, Dictionary<Type, PropertyInfo> cache = null) {
            PropertyInfo? entity_id_field = null;
            if (cache.TryGetValue(o.GetType(), out entity_id_field)) return entity_id_field;
            lock (cache) {
                if (cache.TryGetValue(o.GetType(), out entity_id_field)) return entity_id_field;
                entity_id_field = o.GetType()
                              .GetProperties()
                              .First(s => s.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)) != null);
                if (cache != null)
                    cache.Add(o.GetType(), entity_id_field);
                return entity_id_field;
            }
        }

        internal static PropertyInfo GetPrimaryKeyField(this Type o, Dictionary<Type, PropertyInfo>? cache = null) {
            PropertyInfo? entity_id_field = null;
            if (cache != null) {
                if (cache.TryGetValue(o.GetType(), out entity_id_field)) return entity_id_field;
            }
            entity_id_field = o
                          .GetProperties()
                          .First(s => s.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)) != null);
            if (cache != null)
                cache.Add(o.GetType(), entity_id_field);
            return entity_id_field;
        }

        public static IQueryable<T> IQF_Where<T>(this DbSet<T> set, Expression filterExpression) where T : class {
            return set.IgnoreQueryFilters().Where((Expression<Func<T, bool>>)filterExpression);
        }

        public static T? IQF_SingleOrDefault<T>(this DbSet<T> set, Expression filterExpression) where T : class {
            return set.IgnoreQueryFilters().SingleOrDefault((Expression<Func<T, bool>>)filterExpression);
        }

        public static int SaveChangesWithTracking(this IWaybackContext context) {
            var trackingDatabase = new WaybackDbContext();
            trackingDatabase.Database.EnsureCreated();
            var totalChanges = 0;
            try {
                var changes = context.InternalDbContext.ChangeTracker.Entries().ToList();
                var explicitClassMode = (context.WaybackConfiguration.TrackingMode & WaybackConfig.TrackingModes.ExplicitClasses) != 0;
                var explicitPropertyMode = (context.WaybackConfiguration.TrackingMode & WaybackConfig.TrackingModes.ExplicitProperties) != 0;
                var addedEntities = new List<EntityEntry>();
                var temporaryProperties = new List<Tuple<PropertyEntry, AuditRecord>>();
                var transactionRecord = new AuditTransactionRecord() {
                    TransactionID = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow
                };

                if (changes.Count(x => x.State == EntityState.Modified) > 0) {
                    Console.WriteLine($"Save operation, running {changes.Count(x => x.State == EntityState.Modified)} changes...");
                }
                foreach (var entry in changes) {
                    var entryType = entry.Entity.GetType();
                    var isAuditable = entryType.GetCustomAttribute<Audit>() != null;
                    var isSoftDeletable = entryType.GetCustomAttribute<SoftDelete>() != null;
                    var isJunction = entryType.GetCustomAttribute<JunctionTable>() != null;

                    if (entry.Entity is AuditRecord || entry.Entity is AuditTransactionRecord) continue;
                    if (entry.State == EntityState.Deleted && isJunction) {
                        var fks = entry.Metadata.GetForeignKeys().ToList();

                        var table_a = fks[0].PrincipalEntityType.ClrType.GetTableEnitity(trackingDatabase);
                        var table_b = fks[1].PrincipalEntityType.ClrType.GetTableEnitity(trackingDatabase);
                        var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                        var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                        transactionRecord.Changes.Add(new AuditRecord() {
                            EntityID = entry.Entity.GetPrimaryKeyValue(),
                            Table = entryType.BaseType.GetTableEnitity(trackingDatabase),
                            J1 = index_a,
                            J2 = index_b,
                            J1Table = table_a,
                            J2Table = table_b,
                            ChangeType = AuditEntryType.CollectionRemove
                        });
                    }
                    if (entry.State == EntityState.Deleted && isSoftDeletable) {
                        var deleteProperty = entryType.GetBase().GetProperty("DeleteDate")
                            ?? throw new Exception($"The DeleteDate Property is not defined but the SoftDeleted attribute is defined for type {entryType.GetBase().FullName}");

                        deleteProperty.SetValue(entry.Entity, DateTime.UtcNow);
                        entry.State = EntityState.Unchanged;
                        continue;
                    }

                    if (entry.Entity is AuditRecord || entry.Entity is AuditTransactionRecord || entry.State == EntityState.Unchanged) continue;

                    if (entry.State == EntityState.Added) {
                        if (explicitClassMode && !isAuditable) continue;
                        addedEntities.Add(entry);
                        continue;
                    }
                    if (entry.State != EntityState.Modified) continue;

                    var idProperty = entry.Entity.GetPrimaryKeyField(context.WaybackConfiguration.PropertyPrimaryFieldTrackingCache);
                    var idValue = idProperty.GetValue(entry.Entity);
                    var tableEntity = entryType.GetTableEnitity(trackingDatabase)
                        ?? throw new Exception("Null returned for the table enitity");

                    foreach (var property in entry.Properties) {
                        if (property == null) continue;
                        if (!property.IsModified) continue;
                        if (explicitPropertyMode && !(explicitClassMode && isAuditable) && property.Metadata.PropertyInfo != null) {
                            if (property.Metadata.PropertyInfo.GetCustomAttribute(typeof(Audit)) == null) continue;
                        }

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
                            Property = property.GetPropertyEntity(entryType, trackingDatabase),
                            EntityID = (int)(idValue ?? -1),
                            Table = tableEntity,
                            OldValue = (converter != null ? OriginalValue_converted : OriginalValue)?.ToString(),
                            NewValue = (converter != null ? CurrentValue_converted : CurrentValue)?.ToString(),
                            ChangeType = AuditEntryType.PropertyOrReferenceChange
                        };

                        if (property.IsTemporary) temporaryProperties.Add(Tuple.Create(property, changeRecord));
                        transactionRecord.Changes.Add(changeRecord);
                    }
                }

                totalChanges = context.BaseSaveChanges();

                foreach (var entry in temporaryProperties) {
                    dynamic? CurrentValue = entry.Item1.CurrentValue;
                    var converter = entry.Item1.Metadata.GetValueConverter();
                    if (converter != null) CurrentValue = converter.ConvertToProvider(CurrentValue);
                    entry.Item2.NewValue = CurrentValue?.ToString();
                }


                //Console.WriteLine($"Starting insertion job...");
                //var sw = new Stopwatch();
                //foreach (var entry in addedEntities) {
                //    Type entryType = entry.Entity.GetType().GetBase()
                //        ?? throw new Exception("Failed to get the damn type");

                //    var primaryKeyProperty = entry.Entity.GetPrimaryKeyField(context.WaybackConfiguration.PropertyPrimaryFieldTrackingCache);
                //    var id = (int)(primaryKeyProperty.GetValue(entry.Entity) ?? -1);

                //    var IsJunction = entryType.GetCustomAttribute(typeof(JunctionTable), true) != null;
                //    if (IsJunction) {
                //        var fks = entry.Metadata.GetForeignKeys().ToList();
                //        var table_a = fks[0].PrincipalEntityType.ClrType.GetTableEnitity(trackingDatabase);
                //        var table_b = fks[1].PrincipalEntityType.ClrType.GetTableEnitity(trackingDatabase);
                //        var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                //        var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                //        transactionRecord.Changes.Add(new AuditRecord() {
                //            EntityID = id,
                //            Table = entryType.GetTableEnitity(trackingDatabase),
                //            J1 = index_a,
                //            J2 = index_b,
                //            J1Table = table_a,
                //            J2Table = table_b,
                //            ChangeType = AuditEntryType.CollectionAdd
                //        });
                //        continue;
                //    }
                //    transactionRecord.Changes.Add(new AuditRecord() {
                //        EntityID = id,
                //        Table = entryType.GetTableEnitity(trackingDatabase),
                //        ChangeType = AuditEntryType.Created
                //    });
                //}

                //sw.Stop();
                //Console.WriteLine($"Finished insertion job in {sw.ElapsedMilliseconds}ms...");



                if (transactionRecord.Changes.Count > 0 || addedEntities.Count > 0) {
                    trackingDatabase.AuditTransactions.Add(transactionRecord);
                    trackingDatabase.SaveChanges();
                }

                if (addedEntities.Count > 0) {
                    Console.WriteLine($"Starting insertion jobs...");
                    var sw = new Stopwatch();
                    sw.Start();
                    var addedJobTasks = addedEntities.Split(8)
                        .Select(x => CreateAddEntitiesToContextJob(x.ToList(), transactionRecord.ID, context))
                        .ToArray();

                    Task.WaitAll(addedJobTasks);
                    sw.Stop();
                    Console.WriteLine($"Finished insertion jobs in {sw.ElapsedMilliseconds}ms...");
                }

                return totalChanges;
            } catch (Exception) {
                throw;
            }
        }

        public static Task CreateAddEntitiesToContextJob(List<EntityEntry> addedEntities, int transactionRecordId, IWaybackContext context) {
            return Task.Run(() => {
                var trackingDatabase = new WaybackDbContext();
                var transactionRecord = trackingDatabase.AuditTransactions.Find(transactionRecordId);
                var jobId = Guid.NewGuid();
                Console.WriteLine($"{jobId} : Adding {addedEntities.Count} entities...");
                var sw = new Stopwatch();
                sw.Start();
                foreach (var entry in addedEntities) {
                    Type entryType = entry.Entity.GetType().GetBase()
                        ?? throw new Exception("Failed to get the damn type");

                    var primaryKeyProperty = entry.Entity.GetPrimaryKeyField(context.WaybackConfiguration.PropertyPrimaryFieldTrackingCache);
                    var id = (int)(primaryKeyProperty.GetValue(entry.Entity) ?? -1);

                    var IsJunction = entryType.GetCustomAttribute(typeof(JunctionTable), true) != null;
                    if (IsJunction) {
                        var fks = entry.Metadata.GetForeignKeys().ToList();
                        var table_a = fks[0].PrincipalEntityType.ClrType.GetTableEnitity(trackingDatabase);
                        var table_b = fks[1].PrincipalEntityType.ClrType.GetTableEnitity(trackingDatabase);
                        var index_a = (int)(fks[0].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);
                        var index_b = (int)(fks[1].Properties.First().FieldInfo?.GetValue(entry.Entity) ?? -1);

                        transactionRecord.Changes.Add(new AuditRecord() {
                            EntityID = id,
                            Table = entryType.GetTableEnitity(trackingDatabase),
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
                        Table = entryType.GetTableEnitity(trackingDatabase),
                        ChangeType = AuditEntryType.Created
                    });
                }
                trackingDatabase.SaveChanges();
                sw.Stop();
                Console.WriteLine($"{jobId} : Finished adding {addedEntities.Count} entities  in {sw.ElapsedMilliseconds}ms...");
            });
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts) {
            int i = 0;
            var splits = from item in list
                         group item by i++ % parts into part
                         select part.AsEnumerable();
            return splits;
        }
    }
}
