// -----------------------------------------------------------------------
// <copyright file="Owner.cs" Company="Lensgrinder, Ltd.">
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
    internal class Owner : TabularObject
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Name
        {
            get;
            set;
        }
    }
}
