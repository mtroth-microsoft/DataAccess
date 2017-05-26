// -----------------------------------------------------------------------
// <copyright file="DynamicProcedureMultiple.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;

    /// <summary>
    /// Class for executing a dynamically defined stored procedure with multiple result sets.
    /// </summary>
    public class DynamicProcedureMultiple : StoredProcedureMultiple
    {
        /// <summary>
        /// Initializes a new instance of the DynamicProcedure class.
        /// </summary>
        /// <param name="type">The type of the database.</param>
        public DynamicProcedureMultiple(DatabaseType type)
            : base(type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DynamicProcedure class.
        /// </summary>
        /// <param name="shardIds">The list of shard identifiers.</param>
        public DynamicProcedureMultiple(IEnumerable<ShardIdentifier> shardIds)
            : base(shardIds)
        {
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

        /// <summary>
        /// Execute the query.
        /// </summary>
        public virtual void Execute()
        {
            this.LoadData();
        }

        /// <summary>
        /// Read data from the underlying result set.
        /// </summary>
        /// <typeparam name="T">The type of the result set.</typeparam>
        /// <param name="ordinal">The ordinal number of the result set.</param>
        /// <returns>The list of entities.</returns>
        public virtual IEnumerable<T> ReadData<T>(int ordinal)
        {
            return this.ReadResultSet<T>(ordinal);
        }
    }
}
