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
    using System.Globalization;
    using System.Linq;
    using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

    /// <summary>
    /// The class for Sql Store.
    /// </summary>
    internal class SqlStore : IStore
    {
        /// <summary>
        /// The connection factory.
        /// </summary>
        private IConnectionFactory factory;

        /// <summary>
        /// Initialize an instance of Store
        /// </summary>
        /// <param name="factory">The connection factory.</param>
        /// <param name="databaseType">The database type to use.</param>
        internal SqlStore(IConnectionFactory factory, DatabaseType databaseType)
        {
            this.factory = factory;
            this.DatabaseType = databaseType ?? new DefaultStoreType();
            this.SqlCommandTimeout = 60 * 60;
            this.SqlRetryPolicy = new RetryPolicy<SqlErrorDetectionStrategy>(3, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Gets the database type.
        /// </summary>
        public DatabaseType DatabaseType
        {
            get;
            private set;
        }

        /// <summary>
        /// Sql Command Default Timeout (in Seconds)
        /// </summary>
        public int SqlCommandTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the retry policy to use for connections to SQL Server.
        /// </summary>
        protected RetryPolicy SqlRetryPolicy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the collection of connection strings.
        /// Will return only 1 element if querying one database / shard
        /// Will return a collection of connection strings in case of a federated query
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetConnectionStrings()
        {
            return new string[] { this.factory.GetConnectionString(this.DatabaseType) };
        }

        /// <summary>
        /// Execute query or stored procedure
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">The collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>Number of rows affected</returns>
        public virtual int Execute(
            StoredProcedureNonQuery storedProcedure, 
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output)
        {
            string connectionString = this.factory.GetConnectionString(this.DatabaseType);
            return this.Run(storedProcedure, parameters, null, connectionString, out output);
        }

        /// <summary>
        /// Execute query or stored procedure
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">The collection of input parameters</param>
        /// <param name="tx">The current transaction.</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>Number of rows affected</returns>
        public virtual int Execute(
            StoredProcedureNonQuery storedProcedure,
            IEnumerable<IParameter> parameters,
            IDbTransaction tx,
            out IDictionary<string, IParameter> output)
        {
            string connectionString = this.factory.GetConnectionString(this.DatabaseType);
            return this.Run(storedProcedure, parameters, tx, connectionString, out output);
        }

        /// <summary>
        /// Execute query or stored procedure
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">The collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The data set of results</returns>
        public virtual DataSet GetData(
            StoredProcedureBase storedProcedure,
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output)
        {
            string connectionString = this.factory.GetConnectionString(this.DatabaseType);
            return this.Load(storedProcedure, parameters, null, connectionString, out output);
        }

        /// <summary>
        /// Execute query or stored procedure
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">The collection of input parameters</param>
        /// <param name="tx">The current transaction.</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The data set of results</returns>
        public virtual DataSet GetData(
            StoredProcedureBase storedProcedure,
            IEnumerable<IParameter> parameters,
            IDbTransaction tx,
            out IDictionary<string, IParameter> output)
        {
            string connectionString = this.factory.GetConnectionString(this.DatabaseType);
            return this.Load(storedProcedure, parameters, tx, connectionString, out output);
        }

        /// <summary>
        /// Convert the IParameters into SqlParameters.
        /// </summary>
        /// <param name="collection">The collection of parameters.</param>
        /// <returns>The collection of sql parameters.</returns>
        protected IEnumerable<SqlParameter> Convert(IEnumerable<IParameter> collection)
        {
            IList<SqlParameter> parameters = new List<SqlParameter>();
            foreach (IParameter p in collection)
            {
                object value = p.Value == null ? DBNull.Value as object : p.Value;
                SqlParameter parameter = new SqlParameter(p.ParameterName, value) { DbType = p.DbType };
                parameter.Direction = p.Direction;
                parameter.IsNullable = p.IsNullable;
                parameter.SourceColumn = p.SourceColumn;
                parameter.SourceVersion = p.SourceVersion;

                parameters.Add(parameter);
            }

            return parameters.AsEnumerable<SqlParameter>();
        }

        /// <summary>
        /// Run a stored procedure given a connection string.
        /// </summary>
        /// <param name="sp">The name of the procedure.</param>
        /// <param name="parameters">The parameters to use.</param>
        /// <param name="tx">The current transaction.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The result.</returns>
        protected int Run(
            StoredProcedureNonQuery sp,
            IEnumerable<IParameter> parameters,
            IDbTransaction tx,
            string connectionString,
            out IDictionary<string, IParameter> output)
        {
            Dictionary<string, IParameter> resultParameters = new Dictionary<string, IParameter>();

            int count = -1;
            this.SqlRetryPolicy.ExecuteAction(() =>
            {
                SqlConnection connection = tx == null ? new SqlConnection(connectionString) : tx.Connection as SqlConnection;
                if (tx == null)
                {
                    connection.Open();
                }

                try
                {
                    using (SqlCommand command = this.CreateSqlCommand(connection, sp, this.Convert(parameters)))
                    {
                        // Execute the command and instrument the result
                        command.Transaction = tx as SqlTransaction;
                        DataAccessTelemetry.Instance.Instrument(
                            connection.DataSource,
                            connection.Database,
                            sp.Name,
                            (e) =>
                            {
                                count = command.ExecuteNonQuery();
                                e.Size = count;
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
                            });
                    }
                }
                finally
                {
                    if (tx == null)
                    {
                        connection.SafeDispose();
                    }
                }
            });

            output = resultParameters;
            return count;
        }

        /// <summary>
        /// Load a data set from the given connection string.
        /// </summary>
        /// <param name="sp">The name of the stored procedure to call.</param>
        /// <param name="parameters">The parameters to use.</param>
        /// <param name="tx">The current transaction.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="output">The output parameters to populate.</param>
        /// <returns>The populated dataset.</returns>
        protected DataSet Load(
            StoredProcedureBase sp,
            IEnumerable<IParameter> parameters,
            IDbTransaction tx,
            string connectionString,
            out IDictionary<string, IParameter> output)
        {
            Dictionary<string, IParameter> resultParameters = new Dictionary<string, IParameter>();
            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            this.SqlRetryPolicy.ExecuteAction(() =>
            {
                SqlConnection connection = tx == null ? new SqlConnection(connectionString) : tx.Connection as SqlConnection;
                if (tx == null)
                {
                    connection.Open();
                }

                try
                {
                    using (SqlCommand command = this.CreateSqlCommand(connection, sp, this.Convert(parameters)))
                    {
                        command.Transaction = tx as SqlTransaction;
                        using (SqlDataAdapter adapter = new SqlDataAdapter())
                        {
                            adapter.SelectCommand = command;
                            DataAccessTelemetry.Instance.Instrument(
                                connection.DataSource,
                                connection.Database,
                                sp.Name,
                                (e) =>
                                {
                                    e.Size = adapter.Fill(dataSet);
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
                                });
                        }
                    }
                }
                finally
                {
                    if (tx == null)
                    {
                        connection.SafeDispose();
                    }
                }
            });

            output = resultParameters;
            return dataSet;
        }

        /// <summary>
        /// Creates Sql  command
        /// </summary>
        /// <param name="connection">The sql connection</param>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Colleciton of input parameters</param>
        /// <returns>The sql command</returns>
        private SqlCommand CreateSqlCommand(
            SqlConnection connection,
            StoredProcedureBase storedProcedure,
            IEnumerable<SqlParameter> parameters)
        {
            SqlCommand sqlCommand = connection.CreateCommand();
            sqlCommand.CommandTimeout = this.SqlCommandTimeout;
            sqlCommand.CommandType = CommandType.StoredProcedure;
            sqlCommand.CommandText = storedProcedure.Name;
            sqlCommand.Parameters.AddRange(parameters.ToArray());
            return sqlCommand;
        }
    }
}
