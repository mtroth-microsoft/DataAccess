// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Data;

    /// <summary>
    /// Class for Parameter.
    /// </summary>
    public sealed class Parameter : IParameter
    {
        /// <summary>
        /// Initialize an instance of the Paramater class.
        /// </summary>
        public Parameter()
        {
            this.DbType = DbType.String;
            this.Direction = ParameterDirection.Input;
            this.IsNullable = false;
            this.SourceVersion = DataRowVersion.Current;
            this.SourceColumn = string.Empty;
        }

        /// <summary>
        /// Initialize an instance of the Paramater class.
        /// </summary>
        /// <param name="parameterName">The parameter name</param>
        /// <param name="value">The value</param>
        public Parameter(string parameterName, object value)
            : this()
        {
            this.ParameterName = parameterName;
            this.Value = value;
        }

        /// <summary>
        /// The Parameter name
        /// </summary>
        public string ParameterName
        {
            get;
            set;
        }

        /// <summary>
        /// The value of property
        /// </summary>
        public object Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the System.Data.DbType of the parameter.
        /// The default is System.Data.DbType.String.
        /// </summary>
        public DbType DbType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is input-only, output-only,
        /// bidirectional, or a stored procedure return value parameter.
        /// The default is Input.
        /// </summary>
        public ParameterDirection Direction
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the parameter accepts null values.
        /// True if null values are accepted; otherwise, false. The default is false.
        /// </summary>
        public bool IsNullable
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the source column that is mapped to the System.Data.DataSet
        /// and used for loading or returning the System.Data.IDataParameter.Value.
        /// The name of the source column that is mapped to the System.Data.DataSet.
        /// The default is an empty string.
        /// </summary>
        public string SourceColumn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the System.Data.DataRowVersion to use when loading System.Data.IDataParameter.Value.
        /// The default is Current.
        /// </summary>
        public DataRowVersion SourceVersion
        {
            get;
            set;
        }
    }
}