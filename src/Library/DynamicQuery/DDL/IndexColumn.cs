// -----------------------------------------------------------------------
// <copyright file="IndexColumn.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Text;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class IndexColumn : SchemaObject
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        public string ColumnName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the key is descending.
        /// </summary>
        public bool IsDescendingKey
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the column is an include column.
        /// </summary>
        public bool IsIncludedColumn
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int PartitionOrdinal
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int KeyOrdinal
        {
            get;
            set;
        }
    }
}
