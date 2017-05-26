// -----------------------------------------------------------------------
// <copyright file="SqlFormatter.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// Helper to provide formatting information during query serialization.
    /// </summary>
    internal sealed class SqlFormatter
    {
        /// <summary>
        /// Initializes a new instance of the SqlFormatter class.
        /// </summary>
        public SqlFormatter()
        {
            this.Indent = string.Empty;
        }

        /// <summary>
        /// Gets or sets the indent margin to use.
        /// </summary>
        public string Indent
        {
            get;
            set;
        }
    }
}
