// <copyright Company="Lensgrinder, Ltd." file="InfrastructureContainer.cs">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The file summary.</summary>
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.OData.Edm;

    /// <summary>
    /// The Context class.
    /// </summary>
    [Datasource("GenericModel")]
    public partial class InfrastructureContainer : IDatasource
    {
        /// <summary>
        /// The metadata resource.
        /// </summary>
        private const string V1MetadataResource =
            "res://*/CoreModelV1.csdl|res://*/CoreModelV1.ssdl|res://*/CoreModelV1.msl";

        /// <summary>
        /// The name of the datasource.
        /// </summary>
        private const string V1DatasourceName = "CoreModelV1";

        /// <summary>
        /// Initializes a new instance of the InfrastructureContainer class.
        /// </summary>
        /// <param name="requestUri">The current request address.</param>
        public InfrastructureContainer(Uri requestUri)
        {
        }

        /// <summary>
        /// Gets the type of the local model.
        /// </summary>
        public Type ModelType
        {
            get
            {
                return this.GetType();
            }
        }

        /// <summary>
        /// Gets the current transaction, null if not applicable.
        /// </summary>
        public ITransaction CurrentTransaction
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
        public IQueryable<T> Get<T>(params string[] includes)
            where T : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the single entity of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entityKey">The entity key.</param>
        /// <returns>The entity.</returns>
        public T GetByKey<T>(InfrastructureKey entityKey)
            where T : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the key for the given entity.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>The created key.</returns>
        public InfrastructureKey CreateKey(string entitySetName, object entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Post an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The added entity.</returns>
        public T Post<T>(WriteRequest request)
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
        public T Put<T>(WriteRequest request)
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
        public T Patch<T>(WriteRequest request)
            where T : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete an instance from the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        public void Delete<T>(WriteRequest request)
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
        public IQueryable<T> Execute<T>(ParsedOperation operation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        public int Execute(ParsedOperation operation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Save any pending changes.
        /// </summary>
        /// <returns>Count of changes made.</returns>
        public int SaveChanges()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a transaction against this datasource.
        /// </summary>
        /// <returns></returns>
        public ITransaction BeginTransaction()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the model for the datasource.
        /// </summary>
        /// <returns>The model.</returns>
        public IEdmModel GetModel()
        {
            return null;
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
        /// Get the config for the current datasource.
        /// </summary>
        /// <returns>The configuration.</returns>
        public InfrastructureConfigType GetConfig()
        {
            return null;
        }
    }
}