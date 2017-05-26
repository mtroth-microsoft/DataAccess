// -----------------------------------------------------------------------
// <copyright file="IPropertyBag.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// Helper interface for configuring property bag data.
    /// </summary>
    internal interface IPropertyBag
    {
        /// <summary>
        /// Try to set the value for a given property.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>True if the value was set, otherwise false.</returns>
        bool TrySetPropertyValue(string name, object value);
    }
}