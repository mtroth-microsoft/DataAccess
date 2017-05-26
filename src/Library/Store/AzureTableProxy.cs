// -----------------------------------------------------------------------
// <copyright file="AzureTableProxy.cs" company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Web.OData;
    using Microsoft.OData.Edm;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Common base class Datasource over Azure tables.
    /// </summary>
    public abstract class AzureTableProxy : IDatasource
    {
        /// <summary>
        /// The account to use.
        /// </summary>
        private string accountName;

        /// <summary>
        /// The account key to use.
        /// </summary>
        private string accountKey;

        /// <summary>
        /// Initializes a new instance of the AzureTableProxy class.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <param name="accountKey">The account key.</param>
        protected AzureTableProxy(string accountName, string accountKey)
        {
            this.accountKey = accountKey;
            this.accountName = accountName;
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
            throw new NotImplementedException();
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
            Type implements = typeof(T).GetInterface(typeof(ITableEntity).FullName);
            if (implements == null)
            {
                throw new ArgumentException("The EntitySet is invalid because its EntityType does not implement ITableEntity.");
            }

            TableQuery<T> inventoryQuery = new TableQuery<T>();
            CloudTable table = this.GetTable(this.LookupTableName(typeof(T)));

            string partition = this.LookupPartitionName(typeof(T));
            if (string.IsNullOrEmpty(partition) == false)
            {
                string filter = TableQuery.GenerateFilterCondition(
                     "PartitionKey",
                     QueryComparisons.Equal,
                     partition);
                inventoryQuery = inventoryQuery.Where(filter);
            }

            MethodInfo method = this.GetType().GetMethod("Query", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo generic = method.MakeGenericMethod(typeof(T));
            IQueryable<T> results = generic.Invoke(this, new object[] { inventoryQuery, table }) as IQueryable<T>;

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
            Type implements = typeof(T).GetInterface(typeof(ITableEntity).FullName);
            if (implements == null)
            {
                throw new ArgumentException("The EntitySet is invalid because its EntityType does not implement ITableEntity.");
            }

            MethodInfo method = this.GetType().GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo generic = method.MakeGenericMethod(typeof(T));
            T result = generic.Invoke(this, new object[] { entityKey }) as T;

            return result;
        }

        /// <summary>
        /// Creates the key for the given entity.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>The created key.</returns>
        public abstract InfrastructureKey CreateKey(string entitySetName, object entity);

        /// <summary>
        /// Post an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The added entity.</returns>
        public virtual T Post<T>(WriteRequest request)
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

            ITableEntity entityToCreate = delta.GetInstance() as ITableEntity;
            InfrastructureKey infraKey = this.CreateKey(request.EntitySetName, entityToCreate);
            entityToCreate.PartitionKey = this.LookupPartitionName(typeof(T));
            entityToCreate.RowKey = this.ConvertToRowKey(infraKey, typeof(T));
            entityToCreate.Timestamp = DateTimeOffset.UtcNow;

            CloudTable table = this.GetTable(this.LookupTableName(typeof(T)));
            TableOperation insertOperation = TableOperation.Insert(entityToCreate);
            TableResult result = table.Execute(insertOperation);

            return result.Result as T;
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
            TableEntity originalEntity = this.GetByKey<T>(request.Key) as TableEntity;
            CloudTable table = this.GetTable(this.LookupTableName(typeof(T)));
            TableOperation deleteOperation = TableOperation.Delete(originalEntity);
            TableResult result = table.Execute(deleteOperation);
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
            return -1;
        }

        /// <summary>
        /// Helper method to cast unconditioned to generic to azure conditioned generic.
        /// </summary>
        /// <typeparam name="T">The type of the table entity.</typeparam>
        /// <param name="inventoryQuery">The table query.</param>
        /// <param name="table">The table.</param>
        /// <returns>The queryable result.</returns>
        protected T Read<T>(InfrastructureKey entityKey)
            where T : class, ITableEntity, new()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(
                this.LookupPartitionName(typeof(T)),
                this.ConvertToRowKey(entityKey, typeof(T)));

            CloudTable table = this.GetTable(this.LookupTableName(typeof(T)));
            TableResult query = table.Execute(retrieveOperation);

            return query.Result as T;
        }

        /// <summary>
        /// Helper method to cast unconditioned to generic to azure conditioned generic.
        /// </summary>
        /// <typeparam name="T">The type of the table entity.</typeparam>
        /// <param name="inventoryQuery">The table query.</param>
        /// <param name="table">The table.</param>
        /// <returns>The queryable result.</returns>
        protected IQueryable<T> Query<T>(TableQuery<T> inventoryQuery, CloudTable table)
            where T : ITableEntity, new()
        {
            IEnumerable<T> inventory = table.ExecuteQuery<T>(inventoryQuery);

            return inventory.AsQueryable();
        }

        /// <summary>
        /// Convert the provided entity key to a azure row key for the given type.
        /// </summary>
        /// <param name="entityKey">The entity key.</param>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>The serialized row key.</returns>
        protected virtual string ConvertToRowKey(InfrastructureKey entityKey, Type type)
        {
            return string.Join(",", entityKey.EntityKeyValues.Select(p => p.Value.ToString()).ToArray());
        }

        /// <summary>
        /// Lookup a partition name for the given type.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>The partition name to use.</returns>
        protected virtual string LookupPartitionName(Type type)
        {
            return type.FullName;
        }

        /// <summary>
        /// Lookup a table name for the given type.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>The name of the azure table to use.</returns>
        protected virtual string LookupTableName(Type type)
        {
            return type.Name;
        }

        /// <summary>
        /// Get a client to read from a table.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <param name="accountKey">The account key.</param>
        /// <returns></returns>
        private static CloudTableClient GetCloudTableClient(string accountName, string accountKey)
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            CloudTableClient client = account.CreateCloudTableClient();
            return client;
        }

        /// <summary>
        /// Get a table of a given name.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <returns>The corresponding azure table.</returns>
        private CloudTable GetTable(string name)
        {
            CloudTableClient client = GetCloudTableClient(this.accountName, this.accountKey);
            CloudTable table = client.GetTableReference(name);
            table.CreateIfNotExists();

            return table;
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

            ITableEntity entityToModify = delta.GetInstance() as ITableEntity;
            entityToModify.Timestamp = DateTimeOffset.UtcNow;
            CloudTable table = this.GetTable(this.LookupTableName(typeof(T)));

            TableOperation operation = null;
            if (request.Request.Method == HttpMethod.Put)
            {
                operation = TableOperation.Replace(entityToModify);
            }
            else
            {
                operation = TableOperation.Merge(entityToModify);
            }

            TableResult result = table.Execute(operation);

            return result.Result as T;
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
            value = null;
            try
            {
                value = this.GetByKey<T>(entityKey);
            }
            catch 
            { 
            }

            return value != null;
        }
    }
}
