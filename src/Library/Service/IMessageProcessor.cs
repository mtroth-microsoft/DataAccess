// -----------------------------------------------------------------------
// <copyright file="IMessageProcessor.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// <summary>The file summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Net.Http;

    /// <summary>
    /// Interface for message processing datasources.
    /// </summary>
    public interface IMessageProcessor : IDatasource
    {
        /// <summary>
        /// Gets or sets the current message.
        /// </summary>
        HttpRequestMessage Message
        {
            get;
            set;
        }

        /// <summary>
        /// Bulk load data.
        /// </summary>
        /// <returns>The result of the action.</returns>
        long BulkLoad<T>();
    }
}
