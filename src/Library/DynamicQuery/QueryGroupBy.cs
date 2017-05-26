// -----------------------------------------------------------------------
// <copyright file="QueryGroupBy.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OdataExpressionModel;

    /// <summary>
    /// Class for declaring a group by item in a query.
    /// </summary>
    public sealed class QueryGroupBy
    {
        /// <summary>
        /// Initializes a new instance of the QueryGroupBy class.
        /// </summary>
        /// <param name="groupingType">The grouping type.</param>
        /// <param name="columns">The list of columns.</param>
        public QueryGroupBy(GroupingType groupingType, params QueryColumn[] columns)
        {
            this.GroupingType = groupingType;
            this.NestedColumns = columns.ToList();
            if (this.GroupingType == GroupingType.None && columns.Length > 1)
            {
                throw new ArgumentException("Only one column per group by when not setting rollup to true.");
            }
        }

        /// <summary>
        /// Gets or sets the grouping type.
        /// </summary>
        public GroupingType GroupingType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of nested columns.
        /// </summary>
        public List<QueryColumn> NestedColumns
        {
            get;
            private set;
        }
    }
}
