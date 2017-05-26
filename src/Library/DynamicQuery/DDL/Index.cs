// -----------------------------------------------------------------------
// <copyright file="Index.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class Index : TabularObject
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Owner
        {
            get;
            set;
        }

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
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the index is unique.
        /// </summary>
        public bool IsUnique
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the key is a primary key.
        /// </summary>
        public bool IsPrimaryKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the constriant is unique.
        /// </summary>
        public bool IsUniqueConstraint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public Partition Partition
        {
            get;
            set;
        }
    }
}
