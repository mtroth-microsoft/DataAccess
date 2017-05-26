// -----------------------------------------------------------------------
// <copyright file="OneToManyNavigationProperty.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Class for declaring one to many navigation properties.
    /// </summary>
    /// <typeparam name="T">The type of the left side property.</typeparam>
    /// <typeparam name="U">The type of the right side property.</typeparam>
    public class OneToManyNavigationProperty<T, U> : NavigationProperty
    {
        /// <summary>
        /// Initializes a new instance of the OneToManyNavigationProperty class.
        /// </summary>
        /// <param name="property">The left property.</param>
        internal OneToManyNavigationProperty(PropertyInfo property)
        {
            this.Left = property;
        }

        /// <summary>
        /// Initializes a new instance of the OneToManyNavigationProperty class.
        /// </summary>
        /// <param name="left">The left property.</param>
        /// <param name="right">The right property.</param>
        internal OneToManyNavigationProperty(
            PropertyInfo left,
            PropertyInfo right)
        {
            this.Left = left;
            this.Right = right;
        }

        /// <summary>
        /// Map the underlying schema.
        /// </summary>
        /// <param name="mapping">The mapping configuration.</param>
        /// <returns>The navigation configuration.</returns>
        public OneToManyNavigationProperty<T, U> Map(Action<OnetoManyMappingConfiguration> mapping)
        {
            OnetoManyMappingConfiguration otm = new OnetoManyMappingConfiguration();
            mapping(otm);
            this.Mapping = otm;

            if (this.Left != null)
            {
                TypeCache.SetOverride(this.Left, otm.LeftNames, otm.RightNames);
            }

            if (this.Right != null)
            {
                TypeCache.SetOverride(this.Right, otm.RightNames, otm.LeftNames);
            }

            return this;
        }
    }
}
