// -----------------------------------------------------------------------
// <copyright file="ScriptQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    /// <summary>
    /// Class for declaring a union of select query.
    /// </summary>
    public sealed class ScriptQuery : QuerySource
    {
        /// <summary>
        /// The list of queries.
        /// </summary>
        private List<QuerySource> queries = new List<QuerySource>();

        /// <summary>
        /// Add a query to the union.
        /// </summary>
        /// <param name="query">The query to add.</param>
        public void AddQuery(QuerySource query)
        {
            this.queries.Add(query);
        }

        /// <summary>
        /// Gets the list of queries.
        /// </summary>
        public ICollection<QuerySource> Sources
        {
            get
            {
                return this.queries.AsReadOnly();
            }
        }
    }
}
