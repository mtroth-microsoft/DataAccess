// -----------------------------------------------------------------------
// <copyright file="DynamicNonQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;

    /// <summary>
    /// Class for executing a dynamically defined stored procedure.
    /// </summary>
    public class DynamicNonQuery : StoredProcedureNonQuery
    {
        /// <summary>
        /// Initializes a new instance of the DynamicNonQuery class.
        /// </summary>
        /// <param name="type">The database type.</param>
        public DynamicNonQuery(DatabaseType type)
            : base(type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DynamicNonQuery class.
        /// </summary>
        /// <param name="shardIds">The list of shard identifiers.</param>
        public DynamicNonQuery(IEnumerable<ShardIdentifier> shardIds)
            : base(shardIds)
        {
        }

        /// <summary>
        /// Write the contents of a dictionary to the underlying parameter collection.
        /// </summary>
        /// <param name="parameters">The parameter dictionary.</param>
        public void Assign(Dictionary<string, object> parameters)
        {
            foreach (string key in parameters.Keys)
            {
                this.UpsertParameter(key, parameters[key]);
            }
        }

        /// <summary>
        /// Write a single parameter to the underlying collection.
        /// </summary>
        /// <param name="key">The key for the parameter.</param>
        /// <param name="value">The value for the parameter.</param>
        public void Assign(string key, object value)
        {
            this.UpsertParameter(key, value);
        }
    }
}
