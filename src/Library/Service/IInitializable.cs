// -----------------------------------------------------------------------
// <copyright file="IInitializable.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// Interface for initializable datasources.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Initialize the datasource.
        /// </summary>
        void Initialize();
    }
}
