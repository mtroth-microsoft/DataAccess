// -----------------------------------------------------------------------
// <copyright file="UnionQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The union query type enum.
    /// </summary>
    internal enum UnionQueryType
    {
        All,
        Distinct,
    }

    /// <summary>
    /// Class for declaring a union of select query.
    /// </summary>
    public sealed class UnionQuery : QuerySource
    {
        /// <summary>
        /// The list of select queries.
        /// </summary>
        private List<SelectQuery> selectQueries = new List<SelectQuery>();

        /// <summary>
        /// The list of union types.
        /// </summary>
        private List<UnionQueryType> unionTypes = new List<UnionQueryType>();

        /// <summary>
        /// Initializes a new instance of the UnionQuery class.
        /// </summary>
        public UnionQuery()
        {
            this.OrderBy = new List<QueryOrder>();
        }

        /// <summary>
        /// Gets the list of ordering statements.
        /// </summary>
        public List<QueryOrder> OrderBy
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of queries.
        /// </summary>
        public ICollection<SelectQuery> Queries
        {
            get
            {
                return this.selectQueries.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the list of union types.
        /// </summary>
        internal ICollection<UnionQueryType> UnionTypes
        {
            get
            {
                return this.unionTypes.AsReadOnly();
            }
        }

        /// <summary>
        /// Add a query to the union.
        /// </summary>
        /// <param name="query">The query to add.</param>
        /// <param name="unionAll">True to add it as a union all, otherwise false.</param>
        public void AddQuery(SelectQuery query, bool unionAll)
        {
            if (query.PathQuery != null)
            {
                throw new ArgumentException("Projection queries can not be unioned.");
            }

            if (this.selectQueries.Count == 0)
            {
                this.selectQueries.Add(query);
            }
            else
            {
                this.selectQueries.Add(query);
                this.unionTypes.Add(unionAll == true ? UnionQueryType.All : UnionQueryType.Distinct);
            }
        }

        /// <summary>
        /// Align the queries in the union to ensure column list is a superset in each case.
        /// </summary>
        internal void Align()
        {
            Dictionary<string, QueryColumn[]> columns = new Dictionary<string, QueryColumn[]>();
            foreach (SelectQuery query in this.selectQueries)
            {
                foreach (QueryColumn column in query.Columns)
                {
                    if (columns.ContainsKey(column.Alias) == false)
                    {
                        columns[column.Alias] = new QueryColumn[this.selectQueries.Count];
                    }

                    columns[column.Alias][this.selectQueries.IndexOf(query)] = column;
                }
            }

            int index = 0;
            foreach (string key in columns.Keys)
            {
                for (int i = 0; i < this.selectQueries.Count; i++)
                {
                    QueryColumn existing = this.selectQueries.SelectMany(p => p.Columns).Where(p => p.Alias == key).First();
                    if (columns[key][i] == null)
                    {
                        QueryColumn placeholder = new QueryColumn();
                        placeholder.Alias = existing.Alias;
                        placeholder.Expression = "NULL";
                        placeholder.ElementType = typeof(string);
                        columns[key][i] = placeholder;
                        this.selectQueries[i].Columns.Insert(index, placeholder);
                        this.selectQueries[i].AllColumns.Insert(index, placeholder);
                    }
                }

                index++;
            }

            foreach (SelectQuery sq in this.selectQueries)
            {
                List<QueryColumn> sorted = sq.Columns.OrderBy(p => p.Alias).ToList();
                sq.Columns.Clear();
                sq.Columns.AddRange(sorted);
            }
        }
    }
}
