// -----------------------------------------------------------------------
// <copyright file="QueryProlog.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// The query prolog class.
    /// </summary>
    public class QueryProlog
    {
        /// <summary>
        /// Gets or sets the raw expression for the prolog.
        /// </summary>
        public string Expression
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the query for the prolog.
        /// </summary>
        public QuerySource Query
        {
            get;
            set;
        }
    }
}
