// -----------------------------------------------------------------------
// <copyright file="IHierarchical.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    /// <summary>
    /// Interface to describe hierarchical configuration types.
    /// </summary>
    public interface IHierarchical
    {
        /// <summary>
        /// Gets the name of the base instance.
        /// </summary>
        string Base
        {
            get;
        }

        /// <summary>
        /// Merge this instance with all of its base instances.
        /// </summary>
        /// <param name="baseInstance">The current instance's base instance.</param>
        void Merge(IHierarchical baseInstance);
    }
}
