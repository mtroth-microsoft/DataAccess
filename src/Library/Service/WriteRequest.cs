// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd." file="WriteRequest.cs">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Net.Http;
    using System.Web.OData;

    /// <summary>
    /// Class for passing write information between datasource and controller.
    /// </summary>
    public sealed class WriteRequest
    {
        /// <summary>
        /// Gets or sets the Key for the request, if applicable.
        /// </summary>
        public InfrastructureKey Key
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the EntitySet Name for the request, if applicable.
        /// </summary>
        public string EntitySetName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Entity for the request, if applicable.
        /// </summary>
        public IEdmEntityObject Entity
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the originating request, if applicable.
        /// </summary>
        public HttpRequestMessage Request
        {
            get;
            set;
        }

        /// <summary>
        /// Factory method for creating WriteRequest instances.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set, if applicable.</param>
        /// <param name="entity">The entity, if applicable.</param>
        /// <param name="key">The key, if applicable.</param>
        /// <param name="request">The original request, if applicable.</param>
        /// <returns>The wrapping write request.</returns>
        internal static WriteRequest Create(
            string entitySetName, 
            IEdmEntityObject entity, 
            InfrastructureKey key, 
            HttpRequestMessage request)
        {
            return new WriteRequest() { Entity = entity, EntitySetName = entitySetName, Key = key, Request = request };
        }
    }
}
