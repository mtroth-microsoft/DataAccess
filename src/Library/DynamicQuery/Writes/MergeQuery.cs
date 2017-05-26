// -----------------------------------------------------------------------
// <copyright file="MergeQuery.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using OdataExpressionModel;

    /// <summary>
    /// Class for declaring a merge query.
    /// </summary>
    internal sealed class MergeQuery
    {
        /// <summary>
        /// Initializes a new instance of the MergeQuery class.
        /// </summary>
        public MergeQuery()
        {
            this.MatchedColumns = new List<QueryColumn>();
            this.TargetUnmatchedColumns = new List<QueryColumn>();
            this.SourceUnmatchedColumns = new List<QueryColumn>();
            this.OutputColumns = new List<QueryColumn>();
            this.ConcurrencyError = "Concurrency check failed.";
        }

        /// <summary>
        /// Gets or sets the bulk writer settings, if applicable.
        /// </summary>
        public BulkWriterSettings BulkWriterSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the query prolog.
        /// </summary>
        public QueryProlog Prolog
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the top argument.
        /// </summary>
        public TopOrSkipType Top
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the merge source.
        /// </summary>
        public QuerySource Target
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the merge target join.
        /// </summary>
        public QueryJoin SourceJoin
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of source columns.
        /// </summary>
        public List<QueryColumn> MatchedColumns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of target unmatched columns.
        /// </summary>
        public List<QueryColumn> TargetUnmatchedColumns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of source unmatched columns.
        /// </summary>
        public List<QueryColumn> SourceUnmatchedColumns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of output columns.
        /// </summary>
        public List<QueryColumn> OutputColumns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the when matched filter.
        /// </summary>
        public FilterType WhenMatched
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the when not matched by target filter.
        /// </summary>
        public FilterType WhenNotMatchedByTarget
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the when not matched by source filer.
        /// </summary>
        public FilterType WhenNotMatchedBySource
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to delete the matching rows.
        /// </summary>
        public bool DeleteMatchedFromTarget
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to delete the non matching rows.
        /// </summary>
        public bool DeleteUnmatchedFromTarget
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the query option.
        /// </summary>
        public QueryOptionType QueryOption
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the concurrency check query to use.
        /// </summary>
        internal QuerySource ConcurrencyCheck
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the concurrency error message to use.
        /// </summary>
        internal string ConcurrencyError
        {
            get;
            set;
        }
    }
}
