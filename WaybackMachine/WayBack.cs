using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using WaybackMachine;
using WaybackMachine.Entities;
using WaybackMachine.FilterAttributes;

namespace WaybackMachine {
    public class WayBack {

        internal IWaybackContext _dbcontext;
        internal WaybackDbContext _trackingDbContext;
        internal ProxyGenerator _generator;
        internal DateTime _revertPoint;
        private WayBack(IWaybackContext dbcontext, DateTime revertPoint) {
            _dbcontext = dbcontext;
            _revertPoint = revertPoint;
            _generator = new ProxyGenerator();
            _trackingDbContext = new WaybackDbContext();
            _trackingDbContext.Database.EnsureCreated();
        }

        public static WayBack CreateWayBack(IWaybackContext dbcontext, DateTime revertPoint) {
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
            object? returnObj = null;
            if (_entityCacheProxies.TryGetValue(_target, out returnObj))
                return returnObj;

            returnObj = _entityCacheProxies.Values.FirstOrDefault(x => x == _target);
            if (returnObj != null) return returnObj;

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
            object? _returnVal = _dbcontext.InternalDbContext.FindSingleOrDefault(tablename, id);
            object? cacheCheck = null;
            if (_entityCacheProxies.TryGetValue(_returnVal, out cacheCheck))
                return cacheCheck;

            var _type = _trackingDbContext.GetTypeFromTableName(_trackingDbContext, tablename);
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
            object? _returnVal = _dbcontext.InternalDbContext.FindSingleOrDefault(type.Name, id);

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
                            .First(s => s.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() != null)
                            .GetValue(_reference)
                ?? throw new Exception("Failed to get the KeyAttribute of the entity"));

            // Get the table name of the entity
            var tableName = targetBaseType.GetBase().Name; //_dbcontext.InternalDbContext.GetTableNameFromType(targetBaseType);

            // Get the change history for the entity
            var auditLogs = _trackingDbContext.AuditEntries
                .Where(s =>
                    s.EntityID == entityID &&
                    s.Table.Name == tableName &&
                    s.ParentTransaction.ChangeDate >= _revertPoint &&
                    s.ChangeType == AuditEntryType.PropertyOrReferenceChange)
                .OrderByDescending(s => s.ParentTransaction.ChangeDate);


            foreach (var property in targetBaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if ((property.SetMethod?.IsVirtual ?? true) || (property.GetMethod?.IsVirtual ?? true)) continue;
                property.SetValue(_target, property.GetValue(_reference));
            }

