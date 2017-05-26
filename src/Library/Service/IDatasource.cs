// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd." file="IDatasource.cs">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Linq;

    /// <summary>
    /// Generic interface for all datasources.
    /// </summary>
    public interface IDatasource : IMetadataProvider
    {
        /// <summary>
        /// Gets the current transaction, null if not applicable.
        /// </summary>
        ITransaction CurrentTransaction
        {
            get;
        }

        /// <summary>
        /// Returns the queryable set of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the queryable set.</typeparam>
        /// <param name="includes">The list of includes, if applicable.</param>
        /// <returns>The queryable set of data.</returns>
        IQueryable<T> Get<T>(params string[] includes) where T : class;

        /// <summary>
        /// Returns the single entity of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entityKey">The entity key.</param>
        /// <returns>The entity.</returns>
        T GetByKey<T>(InfrastructureKey entityKey) where T : class;

        /// <summary>
        /// Creates the key for the given entity.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>The created key.</returns>
        InfrastructureKey CreateKey(string entitySetName, object entity);

        /// <summary>
        /// Post an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The added entity.</returns>
        T Post<T>(WriteRequest request) where T : class;

        /// <summary>
        /// Put an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The updated entity.</returns>
        T Put<T>(WriteRequest request) where T : class;

        /// <summary>
        /// Patch an instance to the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        /// <returns>The patched entity.</returns>
        T Patch<T>(WriteRequest request) where T : class;

        /// <summary>
        /// Delete an instance from the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="request">The write request to process.</param>
        void Delete<T>(WriteRequest request) where T : class;

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <typeparam name="T">The type of the return entity.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        IQueryable<T> Execute<T>(ParsedOperation operation);

        /// <summary>
        /// Execute an action against the datasource.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The query result.</returns>
        int Execute(ParsedOperation operation);

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>The transaction.</returns>
        ITransaction BeginTransaction();

        /// <summary>
        /// Save any pending changes.
        /// </summary>
        /// <returns>Count of changes made.</returns>
        int SaveChanges();
    }
}