// -----------------------------------------------------------------------
// <copyright file="IErrorHandler.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Web.Http;

    /// <summary>
    /// Interface for handling error condition in controllers.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handle an exception via an api controller.
        /// </summary>
        /// <param name="e">The exception to handle.</param>
        /// <param name="controller">The executing controller.</param>
        /// <returns>The related action result.</returns>
        IHttpActionResult Handle(Exception e, ApiController controller);
    }
}
