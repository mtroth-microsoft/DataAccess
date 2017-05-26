// -----------------------------------------------------------------------
// <copyright file="QueryOrder.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Text;

    /// <summary>
    /// Class for declaring an order by statement.
    /// </summary>
    public sealed class QueryOrder
    {
        /// <summary>
        /// Gets or sets the ordered column.
        /// </summary>
        public QueryColumn Column
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the order is ascending.
        /// </summary>
        public bool IsAscending
        {
            get;
            set;
        }
    }
}
