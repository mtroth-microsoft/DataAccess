// -----------------------------------------------------------------------
// <copyright file="StoredProcedure.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Abstract base class for all stored procedures that return data.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the procedure.</typeparam>
    public abstract class StoredProcedure<T> : StoredProcedureBase
    {
        /// <summary>
        /// Initializes an instance of the StoredProcedure class.
        /// </summary>
        /// <param name="type">The database type.</param>
        protected StoredProcedure(DatabaseType type)
            : base(type)
        {
        }

        /// <summary>
        /// Initializes an instance of the StoredProcedure class.
        /// </summary>
        /// <param name="shardIds">Collection of shardIdentifiers</param>
        protected StoredProcedure(IEnumerable<ShardIdentifier> shardIds)
            : base(shardIds)
        {
        }

        /// <summary>
        /// Execute the query.
        /// </summary>
        /// <returns>The result set.</returns>
        public virtual IEnumerable<T> Execute()
        {
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
                        IEnumerable<T> results = ToObject<T>(dt);

                        // Only support single result set on this call.
                        return results;
                    }
                }
            }

            return new List<T>();
        }

        /// <summary>
        /// Execute the query.
        /// </summary>
        /// <returns>The result set.</returns>
        public virtual IEnumerable<T> Execute(IDbTransaction tx)
        {
            using (IDataContext context = this.GetDataContext())
            {
                if (this.Timeout.HasValue == true)
                {
                    context.Store.SqlCommandTimeout = (int)this.Timeout.Value.TotalSeconds;
                }

                IDictionary<string, IParameter> output;
                using (DataSet ds = context.Store.GetData(this, this.Parameters, tx, out output))
                {
                    this.OutputParameters = output;
                    foreach (DataTable dt in ds.Tables)
                    {
                        IEnumerable<T> results = ToObject<T>(dt);

                        // Only support single result set on this call.
                        return results;
                    }
                }
            }

            return new List<T>();
        }
    }
}
