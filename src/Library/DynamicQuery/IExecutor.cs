// -----------------------------------------------------------------------
// <copyright file="IExecutor.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal interface IExecutor
    {
        /// <summary>
        /// Gets the expression to use for count operations.
        /// </summary>
        string CountExpression { get; }

        /// <summary>
        /// Gets a value indicating whether the protocol uses joins to generate skip.
        /// </summary>
        bool UseJoinForSkip { get; }

        /// <summary>
        /// Gets the corresponding projection helper.
        /// </summary>
        IProjectionHelper ProjectionHelper { get; }

        /// <summary>
        /// Gets the bulk writer for the stoer.
        /// </summary>
        IBulkWriter BulkWriter { get; }

        /// <summary>
        /// Run a union query.
        /// </summary>
        /// <param name="query">The union query to run.</param>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIds">The shard ids.</param>
        /// <param name="timeout">The query timeout or null to use the default.</param>
        /// <returns>The results of the query.</returns>
        StoredProcedureMultiple RunMultiple(
            QuerySource query,
            DatabaseType databaseType,
            IEnumerable<ShardIdentifier> shardIds,
            TimeSpan? timeout = null);

        /// <summary>
        /// Run a union query.
        /// </summary>
        /// <param name="query">The union query to run.</param>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIds">The shard ids.</param>
        /// <param name="timeout">The query timeout or null to use the default.</param>
        /// <returns>The results of the query.</returns>
        IQueryable<T> Run<T>(
            QuerySource query,
            DatabaseType databaseType,
            IEnumerable<ShardIdentifier> shardIds,
            TimeSpan? timeout = null);

        /// <summary>
        /// Compile the merge statement into a procedure.
        /// </summary>
        /// <typeparam name="T">The type of the return data.</typeparam>
        /// <param name="merge">The merge query to use.</param>
        /// <param name="databaseType">The store type to use.</param>
        /// <param name="shardId">The shard id to target, if applicable.</param>
        /// <param name="timeout">The query timeout.</param>
        /// <returns>The compiled stored procedure.</returns>
        StoredProcedure<T> CompileMerge<T>(
            MergeQuery merge,
            DatabaseType databaseType,
            ShardIdentifier shardId,
            TimeSpan? timeout = null);

        /// <summary>
        /// Compile the delete queries into a procedure.
        /// </summary>
        /// <param name="deletes">The list of delete queries.</param>
        /// <param name="databaseType">The store type to use.</param>
        /// <param name="shardId">The shard id to target, if applicable.</param>
        /// <param name="timeout">The query timeout.</param>
        /// <returns>The compiled stored procedure.</returns>
        StoredProcedureNonQuery CompileDeletes(
            List<DeleteQuery> deletes,
            DatabaseType databaseType,
            ShardIdentifier shardId,
            TimeSpan? timeout = null);

        /// <summary>
        /// Compile the insert queries into a procedure.
        /// </summary>
        /// <param name="inserts">The list of insert queries.</param>
        /// <param name="databaseType">The store type to use.</param>
        /// <param name="shardId">The shard id to target, if applicable.</param>
        /// <param name="timeout">The query timeout.</param>
        /// <returns>The compiled stored procedure.</returns>
        StoredProcedureNonQuery CompileInserts(
            List<InsertQuery> inserts,
            DatabaseType databaseType,
            ShardIdentifier shardId,
            TimeSpan? timeout = null);

        /// <summary>
        /// Serialize a query.
        /// </summary>
        /// <param name="query">The query to serialize.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>The serialized query.</returns>
        string Serialize(QuerySource query, ParameterContext context, SqlFormatter formatter);
    }
}
