// -----------------------------------------------------------------------
// <copyright file="IMetadataProvider.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// <summary>The file summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Generic interface for all metadata providers.
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets the type of the model.
        /// </summary>
        Type ModelType { get; }

        /// <summary>
        /// Get the model for the datasource.
        /// </summary>
        /// <returns>The compiled model.</returns>
        IEdmModel GetModel();

        /// <summary>
        /// Get the list of controller types.
        /// </summary>
        /// <returns>The list of controller types, if the call returns null, 
        /// all controllers from the ModelType's assembly will be placed in scope.</returns>
        IEnumerable<Type> GetControllerTypes();

        /// <summary>
        /// Get the configuration for the service.
        /// </summary>
        /// <returns></returns>
        InfrastructureConfigType GetConfig();
    }
}