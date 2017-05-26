// -----------------------------------------------------------------------
// <copyright file="SelectQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// The query options enum.
    /// </summary>
    public enum QueryOptionType
    {
        None,
        ForceOrder,
        Recompile,
        KeepPlan,
    }

    /// <summary>
    /// Class for declaring a select query.
    /// </summary>
    public sealed class SelectQuery : QuerySource
    {
        /// <summary>
        /// Initializes a new instance of the SelectQuery class.
        /// </summary>
        public SelectQuery()
        {
            this.Columns = new List<QueryColumn>();
            this.Joins = new List<QueryJoin>();
            this.GroupBy = new List<QueryGroupBy>();
            this.OrderBy = new List<QueryOrder>();
            this.Secondaries = new List<SelectQuery>();
            this.AllColumns = new List<QueryColumn>();
        }

        /// <summary>
        /// Gets the list of columns.
        /// </summary>
        public List<QueryColumn> Columns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the source of the query.
        /// </summary>
        public QuerySource Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the list of joins.
        /// </summary>
        public List<QueryJoin> Joins
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the filter for the query.
        /// </summary>
        public FilterType Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the top clause for the query.
        /// </summary>
        public QueryTop Top
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the skip clause for the query.
        /// </summary>
        public QueryTop Skip
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to execute a distinct query.
        /// </summary>
        public bool Distinct
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of group by columns.
        /// </summary>
        public List<QueryGroupBy> GroupBy
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the having clause.
        /// </summary>
        public FilterType Having
        {
            get;
            set;
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
        /// Gets or sets the query option to use.
        /// </summary>
        public QueryOptionType QueryOption
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the query prolog to use.
        /// </summary>
        internal QueryProlog Prolog
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the root node for the select statement.
        /// </summary>
        internal CompositeNode RootNode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of secondary queries.
        /// </summary>
        internal List<SelectQuery> Secondaries
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the insert query to use when query is projection.
        /// </summary>
        internal SelectQuery InsertQuery
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path query to use when query is projection.
        /// </summary>
        internal SelectQuery PathQuery
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path subselect to use for this query.
        /// </summary>
        internal SelectQuery PathSubSelect
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of internal columns.
        /// </summary>
        internal List<QueryColumn> AllColumns
        {
            get;
            private set;
        }
    }
}
