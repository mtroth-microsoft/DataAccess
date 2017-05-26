// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using Microsoft.Azure.SqlDatabase.ElasticScale.Query;
    using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

    /// <summary>
    /// The class for FederatedSqlStore.
    /// </summary>
    internal sealed class FederatedSqlStore : SqlStore
    {
        /// <summary>
        /// Gets the MultiShardExecutionPolicy for Elastic Scale Multi Shard Command
        /// </summary>
        private static readonly MultiShardExecutionPolicy MultiShardCommandExecutionPolicy = MultiShardExecutionPolicy.CompleteResults;

        /// <summary>
        /// Gets the MultiShardExecutionOptions for Elastic Scale Multi Shard Command
        /// </summary>
        private static readonly MultiShardExecutionOptions MultiShardCommandExecutionOptions = MultiShardExecutionOptions.IncludeShardNameColumn;

        /// <summary>
        /// The list of shard identifiers.
        /// </summary>
        private List<ShardIdentifier> shardIdentifiers = new List<ShardIdentifier>();

        /// <summary>
        /// The connection factory.
        /// </summary>
        private IConnectionFactory factory;

        /// <summary>
        /// Initializes a new instance of the DataContext class.
        /// </summary>
        /// <param name="factory">The connection string factory to use.</param>
        /// <param name="databaseType">The database type.</param>
        /// <param name="shardIds">The shard ids.</param>
        internal FederatedSqlStore(
            IConnectionFactory factory,
            DatabaseType databaseType,
            IEnumerable<ShardIdentifier> shardIds)
            : base(factory, databaseType ?? new DefaultStoreType())
        {
            this.factory = factory;
            this.ConnectionStringCredentials = factory.GetCredentialsOnlyConnectionString(this.DatabaseType.Name);
            if (shardIds == null)
            {
                ShardIdentifier shardIdentifier = ShardMapManagement.Instance.GetDefaultShard(this.DatabaseType);
                this.shardIdentifiers.Add(shardIdentifier);
            }
            else
            {
                this.shardIdentifiers.AddRange(shardIds);
            }
        }

        /// <summary>
        /// Gets Connection String with credentials only.
        /// This string does not have database or server name.
        /// </summary>
        public string ConnectionStringCredentials
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of Shard Identifiers
        /// </summary>
        internal IEnumerable<ShardIdentifier> ShardIdentifiers
        {
            get
            {
                return this.shardIdentifiers.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the collection of connection strings.
        /// Will return only 1 element if querying one database / shard
        /// Will return a collection of connection strings in case of a federated query
        /// </summary>
        /// <returns>The list of connection strings.</returns>
        public override IEnumerable<string> GetConnectionStrings()
        {
            foreach (ShardIdentifier shardIdentifier in this.ShardIdentifiers)
            {
                yield return this.factory.GetConnectionString(shardIdentifier);
            }
        }

        /// <summary>
        /// Execute a non query stored procedure.
        /// This type of stored procedure execution is only supported for single shard.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">The collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>Number of rows affected</returns>
        public override int Execute(
            StoredProcedureNonQuery storedProcedure,
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output)
        {
            return this.Run(storedProcedure, parameters, null, this.CreateConnection(), out output);
        }

        /// <summary>
        /// Execute a non query stored procedure.
        /// This type of stored procedure execution is only supported for single shard.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">The collection of input parameters</param>
        /// <param name="tx">The current transaction.</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>Number of rows affected</returns>
        public override int Execute(
            StoredProcedureNonQuery storedProcedure,
            IEnumerable<IParameter> parameters,
            IDbTransaction tx,
            out IDictionary<string, IParameter> output)
        {
            return this.Run(storedProcedure, parameters, tx, this.CreateConnection(), out output);
        }

        /// <summary>
        /// Execute a stored procedure.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">The collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The data set of results</returns>
        public override DataSet GetData(
            StoredProcedureBase storedProcedure,
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output)
        {
            DataSet dataSet = new DataSet();
            if (this.ShardIdentifiers.Count() == 1)
            {
                dataSet = this.ExecuteSingleShardQuery(storedProcedure, parameters, out output);
            }
            else
            {
                dataSet = this.ExecuteMultiShardQuery(storedProcedure, parameters, out output);
            }

            return dataSet;
        }

        /// <summary>
        /// This api will execute the multi-shard query and return the results.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The data set</returns>
        private DataSet ExecuteMultiShardQuery(
            StoredProcedureBase storedProcedure, 
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output)
        {
            IEnumerable<Shard> shards = ShardMapManagement.Instance.GetShards(this.DatabaseType, this.ShardIdentifiers);
            if (shards == null || shards.Any() == false)
            {
                throw new ArgumentNullException("Failed to identify the target shard for stored procedure execution");
            }

            Dictionary<string, IParameter> resultParameters = new Dictionary<string, IParameter>();
            DataSet dataSet = new DataSet();
            this.SqlRetryPolicy.ExecuteAction(() =>
            {
                using (MultiShardConnection connection = this.CreateMultiShardConnection(shards))
                {
                    // Create a simple command 
                    using (MultiShardCommand command = this.CreateMultiShardCommand(connection, storedProcedure, this.Convert(parameters)))
                    {
                        // Execute the command and instrument the result
                        DataAccessTelemetry.Instance.Instrument(
                            storedProcedure.Name,
                            (e) =>
                            {
                                e.TargetCount = shards.Count();

                                // Execute the command
                                using (MultiShardDataReader reader = command.ExecuteReader())
                                {
                                    do
                                    {
                                        DataTable dataTable = new DataTable();
                                        dataTable.Load(reader);
                                        dataSet.Tables.Add(dataTable);
                                    }
                                    while (reader.IsClosed == false && reader.NextResult() == true);
                                    foreach (SqlParameter sqlparam in command.Parameters)
                                    {
                                        if (sqlparam.Direction == ParameterDirection.Output)
                                        {
                                            Parameter p = new Parameter(sqlparam.ParameterName, sqlparam.Value);
                                            p.DbType = sqlparam.DbType;
                                            p.Direction = sqlparam.Direction;
                                            p.IsNullable = sqlparam.IsNullable;
                                            p.SourceColumn = sqlparam.SourceColumn;
                                            p.SourceVersion = sqlparam.SourceVersion;
                                            resultParameters[sqlparam.ParameterName] = p;
                                        }
                                    }
                                }
                            });
                    }
                }
            });

            output = resultParameters;
            return dataSet;
        }

        /// <summary>
        /// This api will execute the multi-shard query and return the results.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The data set</returns>
        private DataSet ExecuteSingleShardQuery(
            StoredProcedureBase storedProcedure, 
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output)
        {
            return this.Load(
                storedProcedure,
                parameters, 
                null,
                this.CreateConnection(), 
                out output);
        }

        /// <summary>
        /// Create Sql Connection String to a Shard.
        /// </summary>
        /// <returns>The sql connection string.</returns>
        private string CreateConnection()
        {
            if (this.ShardIdentifiers.Count() > 1)
            {
                throw new NotSupportedException();
            }

            ShardIdentifier shardId = this.shardIdentifiers.Count() == 1 ?
                    this.shardIdentifiers.Single() :
                    ShardMapManagement.Instance.GetDefaultShard(this.DatabaseType);

            return this.factory.GetConnectionString(shardId);
        }

        /// <summary>
        /// Create MultiShardConnection to Multiple Shards.
        /// </summary>
        /// <param name="shards">Collection of Shards</param>
        /// <returns>MultiShard sql connection</returns>
        private MultiShardConnection CreateMultiShardConnection(IEnumerable<Shard> shards)
        {
            return new MultiShardConnection(shards, this.ConnectionStringCredentials);
        }

        /// <summary>
        /// Creates Multi Shard Sql command.
        /// </summary>
        /// <param name="connection">The sql connection</param>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Colleciton of input parameters</param>
        /// <returns>The sql command</returns>
        private MultiShardCommand CreateMultiShardCommand(
            MultiShardConnection connection,
            StoredProcedureBase storedProcedure,
            IEnumerable<SqlParameter> parameters)
        {
            MultiShardCommand multiShardCommand = connection.CreateCommand();
            multiShardCommand.CommandTimeout = this.SqlCommandTimeout;
            multiShardCommand.CommandTimeoutPerShard = this.SqlCommandTimeout;
            multiShardCommand.CommandType = CommandType.StoredProcedure;
            multiShardCommand.CommandText = storedProcedure.Name;
            multiShardCommand.ExecutionOptions = MultiShardCommandExecutionOptions;
            multiShardCommand.ExecutionPolicy = MultiShardCommandExecutionPolicy;
            multiShardCommand.Parameters.AddRange(parameters.ToArray());
            return multiShardCommand;
        }
    }
}
