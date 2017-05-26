// -----------------------------------------------------------------------
// <copyright file="IDatasourceAwareController.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// The controller interface for use during generic execution.
    /// </summary>
    public interface IDatasourceAwareController
    {
        /// <summary>
        /// Gets or sets the currently active datasource.
        /// </summary>
        IDatasource Datasource
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the error handler.
        /// </summary>
        IErrorHandler ErrorHandler
        {
            get;
            set;
        }
    }
}
