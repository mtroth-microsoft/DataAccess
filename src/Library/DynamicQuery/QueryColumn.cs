// -----------------------------------------------------------------------
// <copyright file="QueryColumn.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text;

    /// <summary>
    /// Class for declaring a column in a query.
    /// </summary>
    public sealed class QueryColumn
    {
        /// <summary>
        /// Initializes a new instance of the QueryColumn class.
        /// </summary>
        public QueryColumn()
        {
            this.NestedColumns = new List<QueryColumn>();
        }

        /// <summary>
        /// Initializes a new instance of the QueryColumn class.
        /// </summary>
        /// <param name="original">The original column to deep copy.</param>
        public QueryColumn(QueryColumn original)
        {
            this.AggregateColumnReference = original.AggregateColumnReference;
            this.Alias = original.Alias;
            this.Computed = original.Computed;
            this.ConcurrencyCheck = original.ConcurrencyCheck;
            this.DeclaringType = original.DeclaringType;
            this.DefaultValue = original.DefaultValue;
            this.ElementType = original.ElementType;
            this.Expression = original.Expression;
            this.IsKeyColumn = original.IsKeyColumn;
            this.Name = original.Name;
            this.NestedColumns = original.NestedColumns;
            this.Nullable = original.Nullable;
            this.Size = original.Size;
            this.Source = original.Source;
            this.IsInsertedTime = original.IsInsertedTime;
            this.IsUpdatedTime = original.IsUpdatedTime;
            this.IsChangedBy = original.IsChangedBy;
        }

        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the alias for the column.
        /// </summary>
        public string Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the column expression.
        /// </summary>
        public string Expression
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source of the column.
        /// </summary>
        public QuerySource Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column is part of the key.
        /// </summary>
        public bool IsKeyColumn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default value for the column.
        /// </summary>
        public object DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the column.
        /// </summary>
        public Type ElementType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column is a concurrency check.
        /// </summary>
        internal bool ConcurrencyCheck
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether column is an inserted time column.
        /// </summary>
        internal bool IsInsertedTime
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether column is an updated time column.
        /// </summary>
        internal bool IsUpdatedTime
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether column is the changed by user.
        /// </summary>
        internal bool IsChangedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets db generated option.
        /// </summary>
        internal DatabaseGeneratedOption Computed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column is nullable.
        /// </summary>
        internal bool Nullable
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the column size.
        /// </summary>
        internal int Size
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the declaring type.
        /// </summary>
        internal Type DeclaringType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of nested columns.
        /// </summary>
        internal List<QueryColumn> NestedColumns
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the aggregate column reference of the column.
        /// </summary>
        internal AggregateColumnReference AggregateColumnReference
        {
            get;
            set;
        }
    }
}
