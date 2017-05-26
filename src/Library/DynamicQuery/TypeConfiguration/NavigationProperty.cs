// -----------------------------------------------------------------------
// <copyright file="NavigationProperty.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Reflection;

    /// <summary>
    /// Abstract base class for property configuration.
    /// </summary>
    public abstract class NavigationProperty
    {
        /// <summary>
        /// Gets or sets the left property for this navigation.
        /// </summary>
        protected PropertyInfo Left
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the right property for this navigation.
        /// </summary>
        protected PropertyInfo Right
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the mapping configuration.
        /// </summary>
        protected MappingConfiguration Mapping
        {
            get;
            set;
        }
    }
}
