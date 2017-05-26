// -----------------------------------------------------------------------
// <copyright file="StoredProcedureNonQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Abstract base class for all stored procedures that don't return data.
    /// </summary>
    public abstract class StoredProcedureNonQuery : StoredProcedureBase
    {
        /// <summary>
        /// Initializes an instance of the StoredProcedureNonQuery class.
        /// </summary>
        /// <param name="databaseType">The store type.</param>
        protected StoredProcedureNonQuery(DatabaseType databaseType)
            : base(databaseType)
        {
        }

        /// <summary>
        /// Initializes an instance of the StoredProcedureNonQuery class.
        /// </summary>
        /// <param name="shardIds">The list of shard identifiers.</param>
        protected StoredProcedureNonQuery(IEnumerable<ShardIdentifier> shardIds)
            : base(shardIds)
        {
        }

        /// <summary>
        /// Execute the query.
        /// </summary>
        /// <returns>Records affected.</returns>
        public virtual int Execute()
        {
            using (IDataContext context = this.GetDataContext())
            {
                if (this.Timeout.HasValue == true)
                {
                    context.Store.SqlCommandTimeout = (int)this.Timeout.Value.TotalSeconds;
                }

                IDictionary<string, IParameter> output;
                int result = context.Store.Execute(this, this.Parameters, out output);
                this.OutputParameters = output;

                return result;
            }
        }

        /// <summary>
        /// Execute the query.
        /// </summary>
        /// <returns>Records affected.</returns>
        public virtual int Execute(IDbTransaction tx)
        {
            using (IDataContext context = this.GetDataContext())
            {
                if (this.Timeout.HasValue == true)
                {
                    context.Store.SqlCommandTimeout = (int)this.Timeout.Value.TotalSeconds;
                }

                IDictionary<string, IParameter> output;
                int result = context.Store.Execute(this, this.Parameters, tx, out output);
                this.OutputParameters = output;

                return result;
            }
        }
    }
}
