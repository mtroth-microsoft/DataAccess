// -----------------------------------------------------------------------
// <copyright file="ISqlSerializationContext.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// The class declaration.
    /// </summary>
    internal interface ISqlSerializationContext
    {
        /// <summary>
        /// Gets or sets a value indicating whether the current context is for inlining the item or not.
        /// </summary>
        bool ForInline
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        Table CurrentTable
        {
            get;
            set;
        }
    }
}
