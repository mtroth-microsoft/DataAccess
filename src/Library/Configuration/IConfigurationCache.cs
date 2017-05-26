// -----------------------------------------------------------------------
// <copyright file="IConfigurationCache.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    using System.Collections.Generic;
    using OdataExpressionModel;

    /// <summary>
    /// The public interface for accessing the configuration cache.
    /// </summary>
    public interface IConfigurationCache
    {
        /// <summary>
        /// Query for a specific instance of a given type.
        /// </summary>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <param name="fullName">The full name of the instance.</param>
        /// <returns>The correlated instance.</returns>
        T GetWellKnownInstance<T>(string fullName)
            where T : class;

        /// <summary>
        /// Query for a specific instance of a given type.
        /// </summary>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <param name="fullName">The fully qualified name of the instance.</param>
        /// <returns>The correlated instance.</returns>
        T GetWellKnownInstance<T>(FullyQualifiedNamedObject fullName)
            where T : class;

        /// <summary>
        /// Get the set of instances corresponding to the provided type.
        /// </summary>
        /// <typeparam name="T">The type to look for.</typeparam>
        /// <returns>The list of corresponding entities.</returns>
        IEnumerable<T> GetTypedSet<T>()
            where T : class, IStronglyNamed;

        /// <summary>
        /// Locates the fact base instance for the feed reference.
        /// </summary>
        /// <param name="feedReference">The feed reference to inspect.</param>
        /// <returns>The fact base.</returns>
        FactBase GetFactBase(FeedReference feedReference);

    }
}
