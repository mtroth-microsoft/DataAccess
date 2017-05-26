// -----------------------------------------------------------------------
// <copyright file="DeleteQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using OdataExpressionModel;

    /// <summary>
    /// Class for declaring a delate query.
    /// </summary>
    internal sealed class DeleteQuery
    {
        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        public FilterType Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the target of the delete.
        /// </summary>
        public QueryTable Target
        {
            get;
            set;
        }
    }
}
