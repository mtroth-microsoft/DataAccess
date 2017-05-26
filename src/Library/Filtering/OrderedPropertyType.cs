// -----------------------------------------------------------------------
// <copyright file="OrderedPropertyType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Text;

    /// <summary>
    /// Corresponds to OrderedPropertyType in model.
    /// </summary>
    public partial class OrderedPropertyType
    {
        /// <summary>
        /// Gets or sets an alias for the parent of the property.
        /// </summary>
        internal string Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a context dependent prefix to the property name.
        /// </summary>
        internal string Prefix
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the db name for the property.
        /// </summary>
        internal string DbName
        {
            get;
            set;
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal string Serialize()
        {
            StringBuilder query = new StringBuilder();
            if (string.IsNullOrEmpty(this.Prefix) == false)
            {
                query.AppendFormat("{0}/", this.Prefix);
            }

            query.Append(this.Name);
            if (this.Ascending == true)
            {
                query.Append(" asc");
            }
            else
            {
                query.Append(" desc");
            }

            return query.ToString();
        }
    }
}
