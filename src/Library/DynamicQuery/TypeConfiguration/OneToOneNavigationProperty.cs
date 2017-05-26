// -----------------------------------------------------------------------
// <copyright file="OneToOneNavigationProperty.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Class for declaring one to one navigation properties.
    /// </summary>
    /// <typeparam name="T">The type of the left side property.</typeparam>
    /// <typeparam name="U">The type of the right side property.</typeparam>
    public class OneToOneNavigationProperty<T, U> : NavigationProperty
    {
        /// <summary>
        /// Initializes a new instance of the OneToOneNavigationProperty class.
        /// </summary>
        /// <param name="property">The property configuration.</param>
        internal OneToOneNavigationProperty(PropertyInfo property)
        {
            this.Left = property;
        }

        /// <summary>
        /// Initializes a new instance of the OneToOneNavigationProperty class.
        /// </summary>
        /// <param name="left">The left property.</param>
        /// <param name="right">The right property.</param>
        internal OneToOneNavigationProperty(
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
        public OneToOneNavigationProperty<T, U> Map(Action<OnetoOneMappingConfiguration> mapping)
        {
            OnetoOneMappingConfiguration oto = new OnetoOneMappingConfiguration();
            mapping(oto);
            this.Mapping = oto;

            if (this.Left != null)
            {
                TypeCache.SetOverride(this.Left, oto.LeftNames, oto.RightNames);
            }

            if (this.Right != null)
            {
                TypeCache.SetOverride(this.Right, oto.RightNames, oto.LeftNames);
            }

            return this;
        }
    }
}
