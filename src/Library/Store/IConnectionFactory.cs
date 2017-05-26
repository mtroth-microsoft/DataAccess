// -----------------------------------------------------------------------
// <copyright file="IConnectionFactory.cs" company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Data.Entity.Core.EntityClient;

    /// <summary>
    /// The connection factory interface.
    /// </summary>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Gets a connection string based on a store type and list of segments.
        /// Empty segments list indicates all segments.
        /// </summary>
        /// <param name="databaseType">The store type.</param>
        /// <returns>The correlated connection string.</returns>
        string GetConnectionString(DatabaseType databaseType);

        /// <summary>
        /// Gets a connection string based on a store type and list of segments.
        /// Empty segments list indicates all segments.
        /// </summary>
        /// <param name="databaseType">The store type.</param>
        /// <returns>The correlated connection string.</returns>
        string GetConnectionString(string databaseType);

        /// <summary>
        /// Gets a connection string based on a store type and list of segments.
        /// Empty segments list indicates all segments.
        /// </summary>
        /// <param name="shardId">The shard identifier.</param>
        /// <returns>The correlated connection string.</returns>
        string GetConnectionString(ShardIdentifier shardId);

        /// <summary>
        /// Gets an entity connection.
        /// </summary>
        /// <param name="activeModel">The active model.</param>
        /// <param name="metadataResource">The metadata resource string.</param>
        /// <param name="databaseType">The store type for the connection.</param>
        /// <returns>The entity connection to use.</returns>
        EntityConnection GetEntityConnection(
            string activeModel,
            string metadataResource,
            string databaseType);

        /// <summary>
        /// Gets the Connection String Credentials
        /// This string only contains the credentials required to connect to database.
        /// This string cannot be used to connect to database because it does not have server name
        /// or database name.
        /// </summary>
        /// <param name="databaseType">The Database Key.</param>
        /// <returns>Connection String Credentials</returns>
        string GetCredentialsOnlyConnectionString(string databaseType);
    }
}
