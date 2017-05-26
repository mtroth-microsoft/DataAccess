// -----------------------------------------------------------------------
// <copyright file="QueryTop.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Text;

    /// <summary>
    /// Class for declaring a top query.
    /// </summary>
    public sealed class QueryTop
    {
        /// <summary>
        /// Gets or sets the top parameter.
        /// </summary>
        public Parameter Parameter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating the top count to query.
        /// </summary>
        public long? Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to set the top with ties.
        /// </summary>
        public bool WithTies
        {
            get;
            set;
        }
    }
}
