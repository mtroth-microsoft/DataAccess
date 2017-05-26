// -----------------------------------------------------------------------
// <copyright file="DatasourceWrapper.cs" company="Lensgrinder">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Net.Http;
    using System.Web.OData;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Wrapper class for vanilla DbContext or ObjectContext datasources.
    /// </summary>
    public class DatasourceWrapper : IDatasource
    {
        /// <summary>
        /// Internal model storage.
        /// </summary>
        private IEdmModel model;

        /// <summary>
        /// The original data source being wrapped.
        /// </summary>
        private object original;

        /// <summary>
        /// Initializes an instance of the DatasourceWrapper class.
        /// </summary>
        /// <param name="context">The dbcontext to wrap.</param>
        public DatasourceWrapper(object context)
        {
            this.original = context;
            this.model = ConfigurationHelper.BuildModel(
                this.ReadObjectContext(),
                context.GetType(),
                null);
        }

        /// <summary>
        /// The Type of the model.
        /// </summary>
        public virtual Type ModelType
        {
            get
            {
                return this.original.GetType();
            }
        }

        /// <summary>
        /// Gets the current transaction, null if not applicable.
        /// </summary>
        public virtual ITransaction CurrentTransaction
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the queryable set of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the queryable set.</typeparam>
        /// <param name="includes">The list of includes, if applicable.</param>
        /// <returns>The queryable set of data.</returns>
        public virtual IQueryable<T> Get<T>(params string[] includes)
            where T : class
        {
            ObjectContext obc = this.ReadObjectContext();
            ObjectQuery<T> result = obc.CreateObjectSet<T>();
            foreach (string item in includes)
            {
                result = result.Include(item);
            }

            return result;
        }

        /// <summary>
        /// Returns the single entity of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entityKey">The entity key.</param>
        /// <returns>The entity.</returns>
        public virtual T GetByKey<T>(InfrastructureKey entityKey)
            where T : class
        {
            EntityKey key = new EntityKey(entityKey.QualifiedEntitySetName, entityKey.EntityKeyValues);
            ObjectContext obc = this.ReadObjectContext();
            T result = obc.GetObjectByKey(key) as T;

            return result;
        }

        /// <summary>
        /// Creates the key for the given entity.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>The created key.</returns>
        public virtual InfrastructureKey CreateKey(string entitySetName, object entity)
        {
            ObjectContext obc = this.ReadObjectContext();
            EntityKey key = obc.CreateEntityKey(entitySetName, entity);
            Dictionary<string, object> keys = new Dictionary<string, object>();
            foreach (EntityKeyMember member in key.EntityKeyValues)
            {
                keys[member.Key] = member.Value;
            }

            return new InfrastructureKey(entitySetName, keys);
        }

        /// <summary>
        /// Post an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The added entity.</returns>
        public virtual T Post<T>(WriteRequest request)
            where T : class
        {
            if (request == null || request.Entity == null)
            {
                throw new ArgumentNullException("request");
            }

            ObjectContext obc = this.ReadObjectContext();
            InfrastructureDelta<T> delta = new InfrastructureDelta<T>();
            EdmEntityObject eeo = request.Entity as EdmEntityObject;
            IEnumerable<string> names = eeo.GetChangedPropertyNames();
            foreach (string name in names)
            {
                object value;
                request.Entity.TryGetPropertyValue(name, out value);
                delta.TrySetPropertyValue(name, value);
            }

            T entityToCreate = delta.GetInstance();

            obc.AddObject(request.EntitySetName, entityToCreate);
            if (this.CurrentTransaction == null)
            {
                obc.SaveChanges();
            }

            return entityToCreate;
        }

        /// <summary>
        /// Put an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The updated entity.</returns>
        public virtual T Put<T>(WriteRequest request)
            where T : class
        {
            if (request == null || request.Entity == null)
            {
                throw new ArgumentNullException("request");
            }

            return this.Upsert<T>(request);
        }

        /// <summary>
        /// Patch an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The patched entity.</returns>
        public virtual T Patch<T>(WriteRequest request)
            where T : class
        {
            if (request == null || request.Entity == null)
            {
                throw new ArgumentNullException("request");
            }

            return this.Upsert<T>(request);
        }

        /// <summary>
        /// Delete an instance from the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        public virtual void Delete<T>(WriteRequest request)
            where T : class
        {
            ObjectContext obc = this.ReadObjectContext();
            T originalEntity = this.GetByKey<T>(request.Key);
            obc.DeleteObject(originalEntity);
            if (this.CurrentTransaction == null)
            {
                obc.SaveChanges();
            }

            return;
        }

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the return entity.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        public virtual IQueryable<T> Execute<T>(ParsedOperation operation)
        {
            ObjectContext obc = this.ReadObjectContext();
            
            return obc.ExecuteFunction<T>(operation.Name, operation.Parameters.ToArray()).AsQueryable<T>();
        }

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        public virtual int Execute(ParsedOperation operation)
        {
            ObjectContext obc = this.ReadObjectContext();
            
            return obc.ExecuteFunction(operation.Name, operation.Parameters.ToArray());
        }

        /// <summary>
        /// Save any pending changes.
        /// </summary>
        /// <returns>Count of changes made.</returns>
        public virtual int SaveChanges()
        {
            ObjectContext context = this.ReadObjectContext();
            int result = context.SaveChanges();
            this.CurrentTransaction = null;

            return result;
        }

        /// <summary>
        /// Returns a transaction against this datasource.
        /// </summary>
        /// <returns></returns>
        public virtual ITransaction BeginTransaction()
        {
            this.CurrentTransaction = ConfigurationHelper.CreateTransaction(this);
            return this.CurrentTransaction;
        }

        /// <summary>
        /// Get the model for the datasource.
        /// </summary>
        /// <returns>The model.</returns>
        public virtual IEdmModel GetModel()
        {
            return this.model;
        }

        /// <summary>
        /// Get the list of controller types.
        /// </summary>
        /// <returns>The list of controller types, if the call returns null, 
        /// all controllers from the ModelType's assembly will be placed in scope.</returns>
        public virtual IEnumerable<Type> GetControllerTypes()
        {
            return null;
        }

        /// <summary>
        /// Get the configuration for the service.
        /// </summary>
        /// <returns></returns>
        public virtual InfrastructureConfigType GetConfig()
        {
            InfrastructureConfigType config = ConfigurationHelper.CreateDefaultConfig();
            config.Access.WriteEnabled = true;

            return config;
        }

        /// <summary>
        /// Tries to get a single entity of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entityKey">The entity key.</param>
        /// <param name="value">The entity.</param>
        /// <returns>True if the entity was found, otherwise false.</returns>
        protected virtual bool TryGetByKey<T>(InfrastructureKey entityKey, out T value)
            where T : class
        {
            ObjectContext obc = this.ReadObjectContext();
            EntityKey key = new EntityKey(entityKey.QualifiedEntitySetName, entityKey.EntityKeyValues);

            object inner;
            bool result = obc.TryGetObjectByKey(key, out inner);
            value = inner as T;

            return result;
        }

        /// <summary>
        /// Use logic to determine update/insert of request.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The upserted entity.</returns>
        private T Upsert<T>(WriteRequest request)
            where T : class
        {
            bool ifmatch = false, ifnonematch = false;
            if (request != null)
            {
                ifmatch = request.Request.Headers.IfMatch != null && request.Request.Headers.IfMatch.Count > 0;
                ifnonematch = request.Request.Headers.IfNoneMatch != null && request.Request.Headers.IfNoneMatch.Count > 0;
            }

            T originalEntity = this.GetByKey<T>(request.Key);
            if (ifnonematch || (this.TryGetByKey<T>(request.Key, out originalEntity) == false && ifmatch == false))
            {
                EdmEntityObject eeo = request.Entity as EdmEntityObject;
                foreach (KeyValuePair<string, object> pair in request.Key.EntityKeyValues)
                {
                    eeo.TrySetPropertyValue(pair.Key, pair.Value);
                }

                return this.Post<T>(request);
            }
            else
            {
                return this.Update<T>(request, originalEntity);
            }
        }

        /// <summary>
        /// Use logic to determine update/insert of request.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <param name="originalEntity">The original entity to update.</param>>
        /// <returns>The upserted entity.</returns>
        private T Update<T>(WriteRequest request, T originalEntity)
            where T : class
        {
            ObjectContext obc = this.ReadObjectContext();
            InfrastructureDelta<T> delta = new InfrastructureDelta<T>();
            EdmEntityObject eeo = request.Entity as EdmEntityObject;
            IEnumerable<string> names = eeo.GetChangedPropertyNames();
            foreach (string name in names)
            {
                object value;
                request.Entity.TryGetPropertyValue(name, out value);
                delta.TrySetPropertyValue(name, value);
            }

            foreach (KeyValuePair<string, object> member in request.Key.EntityKeyValues)
            {
                delta.TrySetPropertyValue(member.Key, member.Value);
            }

            obc.AttachTo(request.EntitySetName, originalEntity);
            if (request.Request.Method == HttpMethod.Put)
            {
                delta.Put(originalEntity);
            }
            else
            {
                delta.Patch(originalEntity);
            }

            if (this.CurrentTransaction == null)
            {
                obc.SaveChanges();
            }

            return originalEntity;
        }

        /// <summary>
        /// Read the object context from the original datasource.
        /// </summary>
        /// <returns>The object context.</returns>
        private ObjectContext ReadObjectContext()
        {
            ObjectContext obc = this.original as ObjectContext;
            if (obc == null)
            {
                DbContext dbc = this.original as DbContext;
                if (dbc != null)
                {
                    IObjectContextAdapter adapter = dbc as IObjectContextAdapter;
                    obc = adapter.ObjectContext;
                }
            }

            return obc;
        }
    }
}
