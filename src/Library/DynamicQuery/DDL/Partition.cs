// -----------------------------------------------------------------------
// <copyright file="Partition.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class Partition : SchemaObject
    {
        /// <summary>
        /// The list of columns.
        /// </summary>
        private List<IndexColumn> columns = new List<IndexColumn>();

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public IList<IndexColumn> Columns
        {
            get
            {
                return this.columns;
            }
        }
    }
}
