// -----------------------------------------------------------------------
// <copyright file="DataContext.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DataContext : IDataContext
    {
        /// <summary>
        /// The default timeout value to use.
        /// </summary>
        private const int SqlCommandTimeoutInSeconds = 60 * 60;

        /// <summary>
        /// Initializes a new instance of the DataContext class.
        /// </summary>
        /// <param name="databaseType">The Database Type.</param>
        internal DataContext(DatabaseType databaseType)
        {
            IConnectionFactory factory = Container.Get<IConnectionFactory>();
            if (databaseType == null || databaseType.Federated == true)
            {
                this.Store = new FederatedSqlStore(factory, databaseType, null);
            }
            else if (databaseType.Protocol == StoreProtocol.TSql)
            {
                this.Store = new SqlStore(factory, databaseType);
            }
            else if (databaseType.Protocol == StoreProtocol.MySql)
            {
                this.Store = new MySqlStore(factory, databaseType);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Initializes a new instance of the DataContext class.
        /// </summary>
        /// <param name="databaseType">The Database Type.</param>
        /// <param name="shardlets">The list of shardlets.</param>
        internal DataContext(DatabaseType databaseType, IEnumerable<int> shardlets)
        {
            IConnectionFactory factory = Container.Get<IConnectionFactory>();
            List<ShardIdentifier> shardIds = ShardMapManagement.Instance.GetShards(databaseType, shardlets).ToList();
            if (shardIds.Count == 0)
            {
                ShardIdentifier defaultShard = ShardMapManagement.Instance.GetDefaultShard(databaseType);
                shardIds.Add(defaultShard);
            }

            this.Store = new FederatedSqlStore(factory, databaseType, shardIds);
        }

        /// <summary>
        /// Initializes a new instance of the DataContext class.
        /// </summary>
        /// <param name="databaseType">The Database Type.</param>
        /// <param name="shardIds">The list shards.</param>
        internal DataContext(DatabaseType databaseType, IEnumerable<ShardIdentifier> shardIds)
        {
            IConnectionFactory factory = Container.Get<IConnectionFactory>();
            this.Store = new FederatedSqlStore(factory, databaseType, shardIds);
        }

        /// <summary>
        /// The Destructor.
        /// </summary>
        ~DataContext()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the interface to execute queries
        /// </summary>
        public IStore Store { get; protected set; }

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose pattern method.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}