// -----------------------------------------------------------------------
// <copyright file="IStronglyNamed.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    /// <summary>
    /// Helper interface to indicate strongly named objects.
    /// </summary>
    public interface IStronglyNamed
    {
        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the namespace of the object.
        /// </summary>
        string Namespace { get; }
    }
}
