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
        public T DbSetFirst<T>(Expression<Func<T, bool>> fetchExpression) where T : class {

            // Get the dbset
            DbSet<T>? dbSet = _dbcontext.GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .FirstOrDefault(p => p.PropertyType.GetGenericArguments().FirstOrDefault() == typeof(T))
                ?.GetValue(_dbcontext) as DbSet<T>;

            // If the DbSet is null then throw a
            // temper tantrum
            if (dbSet == null) throw new Exception("DbSet fetch returned null");

            // Get the entity that matches the first expression
            var result = dbSet.First(fetchExpression);

            // Try to get the entity from the cache dictionary
            // and if successful return that
            var returnObj = _entityCacheProxies.Values.FirstOrDefault(x => x == result);
            if (returnObj != null) return (T)returnObj;
            if (_entityCacheProxies.TryGetValue(result, out returnObj))
                return (T)returnObj;

            // Create a new wayback interceptor
            // then add to the cache and then revert the simple
            // properties
            var interceptor = new WayBackInterceptor(result, this);
            returnObj = _generator.CreateClassProxy<T>(interceptor);
            _entityCacheProxies.Add(result, returnObj);
            RevertObject(returnObj, result);
            return (T)returnObj;
        }
        /// <summary>
        /// This dictionary keeps track of EFCore entities to Wayback Entities. Significatly helps
        /// with memory optimization
        /// </summary>
        private Dictionary<object, object> _entityCacheProxies = new Dictionary<object, object>();

        /// <summary>
        /// Generate a proxy entity
        /// </summary>
        /// <param name="_target">Object to make a proxy out of. It has to be an EFCore Proxy Entity</param>
        /// <param name="t">Type to </param>
        /// <returns>The Wayback Entity for the provided EFCore Entity</returns>
        public object GenerateEntity(object _target, Type t) {
            // Try to get the entity from the cache and 
            // succesful return that
            object? returnObj = _entityCacheProxies.Values.FirstOrDefault(x => x == _target);
            if (returnObj != null) return returnObj;
            if (_entityCacheProxies.TryGetValue(_target, out returnObj))
                return returnObj;

            // Other wise create a new wayback intercept based on the object
            // revert the changes and return it
            var interceptor = new WayBackInterceptor(_target, this);
            returnObj = _generator.CreateClassProxy(t, interceptor);
            _entityCacheProxies.Add(_target, returnObj);
            RevertObject(returnObj, _target);
            return returnObj;
        }

        /// <summary>
        /// Generate a proxy entity based on an EFCore entity that can be fetched by the tablename and primary key
        /// </summary>
        /// <param name="tablename">Table name to target</param>
        /// <param name="id">Primary key to target</param>
        /// <returns>Proxy entity to use</returns>        
        public object GenerateEntity(string tablename, int id) {
            object? _returnVal = _dbcontext.FindEntity(tablename, id);

            object? cacheCheck = null;
            if (_entityCacheProxies.TryGetValue(_returnVal, out cacheCheck))
                return cacheCheck;

            var _type = _dbcontext.GetTypeFromTableName(tablename);

            _returnVal = GenerateEntity(_returnVal, _type);
            return _returnVal;
        }

        /// <summary>
        /// Generate a proxy entity based on an EFCore entity that can be fetched by the entity take and primary key
        /// </summary>
        /// <param name="type">Type to taget</param>
        /// <param name="id">Primary key of the target table name to target</param>
        /// <returns>Proxy entity to use</returns>
        public object? GenerateEntity(Type type, int id) {
            object? _returnVal = _dbcontext.FindEntity(_dbcontext.GetTableNameFromType(type)
                ?? throw new Exception($"Failed to get the tablename for {type.FullName}"), id);

            object? cacheCheck = null;
            if (_entityCacheProxies.TryGetValue(_returnVal, out cacheCheck))
                return cacheCheck;


            _returnVal = GenerateEntity(_returnVal, type);
            return _returnVal;
        }

        /// <summary>
        /// Check if the provided type is supported by the wayback
        /// </summary>
        public bool SupportsType(Type T) =>
            _dbcontext.GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Any(p => p.PropertyType.GetGenericArguments().FirstOrDefault() == T);

        /// <summary>
        /// Revert an object to its previous state. Note that it will only revert normal properties and 
        /// skip virtual navigational properties 
        /// </summary>
        /// <param name="_target">Object to apply the changes on (Wayback Entity)</param>
        /// <param name="_reference">Object to copy the initial values from (EFCore Entity)</param>
        /// <returns>The target but reverted</returns>
        private object RevertObject(object _target, object _reference) {

            // Get the base type of the entity
            // GetType will return the proxy
            var targetBaseType = _target.GetType().BaseType
                ?? throw new Exception("Cannot get a proper base type from this type! Is this even an attached entity?");


            // Get the entity ID by getting the ID property
            // and using it on the entity
            var entityID = (int)(_target.GetType()
                            .GetProperties()
                            .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)))
                            .GetValue(_reference)
                ?? throw new Exception("Failed to get the KeyAttribute of the entity"));

            // Get the table name of the entity
            var tableName = _dbcontext.GetTableNameFromType(targetBaseType);

            // Get the change history for the entity
            var auditLogs = _dbcontext.AuditEntries
                .Where(s =>
                    s.EntityID == entityID &&
                    s.TableName == tableName &&
                    s.ChangeDate >= _revertPoint &&
                    s.ChangeType == AuditEntryType.PropertyOrReferenceChange)
                .OrderByDescending(s => s.ChangeDate);

            // Copy the values from the reference EFCore
            // entity over to the target wayback entity
            foreach (var property in targetBaseType.GetProperties()) {
                if (property.SetMethod?.IsVirtual ?? false || (property.GetMethod?.IsVirtual ?? false)) continue;
                property.SetValue(_target, property.GetValue(_reference));
            }

            // Iterate over the audit logs and revert the changes
            foreach (var al in auditLogs) {

                // Get the property based on the auditlog PropertyName
                // if it returns null, then continue
                var property = targetBaseType.GetProperty(al.PropertyName);
                if (property == null) continue;

                // Try to get the old value
                // and if its not a string, then try to invoke the
                // parse method. That will work for most properties
                object value = al.OldValue ?? "";
                if (property.PropertyType != typeof(string)) {
                    var parseMethod = property.PropertyType.GetMethod("Parse");
                    if (parseMethod == null) continue;
                    value = parseMethod.Invoke(null, new[] { (string)value }) ?? throw new Exception("The parse method didn't return anything");
                }

                // Set the value
                property.SetValue(_target, value);
            }
            return _target;
        }
    }

    public class WayBackInterceptor : IInterceptor {
        internal WayBack _wayback;
        internal object _target;

        /// <summary>
        /// This is a cache dicitonary that will help improve read times
        /// </summary>
        private Dictionary<string, object?> ReadResultCacheDictionary
            = new Dictionary<string, object?>();

        /// <summary>
        /// Constructor of the wayback interceptor
        /// </summary>
        /// <param name="target">Target to intercept calls</param>
        /// <param name="wb">The wayback context to use</param>
        /// <param name="rp">The revert point to use</param>
        public WayBackInterceptor(object target, WayBack wb) {
            _wayback = wb;
            _target = target;

            // If the target type is not an EFCore entity (this is done by
            // checking if its type contains a lazyloader property, not foolproof)
            // then throw an error
            if (!_target.GetType().GetProperties().Any(s => s.Name == "LazyLoader"))
                throw new Exception("Invalid Target Passed");
        }

        public void Intercept(IInvocation invocation) {
            if (invocation.Method.IsVirtual && invocation.Method.Name.StartsWith("get_") && invocation.Method.IsSpecialName) {


                object? cacheResult = null;
                if (ReadResultCacheDictionary.TryGetValue(invocation.Method.Name, out cacheResult)) {
                    invocation.ReturnValue = cacheResult;
                    return;
                }

                // Save the return type and the entity ID
                var returnType = invocation.Method.ReturnParameter.ParameterType;
                var entity_id = (int)(_target.GetType()
                          .GetProperties()
                          .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)))
                          .GetValue(_target) ?? throw new Exception("Failed to get the KeyAttribute of the entity"));


                // The return type is directly supported by the wayback 
                // machine. This means its a direct type, as in the property is a
                // single navigational property
                if (_wayback.SupportsType(returnType)) {

                    // Get the entity type and the navigation property
                    var entityType = _wayback._dbcontext.Model.FindEntityType(returnType) 
                        ?? throw new Exception($"Cannot get entity type of {returnType}");
                    var nav_property = returnType.GetProperty(String.Join(String.Empty, invocation.Method.Name.Skip(4))) 
                        ?? throw new Exception($"Failed to get property for {String.Join(String.Empty, invocation.Method.Name.Skip(4))}");

                    // Get the tablename, column name and the invoation result based on the
                    // EFCore entity
                    var table_name = entityType.GetTableName() ?? string.Empty;
                    var columnName = entityType.FindNavigation(nav_property.Name)?.ForeignKey.Properties.First().Name;
                    var invocation_result = invocation.Method.Invoke(_target, invocation.Arguments);

                    // Get the ID from the last change update
                    var revertpoint_old_key = _wayback._dbcontext.AuditEntries
                        .Where(s =>
                            s.EntityID == entity_id &&
                            s.TableName == table_name &&
                            s.PropertyName == columnName
                        )
                        .OrderByDescending(s => s.ChangeDate)
                        .FirstOrDefault(s => s.ChangeDate <= _wayback._revertPoint);

                    // If there is not no audit record,
                    // then assume there has been no changes to object
                    // and just return the invocation result
                    // But before that, be sure to return a proxy if the invocation
                    // result is not null
                    if (revertpoint_old_key == null) {
                        if (invocation_result == null) {
                            invocation.ReturnValue = null;
                            ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
                            return;
                        }
                        invocation.ReturnValue = _wayback.GenerateEntity(invocation_result, returnType);
                        ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
                        return;
                    }

                    // If the auditlogs new value is null, set that as
                    // the invocation result
                    if (revertpoint_old_key.NewValue == null) {
                        invocation.ReturnValue = null;
                        ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
                        return;
                    }

                    // Else just set the return value of the invocation to the
                    // proxied EFCore entity that was fetched based on the 
                    // tablename and the new value audit change
                    invocation.ReturnValue =
                        _wayback.GenerateEntity(_wayback._dbcontext.FindEntity(table_name, Int32.Parse(revertpoint_old_key.NewValue)), returnType);
                    ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
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
                                        returnList.Remove(_wayback.GenerateEntity(genericType, Int32.Parse(auditEntry.NewValue ?? "-1")));
                                        break;

                                    case AuditEntryType.CollectionRemove:
                                        returnList.Add(_wayback.GenerateEntity(genericType, Int32.Parse(auditEntry.NewValue ?? "-1")));
                                        break;
                                }
                            }
                            invocation.ReturnValue = returnList;
                            ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
                            return;
                        }
                    }
                }
            }
            invocation.ReturnValue = invocation.Method.Invoke(_target, invocation.Arguments);
        }


    }

    public static class MyExtensions {
        public static IQueryable<object> TypeSet(this DbContext _context, Type t) {
            return (IQueryable<object>)_context.GetType().GetMethod("Set", types: Type.EmptyTypes).MakeGenericMethod(t).Invoke(_context, null);
        }
    }
}
