// -----------------------------------------------------------------------
// <copyright file="InsertQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;

    /// <summary>
    /// Class for declaring a delate query.
    /// </summary>
    internal sealed class InsertQuery
    {
        /// <summary>
        /// Initializes a new instance of the InsertQuery class.
        /// </summary>
        public InsertQuery()
        {
            this.Columns = new List<QueryColumn>();
        }

        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        public List<QueryColumn>  Columns
        {
            get;
            private set;
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
