// -----------------------------------------------------------------------
// <copyright file="MultipleObjectContextProxy.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Base class for multiple context operations.
    /// </summary>
    public abstract class MultipleObjectContextProxy<ProxyType> : IDatasource
        where ProxyType : ObjectContextProxy
    {
        /// <summary>
        /// List of actual proxies wrapped by this context.
        /// </summary>
        private List<ObjectContextProxy> proxies = new List<ObjectContextProxy>();

        /// <summary>
        /// Initializes a new instance of the MultipleDbContextProxy class.
        /// </summary>
        /// <param name="context">The data context.</param>
        protected MultipleObjectContextProxy(IDataContext context)
        {
            foreach (string connection in context.Store.GetConnectionStrings())
            {
                object instance = Activator.CreateInstance(typeof(ProxyType), connection);
                ObjectContextProxy proxy = instance as ObjectContextProxy;
                this.proxies.Add(proxy);
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
        /// Gets the type of the model.
        /// </summary>
        public virtual Type ModelType
        {
            get
            {
                return typeof(ProxyType);
            }
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
            InfrastructureQueryable<T> results = new InfrastructureQueryable<T>(false);
            foreach (ObjectContextProxy proxy in this.proxies)
            {
                ObjectSet<T> result = proxy.CreateObjectSet<T>();
                results.Add(result);
            }

            return results;
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
            T instance = null;
            foreach (ObjectContextProxy proxy in this.proxies)
            {
                object value;
                if (proxy.TryGetObjectByKey(key, out value) == true)
                {
                    instance = value as T;
                    break;
                }
            }

            return instance;
        }

        /// <summary>
        /// Creates the key for the given entity.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>The created key.</returns>
        public virtual InfrastructureKey CreateKey(
            string entitySetName, 
            object entity)
        {
            IEnumerable<Type> types = this.proxies.Select(p => p.ModelType).Distinct();
            if (types.Count() > 1)
            {
                throw new InvalidOperationException("All instances of context must be of same type.");
            }

            EntityKey key = this.proxies.First().CreateEntityKey(entitySetName, entity);
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete an instance from the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        public virtual void Delete<T>(WriteRequest request)
            where T : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the return entity.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        public virtual IQueryable<T> Execute<T>(ParsedOperation operation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        public virtual int Execute(ParsedOperation operation)
        {
            throw new NotImplementedException();
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
        public virtual int SaveChanges()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the model definition.
        /// </summary>
        /// <returns>The model output.</returns>
        public virtual IEdmModel GetModel()
        {
            IEnumerable<Type> types = this.proxies.Select(p => p.ModelType).Distinct();
            if (types.Count() > 1)
            {
                throw new InvalidOperationException("All instances of context must be of same type.");
            }

            return ConfigurationHelper.BuildModel(this.proxies.First(), this.ModelType, null);
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
    }
}
