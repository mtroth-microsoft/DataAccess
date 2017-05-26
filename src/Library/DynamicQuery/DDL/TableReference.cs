// -----------------------------------------------------------------------
// <copyright file="TableReference.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class TableReference : SchemaObject
    {
        /// <summary>
        /// The list of references.
        /// </summary>
        private List<ColumnReference> references = new List<ColumnReference>();

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string TargetName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string TargetOwner
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of column references.
        /// </summary>
        public IList<ColumnReference> References
        {
            get
            {
                return this.references;
            }
        }
    }
}
