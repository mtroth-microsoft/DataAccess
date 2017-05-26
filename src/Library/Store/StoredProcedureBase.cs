// -----------------------------------------------------------------------
// <copyright file="StoredProcedureBase.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Abstract base class for all stored procedures.
    /// </summary>
    public abstract class StoredProcedureBase
    {
        /// <summary>
        /// Writeable parameter list.
        /// </summary>
        private List<IParameter> parameters = new List<IParameter>();

        /// <summary>
        /// The list of shard ids.
        /// </summary>
        private IEnumerable<ShardIdentifier> shardIds;

        /// <summary>
        /// Initializes an instance of the StoredProcedureBase class.
        /// </summary>
        protected StoredProcedureBase(DatabaseType databaseType)
        {
            this.Name = this.GetType().Name;
            this.DatabaseType = databaseType;
        }

        /// <summary>
        /// Initializes an instance of the StoredProcedureBase class.
        /// </summary>
        /// <param name="shardIds">Collection of shardIdentifiers</param>
        protected StoredProcedureBase(IEnumerable<ShardIdentifier> shardIds)
        {
            this.Name = this.GetType().Name;
            this.shardIds = shardIds;
        }

        /// <summary>
        /// Gets or sets the name of the procedure.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the Database type.
        /// </summary>
        protected internal DatabaseType DatabaseType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of parameters.
        /// </summary>
        protected internal IList<IParameter> Parameters
        {
            get
            {
                return this.parameters.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets or sets the output parameters.
        /// </summary>
        protected internal IDictionary<string, IParameter> OutputParameters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the timeout in seconds for the procedure execution.
        /// </summary>
        protected internal TimeSpan? Timeout
        {
            get;
            set;
        }

        /// <summary>
        /// Maps the rows in a datable to a list of objects
        /// </summary>
        /// <typeparam name="T">the type of object to map to</typeparam>
        /// <param name="dataTable">this object</param>
        /// <returns></returns>
        internal static IEnumerable<T> ToObject<T>(DataTable dataTable)
        {
            Type type = typeof(T);
            if (type.IsClass == false)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    yield return (T)row[0];
                }
            }
            else
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    yield return (T)TypeCache.ToObject(type, row);
                }
            }
        }

        /// <summary>
        /// Set the value for the given parameter.
        /// </summary>
        /// <param name="name">The name of the parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        protected void UpsertParameter(string name, object value)
        {
            object valueToSet = value ?? DBNull.Value as object;
            IParameter parameter = this.Parameters
                .SingleOrDefault(p => p.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (parameter == null)
            {
                parameter = ParameterContext.CreateParameter(name, valueToSet);
                this.parameters.Add(parameter);
            }
            else
            {
                parameter.Value = valueToSet;
            }
        }

        /// <summary>
        /// Reads the value of the parameter.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>The value for the parameter.</returns>
        protected T ReadParameterValue<T>(string name)
        {
            IParameter parameter = this.parameters
                .SingleOrDefault(p => p.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (parameter != null && parameter.Value != DBNull.Value as object)
            {
                return (T)parameter.Value;
            }

            return default(T);
        }

        /// <summary>
        /// Get DataContext based on ShardIdentifier or DatabaseType
        /// </summary>
        /// <returns>The data context.</returns>
        protected IDataContext GetDataContext()
        {
            if (this.shardIds != null && this.shardIds.Count() != 0)
            {
                return DataContextFactory.Instance.GetDataContext(this.DatabaseType, this.shardIds);
            }

            return DataContextFactory.Instance.GetDataContext(this.DatabaseType);
        }
    }
}
