// -----------------------------------------------------------------------
// <copyright file="TypeConfiguration.cs" company="Lensgrinder, Ltd.">
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
    /// Class for declaring type configuration.
    /// </summary>
    /// <typeparam name="T">The type of the configuration.</typeparam>
    public class TypeConfiguration<T>
    {
        /// <summary>
        /// Declare a collection navigational property.
        /// </summary>
        /// <typeparam name="U">The type of the collection property.</typeparam>
        /// <param name="navigationPropertyExpression">The property expression.</param>
        /// <returns>The navigation configuration.</returns>
        public ManyNavigationProperty<T, U> HasMany<U>(Expression<Func<T, ICollection<U>>> navigationPropertyExpression)
        {
            PropertyInfo pi = navigationPropertyExpression.GetSimplePropertyAccess();

            return new ManyNavigationProperty<T, U>(pi);
        }

        /// <summary>
        /// Declare a single navigational property.
        /// </summary>
        /// <typeparam name="U">The type of the single property.</typeparam>
        /// <param name="navigationPropertyExpression">The property expression.</param>
        /// <returns>The navigation configuration.</returns>
        public SingleNavigationProperty<T, U> HasSingle<U>(Expression<Func<T, U>> navigationPropertyExpression)
        {
            PropertyInfo pi = navigationPropertyExpression.GetSimplePropertyAccess();

            return new SingleNavigationProperty<T, U>(pi);
        }
    }
}
