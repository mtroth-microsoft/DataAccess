// -----------------------------------------------------------------------
// <copyright file="ManyToManyNavigationProperty.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Class for declaring many to many navigation properties.
    /// </summary>
    /// <typeparam name="T">The type of the left side property.</typeparam>
    /// <typeparam name="U">The type of the right side property.</typeparam>
    public class ManyToManyNavigationProperty<T, U> : NavigationProperty
    {
        /// <summary>
        /// Initializes a new instance of the ManyToManyNavigationProperty class.
        /// </summary>
        /// <param name="property">The parent property.</param>
        internal ManyToManyNavigationProperty(PropertyInfo property)
        {
            this.Left = property;
        }

        /// <summary>
        /// Initializes a new instance of the ManyToManyNavigationProperty class.
        /// </summary>
        /// <param name="left">The left property.</param>
        /// <param name="right">The right property.</param>
        internal ManyToManyNavigationProperty(PropertyInfo left, PropertyInfo right)
        {
            this.Left = left;
            this.Right = right;
        }

        /// <summary>
        /// Map the underlying schema.
        /// </summary>
        /// <param name="mapping">The mapping configuration.</param>
        /// <returns>The navigation configuration.</returns>
        public ManyToManyNavigationProperty<T, U> Map(Action<ManytoManyMappingConfiguration> mapping)
        {
            ManytoManyMappingConfiguration mtm = new ManytoManyMappingConfiguration();
            mapping(mtm);
            this.Mapping = mtm;

            QueryTable imtable = new QueryTable() { Name = mtm.TableName, Schema = mtm.SchemaName };
            if (this.Left != null)
            {
                TypeCache.SetOverride(this.Left, imtable, mtm.LeftNames, mtm.RightNames);
            }

            if (this.Right != null)
            {
                TypeCache.SetOverride(this.Right, imtable, mtm.RightNames, mtm.LeftNames);
            }

            return this;
        }
    }
}
