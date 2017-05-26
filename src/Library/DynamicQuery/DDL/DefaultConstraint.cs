// -----------------------------------------------------------------------
// <copyright file="DefaultConstraint.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Text;
    using Config = Configuration;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class DefaultConstraint : SchemaObject
    {
        /// <summary>
        /// Initializes a new instance of the DefaultConstraint class.
        /// </summary>
        /// <param name="property">The property to use to create the class.</param>
        public DefaultConstraint(Config.Property property)
        {
            this.Value = property.Default;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Value
        {
            get;
            private set;
        }
    }
}
