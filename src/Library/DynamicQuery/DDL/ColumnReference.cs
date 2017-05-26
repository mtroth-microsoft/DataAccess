// -----------------------------------------------------------------------
// <copyright file="ColumnReference.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class ColumnReference : SchemaObject
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        public string SourceName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string TargetName
        {
            get;
            set;
        }
    }
}
