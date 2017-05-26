// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;

    /// <summary>
    /// Factory to instantiate DataContext
    /// </summary>
    public sealed class DataContextFactory
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        private static DataContextFactory instance = new DataContextFactory();

        /// <summary>
        /// Prevents a default instance of the DataContextFactory class from being created.
        /// </summary>
        private DataContextFactory()
        {
        }

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static DataContextFactory Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// The Data Context to the database
        /// </summary>
        /// <param name="databaseType">Type of database</param>
        /// <returns>Data Context</returns>
        /// <exception cref="InvalidArgrumentException">If Database Type is null</exception>
        /// <exception cref="NotSupportedException">If Database Type is not Reporting Type</exception>
        public IDataContext GetDataContext(DatabaseType databaseType)
        {
            return new DataContext(databaseType);
        }

        /// <summary>
        /// Provides a Data Context to one or multiple partitions based on shard names
        /// </summary>
        /// <param name="databaseType">Type of database</param>
        /// <param name="shards">List of Shard Names</param>
        /// <returns>Data Context</returns>
        /// <exception cref="InvalidArgrumentException">If list contains a shard that is not present in Shard Map</exception>
        public IDataContext GetDataContext(DatabaseType databaseType, IEnumerable<ShardIdentifier> shards)
        {
            return new DataContext(databaseType, shards);
        }

        /// <summary>
        /// Provides a Data Context to one or multiple partitions based on shardlet list.
        /// </summary>
        /// <param name="databaseType">Type of database</param>
        /// <param name="shardlets">The list of shardlets.</param>
        /// <returns>The data context.</returns>
        public IDataContext GetDataContext(DatabaseType databaseType, IEnumerable<int> shardlets)
        {
            return new DataContext(databaseType, shardlets);
        }
    }
}