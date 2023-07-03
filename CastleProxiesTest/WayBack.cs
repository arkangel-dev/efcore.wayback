using Castle.Components.DictionaryAdapter.Xml;
using Castle.DynamicProxy;
using CastleProxiesTest.DbEntities;
using CastleProxiesTest.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace CastleProxiesTest {
    public class WayBack {

        internal DatabaseContext _dbcontext;
        internal ProxyGenerator _generator;
        internal DateTime _revertPoint;
        private WayBack(DatabaseContext dbcontext, DateTime revertPoint) {
            _dbcontext = dbcontext;
            _revertPoint = revertPoint;
            _generator = new ProxyGenerator();
        }

        public static WayBack CreateWayBack(DatabaseContext dbcontext, DateTime revertPoint) {
            return new WayBack(dbcontext, revertPoint);
        }

        /// <summary>
        /// Get an entity from the database context provided
        /// </summary>
        /// <typeparam name="T">Type parameter to use</typeparam>
        /// <param name="fetchExpression">The fetch expression used to fetch the object</param>
        /// <returns>The newly created wayback proxy</returns>
        public T GetEntity<T>(Expression<Func<T, bool>> fetchExpression) where T : class {
            DbSet<T>? dbSet = _dbcontext.GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .FirstOrDefault(p => p.PropertyType.GetGenericArguments().FirstOrDefault() == typeof(T))
                ?.GetValue(_dbcontext) as DbSet<T>;

            if (dbSet == null) throw new Exception("DbSet fetch returned null");
            var result = dbSet.First(fetchExpression);

            object? returnObj = _entityCacheProxies.Values.FirstOrDefault(x => x == result);
            if (returnObj != null) return (T)returnObj;
            if (_entityCacheProxies.TryGetValue(result, out returnObj))
                return (T)returnObj;

            var type = typeof(T);
            var interceptor = new WayBackInterceptor(result, this);
            returnObj = _generator.CreateClassProxy<T>(interceptor);

            _entityCacheProxies.Add(result, returnObj);
            RevertObject(returnObj, result);

            return (T)returnObj;
        }
        private Dictionary<object, object> _entityCacheProxies = new Dictionary<object, object>();

        /// <summary>
        /// Generate a proxy entity
        /// </summary>>
        public object GenerateEntity(object _target, Type t) {

            object? returnObj = _entityCacheProxies.Values.FirstOrDefault(x => x == _target);
            if (returnObj != null) return returnObj;
            if (_entityCacheProxies.TryGetValue(_target, out returnObj))
                return returnObj;


            var interceptor = new WayBackInterceptor(_target, this);
            returnObj = _generator.CreateClassProxy(t, interceptor);
            _entityCacheProxies.Add(_target, returnObj);

            RevertObject(returnObj, _target);
            return returnObj;
        }

        /// <summary>
        /// Generate a generic proxy entity
        /// </summary>
        public T GenerateEntityGeneric<T>(object _target, Type t) {

            object? returnObj = _entityCacheProxies.Values.FirstOrDefault(x => x == _target);
            if (returnObj != null) return (T)returnObj;
            if (_entityCacheProxies.TryGetValue(_target, out returnObj))
                return (T)returnObj;

            var interceptor = new WayBackInterceptor(_target, this);
            returnObj = _generator.CreateClassProxy(t, interceptor);

            _entityCacheProxies.Add(_target, returnObj);
            RevertObject(returnObj, _target);

            return (T)returnObj;
        }

        /// <summary>
        /// Check if the provided type is supported by the wayback
        /// </summary>
        public bool SupportsType(Type T) =>
            _dbcontext.GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Any(p => p.PropertyType.GetGenericArguments().FirstOrDefault() == T);


        public object? GetCachedEntity(string tablename, int id) {
            object? _returnVal = _dbcontext.FindEntity(tablename, id);

            object? cacheCheck = null;
            if (_entityCacheProxies.TryGetValue(_returnVal, out cacheCheck))
                return cacheCheck;

            var _type = _dbcontext.GetTypeFromTableName(tablename);

            _returnVal = GenerateEntity(_returnVal, _type);
            return _returnVal;
        }

        public object? GetCachedEntity(Type type, int id) {
            object? _returnVal = _dbcontext.FindEntity(_dbcontext.GetTableNameFromType(type)
                ?? throw new Exception($"Failed to get the tablename for {type.FullName}"), id);

            object? cacheCheck = null;
            if (_entityCacheProxies.TryGetValue(_returnVal, out cacheCheck))
                return cacheCheck;


            _returnVal = GenerateEntity(_returnVal, type);
            return _returnVal;
        }

        private object RevertObject(object _target, object _reference) {
            var targetBaseType = _target.GetType().BaseType ?? throw new Exception("Cannot get a proper base type from this type! Is this even an attached entity?");


            var entityID = (int)(_target.GetType()
                            .GetProperties()
                            .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)))
                            .GetValue(_reference) ?? throw new Exception("Failed to get the KeyAttribute of the entity"));

            var tableName = _dbcontext.GetTableNameFromType(targetBaseType);

            var auditLogs = _dbcontext.AuditEntries
                .Where(s => s.EntityID == entityID && s.TableName == tableName && s.ChangeDate >= _revertPoint)
                .OrderByDescending(s => s.ChangeDate)
                .ToList();

            foreach (var property in targetBaseType.GetProperties()) {
                if (property.SetMethod?.IsVirtual ?? false || (property.GetMethod?.IsVirtual ?? false)) continue;
                property.SetValue(_target, property.GetValue(_reference));
            }





            foreach (var al in auditLogs) {
                var property = targetBaseType.GetProperty(al.PropertyName);
                if (property == null) continue;


                if (al.ChangeType == AuditEntryType.PropertyOrReferenceChange) {
                    object value = al.OldValue ?? "";
                    if (property.PropertyType != typeof(string)) {
                        var parseMethod = property.PropertyType.GetMethod("Parse");
                        if (parseMethod == null) continue;
                        value = parseMethod.Invoke(null, new[] { (string)value }) ?? throw new Exception("The parse method didn't return anything");
                    }
                    property.SetValue(_target, value);
                } else {
                    //IList? collection = (IList?)property.GetValue(_reference);

                    //if (collection == null) continue;

                    //var collection_type = property.PropertyType.GetGenericArguments().First() 
                    //    ?? throw new Exception("Failed to get generic type");

                    //var result = GetCachedEntity(_dbcontext.GetTableNameFromType(collection_type) 
                    //    ?? $"Failed to get table name for {collection_type.FullName}", Int32.Parse(al.NewValue));


                    //var proxied_collection = (IList)(Activator.CreateInstance(
                    //    typeof(List<>).MakeGenericType(collection_type)) ?? throw new Exception("Failed to create list inst"));

                    //foreach (var item in collection) {
                    //    proxied_collection.Add(GenerateEntity(item, collection_type));
                    //}


                    //switch (al.ChangeType) {
                    //    case AuditEntryType.CollectionAdd:

                    //        var x = proxied_collection.Contains(result);
                    //        proxied_collection.Remove(result);
                    //        break;

                    //    case AuditEntryType.CollectionRemove:
                    //        proxied_collection.Add(result);
                    //        break;

                    //    default:
                    //        break;
                    //}
                    //property.SetValue(_target, proxied_collection, null);

                }
            }

            return _target;
        }
    }

    public class WayBackInterceptor : IInterceptor {
        internal WayBack _wayback;
        internal object _target;

        /// <summary>
        /// Constructor of the wayback interceptor
        /// </summary>
        /// <param name="target">Target to intercept calls</param>
        /// <param name="wb">The wayback context to use</param>
        /// <param name="rp">The revert point to use</param>
        public WayBackInterceptor(object target, WayBack wb) {
            _wayback = wb;
            _target = target;


            if (!_target.GetType().GetProperties().Any(s => s.Name == "LazyLoader"))
                throw new Exception("Invalid Target Passed");

        }

        public void Intercept(IInvocation invocation) {


            if (invocation.Method.IsVirtual && invocation.Method.Name.StartsWith("get_") && invocation.Method.IsSpecialName) {
                var returnType = invocation.Method.ReturnParameter.ParameterType;


                
                var entity_id = (int)(_target.GetType()
                          .GetProperties()
                          .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)))
                          .GetValue(_target) ?? throw new Exception("Failed to get the KeyAttribute of the entity"));


                // The return type is directly supported by the wayback 
                // machine. This means its a direct type
                if (_wayback.SupportsType(returnType)) {
                    var waybackGeneratorMethod = (typeof(WayBack).GetMethod("GenerateEntityGeneric")
                        ?? throw new Exception("Couldn't get the method that generates proxies in the wayback class (⊙_⊙;)")
                    ).MakeGenericMethod(returnType);
                    var entityType = _wayback._dbcontext.Model.FindEntityType(returnType) ?? throw new Exception($"Cannot get entity type of {returnType}");
                    var table_name = entityType.GetTableName() ?? string.Empty;
                    var nav_property = returnType.GetProperty(String.Join(String.Empty, invocation.Method.Name.Skip(4))) ?? throw new Exception($"Failed to get property for {String.Join(String.Empty, invocation.Method.Name.Skip(4))}");
                    var columnName = entityType.FindNavigation(nav_property.Name)?.ForeignKey.Properties.First().Name;


                    var invocation_result = invocation.Method.Invoke(_target, invocation.Arguments);


                    var revertpoint_old_key = _wayback._dbcontext.AuditEntries
                        .Where(s =>
                            s.EntityID == entity_id &&
                            s.TableName == table_name &&
                            s.PropertyName == columnName
                        )
                        .OrderByDescending(s => s.ChangeDate)
                        .FirstOrDefault(s => s.ChangeDate <= _wayback._revertPoint);

                    if (revertpoint_old_key == null) {
                        if (invocation_result == null) {
                            invocation.ReturnValue = null;
                            return;
                        }
                        invocation.ReturnValue = _wayback.GenerateEntity(invocation_result, returnType);
                        return;
                    }

                    if (revertpoint_old_key.NewValue == null) {
                        invocation.ReturnValue = null;
                        return;
                    }


                    invocation.ReturnValue =
                        _wayback.GenerateEntity(_wayback._dbcontext.FindEntity(table_name, Int32.Parse(revertpoint_old_key.NewValue)), returnType);

                    return;
                }

                // Check if the methods return type has generic
                // type arguments, This might be a sign that its a
                // collection
                if (returnType.GenericTypeArguments.Any()) {

                    var genericType = returnType.GenericTypeArguments.First();
                    var entityType = _target.GetType().BaseType;
                    var efCoreEntityType = _wayback._dbcontext.Model.FindEntityType(entityType) 
                        ?? throw new Exception($"Cannot get entity type of {entityType}");

                    var nav_property = entityType.GetProperty(String.Join(String.Empty, invocation.Method.Name.Skip(4))) 
                        ?? throw new Exception($"Failed to get property for {String.Join(String.Empty, invocation.Method.Name.Skip(4))}");

                    var columnName = nav_property.Name;
                    var table_name = efCoreEntityType.GetTableName() ?? string.Empty;

                    // Check if the generic type is supported by the
                    // wayback machine
                    if (_wayback.SupportsType(genericType)) {

                        // Check if its an object that supports a list
                        var castType = typeof(List<>).MakeGenericType(genericType);
                        if (castType.IsAssignableFrom(returnType)) {

                            // Get the actual result from the EFCore object
                            // and if its null then also return null
                            var invocationResult = (IList?)invocation.Method.Invoke(_target, invocation.Arguments);
                            if (invocationResult == null) {
                                invocationResult = (IList)(Activator.CreateInstance(castType)
                                    ?? throw new Exception($"Failed to create instance of List<{genericType.FullName}>"));
                            }


                            // Get the .GenerateEntityGeneric(object, Type, DateTime) method from the
                            // wayback context and store it in a variable so we can invoke it in the loop
                            // Oh and make it a generic method and apply the generic type 

                            //var waybackGeneratorMethod = (typeof(WayBack).GetMethod("GenerateEntityGeneric")
                            //    ?? throw new Exception("Couldn't get the method that generates proxies in the wayback class (⊙_⊙;)")
                            //).MakeGenericMethod(genericType);


                            var returnList = (IList)(Activator.CreateInstance(castType)
                                ?? throw new Exception($"Failed to create instance of List<{genericType.FullName}>"));

                            // Loop over the invocation results
                            // and create proxies and add them to the list
                            foreach (object obj in invocationResult) {
                                returnList.Add(_wayback.GenerateEntity(obj, genericType));
                            }

                            var revertAuditLogs = _wayback._dbcontext.AuditEntries
                                .Where(s => 
                                    s.EntityID == entity_id &&
                                    s.TableName == table_name &&
                                    s.PropertyName == columnName &&
                                    s.ChangeDate >= _wayback._revertPoint
                                )
                                .OrderByDescending(s => s.ChangeDate)
                                .ToList();

                            foreach (var auditEntry in revertAuditLogs) {
                                switch (auditEntry.ChangeType) {
                                    case AuditEntryType.CollectionAdd:
                                        returnList.Remove(_wayback.GetCachedEntity(genericType, Int32.Parse(auditEntry.NewValue ?? "-1")));
                                        break;

                                    case AuditEntryType.CollectionRemove:
                                        returnList.Add(_wayback.GetCachedEntity(genericType, Int32.Parse(auditEntry.NewValue ?? "-1")));
                                        break;
                                }

                            }


                            invocation.ReturnValue = returnList;
                            return;
                        }
                    }
                }
            }
            invocation.ReturnValue = invocation.Method.Invoke(_target, invocation.Arguments);
            //invocation.Proceed();
        }


    }

    public static class MyExtensions {
        public static IQueryable<object> TypeSet(this DbContext _context, Type t) {
            return (IQueryable<object>)_context.GetType().GetMethod("Set", types: Type.EmptyTypes).MakeGenericMethod(t).Invoke(_context, null);
        }
    }
}
