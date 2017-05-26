// -----------------------------------------------------------------------
// <copyright file="SqlBulkWriter.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using Config = Configuration;

    /// <summary>
    /// Takes a stream and imports it to SQL using bulk insert.
    /// </summary>
    internal sealed class SqlBulkWriter : IBulkWriter
    {
        /// <summary>
        /// The bulk insert timeout
        /// </summary>
        private const int Timeout = 3600;

        /// <summary>
        /// The bulk insert batch copy size
        /// </summary>
        private const int BulkCopyBatchSize = 10000;

        /// <summary>
        /// The number of rows written.
        /// </summary>
        private long rowsWritten = 0;

        /// <summary>
        /// 
        /// </summary>
        private int tableCounter = 0;

        /// <summary>
        /// Value indicating the usability state of the writer.
        /// </summary>
        private bool destroyed = false;

        /// <summary>
        /// Load the writer data via a single connection.
        /// </summary>
        /// <param name="dimension">The dimension being written.</param>
        /// <param name="reader">The reader containing the feed's data.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Records written via bulk load.</returns>
        public long Load(
            Config.Dimension dimension,
            IDataReader reader,
            BulkWriterSettings settings)
        {
            this.CheckDestroyed();
            Table table = new Table(dimension);
            long result = this.Execute(
                settings,
                table, 
                reader, 
                null, 
                BulkCopyBatchSize, 
                false);

            this.destroyed = true;
            return result;
        }

        /// <summary>
        /// Load the writer data via a single connection.
        /// </summary>
        /// <param name="feed">The feed being written.</param>
        /// <param name="reader">The reader containing the feed's data.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Records written via bulk load.</returns>
        public long Load(
            Config.Feed feed, 
            IDataReader reader,
            BulkWriterSettings settings)
        {
            this.CheckDestroyed();
            Table table = new Table(feed);
            long result = this.Execute(
                settings, 
                table, 
                reader, 
                null, 
                BulkCopyBatchSize,
                false);

            this.destroyed = true;
            return result;
        }

        /// <summary>
        /// Load the writer reader data via a single connection.
        /// </summary>
        /// <param name="writerReader">The writer reader with data and set to the correct result.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Records written via bulk load.</returns>
        public long LoadAndMerge(
            WriterReader writerReader,
            BulkWriterSettings settings)
        {
            this.CheckDestroyed();
            Table table;
            MergeQueryBuilder builder = ConfigureReader(writerReader, settings, out table, "#Staging");

            long result = this.Execute(
                settings, 
                table, 
                writerReader, 
                builder, 
                writerReader.Current.RowCount, 
                true);

            this.destroyed = true;
            return result;
        }

        /// <summary>
        /// Load and merge all the data in a set of readers in a single transaction.
        /// </summary>
        /// <param name="executes">The set of operations to run.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>The list of results for each execute.</returns>
        public List<object> LoadAndMergeInTransaction(
            List<object> executes, 
            BulkWriterSettings settings)
        {
            this.CheckDestroyed();
            List<object> results = new List<object>();
            using (SqlConnection connection = CreateAndOpenConnection(settings.Store, settings.ShardIds))
            {
                SqlTransaction tx = connection.BeginTransaction();
                try
                {
                    foreach (object execute in executes)
                    {
                        WriterReader reader = execute as WriterReader;
                        ExecuteNonQuery query = execute as ExecuteNonQuery;
                        ExecuteQuery getdata = execute as ExecuteQuery;
                        if (reader != null)
                        {
                            long rowCount = 0;
                            do
                            {
                                Table table;
                                MergeQueryBuilder builder = ConfigureReader(reader, settings, out table, this.GenerateTableName());
                                string tableName = ConfigureTableName(table);

                                CreateTable(table, connection, true, tx);
                                this.BulkInsert(reader, connection, tableName, reader.Current.RowCount, tx);
                                rowCount += this.rowsWritten;
                                this.rowsWritten = 0;
                                ExecuteMerge(builder, connection, tx);
                            }
                            while (reader.NextResult() == true);
                            results.Add(rowCount);

                            if (settings.LeaveReaderOpen == false && reader.IsClosed == false)
                            {
                                reader.Close();
                            }
                        }
                        else if (query != null)
                        {
                            results.Add(query(tx));
                        }
                        else if (getdata != null)
                        {
                            results.Add(getdata(tx));
                        }
                    }

                    tx.Commit();
                }
                catch (Exception)
                {
                    tx.Rollback();
                    throw;
                }
                finally
                {
                    this.destroyed = true;
                }
            }

            return results;
        }

        /// <summary>
        /// Configure the reader for stage and merge.
        /// </summary>
        /// <param name="reader">The reader to inspect.</param>
        /// <param name="settings">The bulk writer settings.</param>
        /// <param name="table">The table to output.</param>
        /// <param name="tempTableName">The name of the temp table to use.</param>
        /// <returns>The merge query buidler to load reader into table.</returns>
        private static MergeQueryBuilder ConfigureReader(
            WriterReader reader,
            BulkWriterSettings settings,
            out Table table, 
            string tempTableName)
        {
            Config.Feed feed = ResetTableAsFeed(reader.Current, tempTableName);
            table = new Table(feed);

            QueryTable source = new QueryTable() { Name = table.Name, Schema = table.Owner, Hint = HintType.None };
            QueryTable target = reader.Current.QueryTable;
            List<QueryColumn> columns = reader.Current.Columns.ToList();
            MergeQueryBuilder builder = new MergeQueryBuilder(source, target, columns, settings);

            return builder;
        }

        /// <summary>
        /// Configure the table name, give the table structure.
        /// </summary>
        /// <param name="table">The table to inspect.</param>
        /// <returns>The name of the table.</returns>
        private static string ConfigureTableName(Table table)
        {
            QueryTable source = new QueryTable() { Name = table.Name, Schema = table.Owner, Hint = HintType.None };
            if (string.IsNullOrEmpty(table.Owner) == false && table.Owner.Contains('.') == false)
            {
                source.Schema = table.Owner;
            }

            TSqlQuerySerializer serializer = new TSqlQuerySerializer();
            string tableName = serializer.SerializeSource(source, false, null, null);

            return tableName;
        }

        /// <summary>
        /// Create an open a connection to the specified store.
        /// </summary>
        /// <param name="source">The store to connect to.</param>
        /// <param name="shardIds">The shard ids, if applicable.</param>
        /// <returns>The open connection.</returns>
        private static SqlConnection CreateAndOpenConnection(DatabaseType source, IEnumerable<ShardIdentifier> shardIds)
        {
            IDataContext context = null;
            if (shardIds == null)
            {
                context = DataContextFactory.Instance.GetDataContext(source);
            }
            else
            {
                context = DataContextFactory.Instance.GetDataContext(source, new ShardIdentifier[] { shardIds.Single() });
            }

            string connectionString = context.Store.GetConnectionStrings().First();
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            return connection;
        }

        /// <summary>
        /// Convert the writeset to an entity type and then fix it up for use int temp table creation.
        /// </summary>
        /// <param name="current">The WriteSet to inspect.</param>
        /// <param name="tempTableName">The name of the temp table to use.</param>
        /// <returns>The resulting entity type.</returns>
        private static Config.Feed ResetTableAsFeed(WriteSet current, string tempTableName)
        {
            Config.Feed feed = current.Convert();
            feed.Namespace = null;
            feed.Name = tempTableName;
            Config.Property identity = feed.Properties.SingleOrDefault(p => p.AutoIncrement == true);
            if (identity != null)
            {
                identity.AutoIncrement = false;
                Config.PropertyRef key = feed.Keys.SingleOrDefault(p => p.Name == identity.Name);
                if (key != null)
                {
                    feed.Keys.Clear();
                }
            }

            Config.Property timestamp = feed.Properties.SingleOrDefault(p => p.Type == Configuration.DataType.rowversion);
            if (timestamp != null)
            {
                timestamp.Type = Config.DataType.varbinary;
                timestamp.Nullable = true;
            }

            return feed;
        }

        /// <summary>
        /// Create the table, using the provided connection.
        /// </summary>
        /// <param name="table">The table to create.</param>
        /// <param name="connection">The connection to use.</param>
        /// <param name="forInline">True to inline the table, otherwise false.</param>
        /// <param name="tx">The current transaction, if applicable.</param>
        private static void CreateTable(Table table, SqlConnection connection, bool forInline, SqlTransaction tx = null)
        {
            using (SqlCommand cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                SqlSerializationContext context = new SqlSerializationContext() { CurrentTable = table, ForInline = forInline };
                cmd.CommandText = context.Serialize();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Execute the merge, using the provided connection.
        /// </summary>
        /// <param name="builder">The buidler to use.</param>
        /// <param name="connection">The connection to use.</param>
        /// <param name="tx">The current transaction, if applicable.</param>
        private static void ExecuteMerge(MergeQueryBuilder builder, SqlConnection connection, SqlTransaction tx = null)
        {
            if (builder.Query.OutputColumns.Count > 0)
            {
                throw new InvalidDataFilterException("Output columns cannot be specified on bulk writes.");
            }

            using (SqlCommand cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.sp_executesql";
                cmd.CommandTimeout = Timeout;

                string commandText = TSqlQuerySerializer.SerializeMerge(builder.Query, false).Insert(0, "-- AutoGenerated By DataAccess\n");
                string parameterText = TSqlQuerySerializer.SerializeParameterDeclaration(builder.Query.Target);

                cmd.Parameters.Add(new SqlParameter("stmt", commandText));
                cmd.Parameters.Add(new SqlParameter("params", parameterText));
                foreach (Parameter p in builder.Query.Target.Parameters)
                {
                    cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value) { DbType = p.DbType });
                }

                try
                {
                    int rows = cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    if (builder.Query.ConcurrencyCheck != null && ex.Message.Contains(builder.Query.ConcurrencyError) == true)
                    {
                        throw new InvalidDataFilterException(builder.Query.ConcurrencyError, ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Execute the operation via a single connection.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <param name="table">The table to target for the load.</param>
        /// <param name="reader">The reader with data to load.</param>
        /// <param name="builder">The merge, if applicable.</param>
        /// <param name="notifyRows">The number of rows to write before notifying counts.</param>
        /// <param name="forInline">True to inline the table create, otherwise false.</param>
        /// <returns>Records written via bulk load.</returns>
        private long Execute(
            BulkWriterSettings settings,
            Table table,
            IDataReader reader,
            MergeQueryBuilder builder,
            int notifyRows,
            bool forInline)
        {
            string tableName = ConfigureTableName(table);
            using (SqlConnection connection = CreateAndOpenConnection(settings.Store, settings.ShardIds))
            {
                CreateTable(table, connection, forInline);
                this.BulkInsert(reader, connection, tableName, notifyRows);
                if (builder != null)
                {
                    ExecuteMerge(builder, connection);
                }
            }

            if (settings.LeaveReaderOpen == false && reader.IsClosed == false)
            {
                reader.Close();
            }

            return this.rowsWritten;
        }

        /// <summary>
        /// Bulk writes a reader to a SQL table.
        /// </summary>
        /// <param name="reader">The source stream.</param>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="tableName">The name of the table to load.</param>
        /// <param name="notifyRows">The number of rows after which to notify.</param>
        /// <param name="tx">The current transaction, if applicable.</param>
        /// <returns>The count of rows inserted</returns>
        private void BulkInsert(
            IDataReader reader, 
            SqlConnection connection, 
            string tableName, 
            int notifyRows, 
            SqlTransaction tx = null)
        {
            using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, tx))
            {
                WriterReader writerReader = reader as WriterReader;
                if (writerReader != null)
                {
                    ICollection<QueryColumn> columns = writerReader.Current.Columns;
                    foreach (QueryColumn column in columns)
                    {
                        sqlBulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column.Alias, column.Name));
                    }
                }
                else
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string fieldName = reader.GetName(i);
                        sqlBulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fieldName, fieldName));
                    }
                }

                sqlBulkCopy.SqlRowsCopied += RowsCompletedHandler;
                sqlBulkCopy.NotifyAfter = notifyRows;
                sqlBulkCopy.BulkCopyTimeout = Timeout;
                sqlBulkCopy.BatchSize = BulkCopyBatchSize;
                sqlBulkCopy.DestinationTableName = tableName;
                sqlBulkCopy.WriteToServer(reader);
            }
        }

        /// <summary>
        /// Handle row counts from bulk write.
        /// </summary>
        /// <param name="sender">The event's sender.</param>
        /// <param name="e">The args passed by the sender.</param>
        private void RowsCompletedHandler(object sender, SqlRowsCopiedEventArgs e)
        {
            this.rowsWritten += e.RowsCopied;
        }

        /// <summary>
        /// Generate a name for the temporary staging table to use.
        /// </summary>
        /// <returns>The name of the temporary table.</returns>
        private string GenerateTableName()
        {
            string tableName = "#Staging" + this.tableCounter++;

            return tableName;
        }

        /// <summary>
        /// Check the destoyed state of the instance.
        /// </summary>
        private void CheckDestroyed()
        {
            if (this.destroyed == true)
            {
                throw new InvalidOperationException("Can not re-use bulk writer.");
            }
        }
    }
}
