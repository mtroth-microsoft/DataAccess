// -----------------------------------------------------------------------
// <copyright file="ManyNavigationProperty.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Data.Entity.Utilities;
    using System.Reflection;

    /// <summary>
    /// Class for declaring collection based navigation properties.
    /// </summary>
    /// <typeparam name="T">The type of the left side property.</typeparam>
    /// <typeparam name="U">The type of the right side property.</typeparam>
    public class ManyNavigationProperty<T, U> : NavigationProperty
    {
        /// <summary>
        /// Initializes a new instance of the ManyNavigationProperty class.
        /// </summary>
        /// <param name="property">The provided property.</param>
        internal ManyNavigationProperty(PropertyInfo property)
        {
            this.Left = property;
        }

        /// <summary>
        /// Declares a many sided relationship.
        /// </summary>
        /// <param name="navigationPropertyExpression">The property expression.</param>
        /// <returns>The navigation configuration.</returns>
        public ManyToManyNavigationProperty<T, U> WithMany(Expression<Func<U, ICollection<T>>> navigationPropertyExpression)
        {
            this.Right = navigationPropertyExpression.GetSimplePropertyAccess();

            return new ManyToManyNavigationProperty<T, U>(this.Left, this.Right);
        }

        /// <summary>
        /// Declares an empty many sided relationship.
        /// </summary>
        /// <returns>The navigation configuration.</returns>
        public ManyToManyNavigationProperty<T, U> WithMany()
        {
            return new ManyToManyNavigationProperty<T, U>(this.Left);
        }

        /// <summary>
        /// Declares a one sided relationship.
        /// </summary>
        /// <param name="navigationPropertyExpression">The property expression.</param>
        /// <returns>The navigation configuration.</returns>
        public OneToManyNavigationProperty<T, U> WithSingle(Expression<Func<U, T>> navigationPropertyExpression)
        {
            this.Right = navigationPropertyExpression.GetSimplePropertyAccess();

            return new OneToManyNavigationProperty<T, U>(this.Left, this.Right);
        }

        /// <summary>
        /// Declares an empty one sided relationship.
        /// </summary>
        /// <returns>The navigation configuration.</returns>
        public OneToManyNavigationProperty<T, U> WithSingle()
        {
            return new OneToManyNavigationProperty<T, U>(this.Left);
        }
    }
}
