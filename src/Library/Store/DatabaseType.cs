// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Enumeration of store protocol options.
    /// </summary>
    public enum StoreProtocol
    {
        /// <summary>
        /// A T-Sql store.
        /// </summary>
        TSql,

        /// <summary>
        /// A MySql store.
        /// </summary>
        MySql
    }

    /// <summary>
    /// Abstract class for Database Type
    /// </summary>
    public abstract class DatabaseType
    {
        /// <summary>
        /// Gets the configuration key for the database.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return "Reporting";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the database is federated.
        /// True if database is federated i.e. it has more than one shard; otherwise, false. The default is false.
        /// </summary>
        public virtual bool Federated
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the shardlet policy for this database type.
        /// </summary>
        public IAddShardletPolicy ShardletPolicy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the protocol for the current database type.
        /// </summary>
        public virtual StoreProtocol Protocol
        {
            get
            {
                return StoreProtocol.TSql;
            }
        }

        /// <summary>
        /// Gets the query executor to use.
        /// </summary>
        internal virtual IExecutor Executor
        {
            get
            {
                if (this.Protocol == StoreProtocol.TSql)
                {
                    return new TSqlQueryExecution();
                }
                else if (this.Protocol == StoreProtocol.MySql)
                {
                    return new MySqlQueryExecution();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
