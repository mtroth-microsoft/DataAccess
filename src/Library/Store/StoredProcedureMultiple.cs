// -----------------------------------------------------------------------
// <copyright file="StoredProcedureMultiple.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Stored procedure base class for procedures with multiple result sets.
    /// </summary>
    public abstract class StoredProcedureMultiple : StoredProcedureBase
    {
        /// <summary>
        /// The private data storage.
        /// </summary>
        private List<DataTable> data;

        /// <summary>
        /// Initializes an instance of the StoredProcedureMultiple class.
        /// </summary>
        /// <param name="databaseType">The store type.</param>
        protected StoredProcedureMultiple(DatabaseType databaseType)
            : base(databaseType)
        {
        }

        /// <summary>
        /// Initializes an instance of the StoredProcedureMultiple class.
        /// </summary>
        /// <param name="shardIds">The list of shard identifiers.</param>
        protected StoredProcedureMultiple(IEnumerable<ShardIdentifier> shardIds)
            : base(shardIds)
        {
        }

        /// <summary>
        /// Read the raw data table from the result set.
        /// </summary>
        /// <param name="ordinal">The ordinal representing the location of the result set.</param>
        /// <returns>The raw data table.</returns>
        internal DataTable ReadRawData(int ordinal)
        {
            if (this.data != null && this.data.Count > ordinal)
            {
                return this.data[ordinal];
            }

            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Read the result set at the given location.
        /// </summary>
        /// <typeparam name="T">The type of the result set.</typeparam>
        /// <param name="ordinal">The ordinal representing the location of the result set.</param>
        /// <returns>The list of deserialized objects.</returns>
        protected IEnumerable<T> ReadResultSet<T>(int ordinal)
        {
            if (this.data != null && this.data.Count > ordinal)
            {
                return ToObject<T>(this.data[ordinal]);
            }

            return new List<T>(0);
        }

        /// <summary>
        /// Load the data from the database.
        /// </summary>
        protected virtual void LoadData()
        {
            List<DataTable> tables = new List<DataTable>();
            using (IDataContext context = this.GetDataContext())
            {
                if (this.Timeout.HasValue == true)
                {
                    context.Store.SqlCommandTimeout = (int)this.Timeout.Value.TotalSeconds;
                }

                IDictionary<string, IParameter> output;
                using (DataSet ds = context.Store.GetData(this, this.Parameters, out output))
                {
                    this.OutputParameters = output;
                    foreach (DataTable dt in ds.Tables)
                    {
                        tables.Add(dt);
                    }

                    this.data = tables;
                }
            }
        }
    }
}