            // Iterate over the audit logs and revert the changes
            foreach (var al in auditLogs.ToList()) {

                // Get the property based on the auditlog PropertyName
                // if it returns null, then continue
                var property = targetBaseType.GetProperty(al.Property.Name);
                if (property == null) continue;
                if ((property.SetMethod?.IsVirtual ?? true) || (property.GetMethod?.IsVirtual ?? true)) continue;

                // Try to get the old value
                // and if its not a string, then try to invoke the
                // parse method. That will work for most properties
                object? value = al.OldValue ?? "";
                if (property.PropertyType != typeof(string)) {
                    value = TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString((string)value);
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

        private PropertyInfo? PrimaryKeyProperty = null;

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


                var propertyName = invocation.Method.Name.Substring(4, invocation.Method.Name.Length - 4); //String.Join(String.Empty, invocation.Method.Name.Skip(4));
                var returnType = invocation.Method.ReturnParameter.ParameterType;
                if (PrimaryKeyProperty == null)
                        PrimaryKeyProperty = _target.GetType()
                                     .GetProperties()
                                     .First(s => s.GetCustomAttributes(false).Any(s => s.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)));
             

                var entity_id = (int)(PrimaryKeyProperty
                          .GetValue(_target) ?? throw new Exception("Failed to get the KeyAttribute of the entity"));

                var entityType = _target.GetType().BaseType
                    ?? throw new Exception($"Cannot get the base type for `{_target.GetType().FullName}`");

                var efCoreEntityType = _wayback._dbcontext.InternalDbContext.Model.FindEntityType(entityType)
                    ?? throw new Exception($"Cannot get entity type of `{entityType.FullName}`");

                IProperty targetForeignKey = null;
                var IsJunction = false;


                var efNavProperty = efCoreEntityType.FindNavigation(propertyName);

                if (efNavProperty != null) {
                    targetForeignKey = efNavProperty.ForeignKey.Properties.First();
                } else {
                    IsJunction = true;
                    var efNavSkipProperty = efCoreEntityType.FindSkipNavigation(propertyName)
                        ?? throw new Exception($"Cannot get the EFNavProperty (Skip) {propertyName} in type `{efCoreEntityType.Name}");
                    targetForeignKey = efNavSkipProperty.ForeignKey.Properties.First();
                }

                // The return type is directly supported by the wayback 
                // machine. This means its a direct type, as in the property is a
                // single navigational property
                if (_wayback.SupportsType(returnType)) {

                    var sourceTableName = entityType.GetBase().Name; //_wayback._dbcontext.InternalDbContext.GetTableNameFromType(entityType)
                                                           //?? throw new Exception($"Failed to get the table name for type `{entityType.FullName}`");

                    var targetTableName = returnType.GetBase().Name; //_wayback._dbcontext.InternalDbContext.GetTableNameFromType(returnType)
                                                           //?? throw new Exception($"Failed to get the table name for type `{returnType.FullName}`");

                    var invocation_result = invocation.Method.Invoke(_target, invocation.Arguments);

                    var latestUpdate = _wayback._trackingDbContext.AuditEntries
                        .Where(s =>
                            s.Property.Name == targetForeignKey.Name &&
                            s.Table.Name == sourceTableName &&
                            s.EntityID == entity_id &&
                            s.ParentTransaction.ChangeDate >= _wayback._revertPoint
                        )
                        .OrderByDescending(s => s.ParentTransaction.ChangeDate)
                        .FirstOrDefault();

                    if (latestUpdate == null || latestUpdate?.OldValue == null) {
                        invocation.ReturnValue = invocation_result;
                        ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
                        return;
                    }


                    var newResult = _wayback.GenerateEntity(targetTableName, Int32.Parse(latestUpdate.OldValue));
                    invocation.ReturnValue = newResult;
                    ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
                    return;



                }

                // Check if the methods return type has generic
                // type arguments, This might be a sign that its a
                // collection
                if (returnType.GenericTypeArguments.Any()) {




                    var genericType = returnType.GenericTypeArguments.First();
                    // Check if the generic type is supported by the
                    // wayback machine
                    if (_wayback.SupportsType(genericType)) {

                        // Check if its an object that supports a list
                        var castType = typeof(List<>).MakeGenericType(genericType);

                        if (!IsJunction) {
                            if (castType.IsAssignableFrom(returnType)) {


                                var targetTableName = genericType.Name;//_wayback._dbcontext.InternalDbContext.GetTableNameFromType(genericType)
                                                                       //?? throw new Exception($"Failed to get the table name for type `{genericType.FullName}`");




                                // Get the actual result from the EFCore object
                                // and if its null then also return null
                                var invocationResult = ((IList?)invocation.Method.Invoke(_target, invocation.Arguments)).Cast<object>().ToList();
                                if (invocationResult == null) {
                                    invocationResult = new List<object>();
                                }

                                // Get the .GenerateEntityGeneric(object, Type, DateTime) method from the
                                // wayback context and store it in a variable so we can invoke it in the loop
                                // Oh and make it a generic method and apply the generic type 

                                var returnList = (IList)(Activator.CreateInstance(castType)
                                    ?? throw new Exception($"Failed to create instance of List<{genericType.FullName}>"));

                                // Loop over the invocation results
                                // and create proxies and add them to the list

                                var targetAuditEntries = _wayback._trackingDbContext.AuditEntries.Where(s =>
                                   s.Table.Name == targetTableName &&
                                   s.Property.Name == targetForeignKey.Name &&
                                   s.ParentTransaction.ChangeDate >= _wayback._revertPoint &&
                                   (s.OldValue == entity_id.ToString() || s.NewValue == entity_id.ToString())
                                ).OrderBy(s => s.ParentTransaction.ChangeDate);






                                var createdEntities = _wayback._trackingDbContext.AuditEntries.Where(s =>
                                    s.Table.Name == targetTableName &&
                                    s.ChangeType == AuditEntryType.Created &&
                                    s.ParentTransaction.ChangeDate > _wayback._revertPoint
                                )
                                    .Select(x => x.EntityID)
                                    .ToList();

                                var addedEntities = targetAuditEntries
                                    .Where(x => x.OldValue == null)
                                    .Select(x => x.EntityID)
                                    .ToList();

                                var containsMethod = typeof(List<int>)
                                    .GetMethod("Contains")
                                        ?? throw new Exception("Failed to get the contains method");

                                var filterQueryParam = Expression.Parameter(typeof(object));
                                var filterQuery = (Func<object, bool>)Expression.Lambda(
                                        Expression.OrElse(
                                            Expression.Call(
                                                Expression.Constant(addedEntities),
                                                containsMethod,
                                                    Expression.Property(
                                                        Expression.TypeAs(filterQueryParam, genericType), genericType.GetPrimaryKeyField().Name)
                                            ),
                                            Expression.Call(
                                                Expression.Constant(createdEntities),
                                                containsMethod,
                                                    Expression.Property(
                                                        Expression.TypeAs(filterQueryParam, genericType), genericType.GetPrimaryKeyField().Name)
                                            )
                                        ),
                                        filterQueryParam).Compile();



                                invocationResult.RemoveAll(x => filterQuery(x));
                                foreach (object obj in invocationResult) {
                                    returnList.Add(_wayback.GenerateEntity(obj, genericType));
                                }

                                foreach (var auditEntry in targetAuditEntries.Where(s => s.OldValue == entity_id.ToString()).ToList()) {
                                    returnList.Add(_wayback.GenerateEntity(targetTableName, auditEntry.EntityID));
                                }


                                var softDeleteProperty = genericType.GetCustomAttribute(typeof(SoftDelete)) != null ?
                                    (genericType.GetProperty("DeleteDate") ?? throw new InvalidProgramException("SoftDelete decalared but no DeleteDate")) :
                                    null;

                                if (softDeleteProperty != null) {

                                    var inParam = Expression.Parameter(genericType);
                                    var expression = Expression.Lambda(
                                        Expression.AndAlso(
                                            Expression.LessThan(
                                                Expression.Constant(_wayback._revertPoint, typeof(DateTime?)),
                                                Expression.PropertyOrField(inParam, "DeleteDate")
                                            ),
                                            Expression.Equal(
                                                Expression.Constant(entity_id),
                                                Expression.Call(
                                                    (
                                                        typeof(EF).GetMethod("Property") ?? throw new Exception("")
                                                    ).MakeGenericMethod(typeof(int)),
                                                    inParam,
                                                    Expression.Constant(targetForeignKey.Name, typeof(string))
                                                )
                                            )
                                        ),
                                        inParam
                                    );

                                    IList deletedSet = ((IQueryable<dynamic>)_wayback._dbcontext.InternalDbContext.FindWhere(genericType.GetBase().Name, expression)).ToList();
                                    foreach (var deletedRecord in deletedSet) {
                                        returnList.Add(_wayback.GenerateEntity(deletedRecord, genericType));
                                    }
                                }


                                invocation.ReturnValue = returnList;
                                ReadResultCacheDictionary.Add(invocation.Method.Name, invocation.ReturnValue);
                                return;
                            }

                        } else {

                            var junctionTable = targetForeignKey.DeclaringEntityType.ClrType.Name;
                            var srcTable = entityType.GetTableEnitity(_wayback._trackingDbContext);
                            var destTable = genericType.GetTableEnitity(_wayback._trackingDbContext);

                            var targetAuditEntries = _wayback._trackingDbContext.AuditEntries
                                .Where(s =>
                                    s.Table.Name == junctionTable &&
                                    s.ParentTransaction.ChangeDate >= _wayback._revertPoint &&
                                    (
                                        (s.J1Table.ID == srcTable.ID && s.J1 == entity_id) ||
                                        (s.J2Table.ID == srcTable.ID && s.J2 == entity_id)
                                    ))
                                .OrderByDescending(s => s.ParentTransaction.ChangeDate);

                            var invocationResult = (IList?)invocation.Method.Invoke(_target, invocation.Arguments);
                            if (invocationResult == null) {
                                invocationResult = (IList)(Activator.CreateInstance(castType)
                                    ?? throw new Exception($"Failed to create instance of List<{genericType.FullName}>"));
                            }

                            var returnList = (IList)(Activator.CreateInstance(castType)
                                    ?? throw new Exception($"Failed to create instance of List<{genericType.FullName}>"));

                            // Loop over the invocation results
                            // and create proxies and add them to the list
                            foreach (object obj in invocationResult) {
                                if (targetAuditEntries.Any(s =>
                                    (s.J2 == entity_id && s.J1 == obj.GetPrimaryKeyValue() && s.J1Table == destTable && s.ChangeType == AuditEntryType.CollectionAdd) ||
                                    (s.J1 == entity_id && s.J2 == obj.GetPrimaryKeyValue() && s.J2Table == destTable && s.ChangeType == AuditEntryType.CollectionAdd)
                                )) continue;
                                returnList.Add(_wayback.GenerateEntity(obj, genericType));
                            }


                            var removedEntries = targetAuditEntries.Where(s =>
                                    (s.J1 == entity_id && s.J1Table.ID == srcTable.ID && s.ChangeType == AuditEntryType.CollectionRemove) ||
                                    (s.J2 == entity_id && s.J2Table.ID == srcTable.ID && s.ChangeType == AuditEntryType.CollectionRemove)
                                ).ToList();

                            foreach (var auditEntry in removedEntries)
                                returnList.Add(_wayback.GenerateEntity(destTable.Name, auditEntry.GetJunctionKeyForTable(destTable.Name)));

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


}
