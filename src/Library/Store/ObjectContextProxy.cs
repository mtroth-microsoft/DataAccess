// -----------------------------------------------------------------------
// <copyright file="ObjectContextProxy.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    using System.Net.Http;
    using System.Web.OData;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Common base class Datasource over ObjectContext.
    /// </summary>
    public abstract class ObjectContextProxy : ObjectContext, IDatasource
    {
        /// <summary>
        /// Initializes a new instance of the ObjectContextProxy class.
        /// </summary>
        /// <param name="connection">The entity connection.</param>
        protected ObjectContextProxy(EntityConnection connection)
            : base(connection)
        {
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
        /// Gets the type of the model.
        /// </summary>
        public virtual Type ModelType
        {
            get
            {
                return this.GetType();
            }
        }

        /// <summary>
        /// Gets the model definition.
        /// </summary>
        /// <returns>The model output.</returns>
        public virtual IEdmModel GetModel()
        {
            return ConfigurationHelper.BuildModel(this, this.GetType(), null);
        }

        /// <summary>
        /// Get the list of controller types.
        /// </summary>
        /// <returns>The list of controller types.</returns>
        public virtual IEnumerable<Type> GetControllerTypes()
        {
            return null;
        }

        /// <summary>
        /// Get the configuration for the service.
        /// </summary>
        /// <returns>The configuration to use.</returns>
        public virtual InfrastructureConfigType GetConfig()
        {
            return null;
        }

        /// <summary>
        /// Returns the entity set data for a given type.
        /// </summary>
        /// <typeparam name="T">The type of the entity set.</typeparam>
        /// <param name="includes">Navigational properties to include.</param>
        /// <returns>The list of corresponding instances.</returns>
        public virtual IQueryable<T> Get<T>(params string[] includes)
            where T : class
        {
            IQueryable<T> result = this.CreateObjectSet<T>();
            foreach (string include in includes)
            {
                result = result.Include(include);
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
            T value;
            if (this.TryGetByKey<T>(entityKey, out value) == false)
            {
                throw new ObjectNotFoundException(entityKey.QualifiedEntitySetName);
            }

            return value;
        }

        /// <summary>
        /// Creates the key for the given entity.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>The created key.</returns>
        public virtual InfrastructureKey CreateKey(string entitySetName, object entity)
        {
            EntityKey key = this.CreateEntityKey(entitySetName, entity);
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

            this.AddObject(request.EntitySetName, entityToCreate);
            if (this.CurrentTransaction == null)
            {
                this.SaveChanges();
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
                throw new ArgumentNullException("entity");
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
            T originalEntity = this.GetByKey<T>(request.Key);
            this.DeleteObject(originalEntity);
            if (this.CurrentTransaction == null)
            {
                this.SaveChanges();
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
            return this.ExecuteFunction<T>(operation.Name, operation.Parameters.ToArray()).AsQueryable<T>();
        }

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        public virtual int Execute(ParsedOperation operation)
        {
            return this.ExecuteFunction(operation.Name, operation.Parameters.ToArray());
        }

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>The transaction.</returns>
        public virtual ITransaction BeginTransaction()
        {
            this.CurrentTransaction = ConfigurationHelper.CreateTransaction(this);

            return this.CurrentTransaction;
        }

        /// <summary>
        /// Save any pending changes.
        /// </summary>
        /// <returns>Count of changes made.</returns>
        public override int SaveChanges()
        {
            int result = base.SaveChanges();
            this.CurrentTransaction = null;

            return result;
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
            bool result = false;
            EntityKey key = new EntityKey(entityKey.QualifiedEntitySetName, entityKey.EntityKeyValues);

            ObjectStateEntry entry;
            if (this.ObjectStateManager.TryGetObjectStateEntry(key, out entry) == true &&
                entry.State == EntityState.Deleted)
            {
                value = null;
            }
            else
            {
                object inner;
                result = this.TryGetObjectByKey(key, out inner);
                value = inner as T;
            }

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

            T originalEntity;
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
        /// <param name="originalEntity">The original entity to update.</param>
        /// <returns>The upserted entity.</returns>
        private T Update<T>(WriteRequest request, T originalEntity)
            where T : class
        {
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

            this.AttachTo(request.EntitySetName, originalEntity);
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
                this.SaveChanges();
            }

            return originalEntity;
        }
    }
}
