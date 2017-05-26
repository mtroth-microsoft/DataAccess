// -----------------------------------------------------------------------
// <copyright file="QueryTable.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Text;

    /// <summary>
    /// Table hint enumeration.
    /// </summary>
    public enum HintType
    {
        /// <remarks/>
        NoLock,

        /// <remarks/>
        HoldLock,

        /// <remarks/>
        None,
    }

    /// <summary>
    /// Class for declaring the table source for a query.
    /// </summary>
    public sealed class QueryTable : QuerySource
    {
        /// <summary>
        /// Initializes an instance of the QueryTable class.
        /// </summary>
        public QueryTable()
        {
        }

        /// <summary>
        /// Initializes a new instance of the QueryTable class.
        /// </summary>
        /// <param name="original">The table to deep copy.</param>
        public QueryTable(QueryTable original)
            : base(original)
        {
            this.Hint = original.Hint;
            this.Name = original.Name;
            this.Schema = original.Schema;
        }

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the schema of the table.
        /// </summary>
        public string Schema
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the hint to use.
        /// </summary>
        public HintType Hint
        {
            get;
            set;
        }
    }
}
