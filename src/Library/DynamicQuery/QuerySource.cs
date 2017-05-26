// -----------------------------------------------------------------------
// <copyright file="QuerySource.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Class for declaring the source of a query.
    /// </summary>
    public abstract class QuerySource
    {
        /// <summary>
        /// Initializes a new instance of the QuerySource class.
        /// </summary>
        protected QuerySource()
        {
            this.Parameters = new List<Parameter>();
        }

        /// <summary>
        /// Initializes a new instance of the QuerySource class.
        /// </summary>
        /// <param name="original">The query source to deep copy.</param>
        protected QuerySource(QuerySource original)
        {
            this.Alias = original.Alias;
            this.Parameters = original.Parameters;
            this.Path = original.Path;
            this.Type = original.Type;
        }

        /// <summary>
        /// Gets the list of parameters.
        /// </summary>
        public List<Parameter> Parameters
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the alias of the query source.
        /// </summary>
        public string Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path of the table.
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type used to generate the query.
        /// </summary>
        public Type Type
        {
            get;
            set;
        }
    }
}
