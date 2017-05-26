// -----------------------------------------------------------------------
// <copyright file="IController.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Web.Http;
    using System.Web.OData;

    /// <summary>
    /// The controller interface for use during generic execution.
    /// </summary>
    public interface IController : IDatasourceAwareController
    {
        /// <summary>
        /// List based action result.
        /// </summary>
        /// <returns>The result.</returns>
        IHttpActionResult Get();

        /// <summary>
        /// Single Entity result.
        /// </summary>
        /// <param name="key">The key argument.</param>
        /// <returns>The result.</returns>
        IHttpActionResult Get(string key);

        /// <summary>
        /// Get a queryable for the entity with the given key.
        /// </summary>
        /// <param name="key">The Key argument.</param>
        /// <returns>The result.</returns>
        IHttpActionResult GetPropertyFrom(string key);

        /// <summary>
        /// Get a queryable for the entity with the given key.
        /// </summary>
        /// <param name="key">The Key argument.</param>
        /// <returns>The result.</returns>
        IHttpActionResult GetNavigationFrom(string key);

        /// <summary>
        /// Get the reference described in the url via the related key.
        /// </summary>
        /// <param name="key">The string supplied as the entity key.</param>
        /// <param name="relatedKey">The string supplied as the related entity key.</param>
        /// <returns>The result.</returns>
        IHttpActionResult GetRelated(
            string key,
            string relatedKey);

        /// <summary>
        /// Bulk load entities.
        /// </summary>
        /// <returns>The result.</returns>
        IHttpActionResult BulkLoad();

        /// <summary>
        /// Post a new entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The result.</returns>
        IHttpActionResult Post(IEdmEntityObject entity);

        /// <summary>
        /// Put an existing entity.
        /// </summary>
        /// <param name="key">The key values.</param>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The result.</returns>
        IHttpActionResult Put(
            string key,
            IEdmEntityObject entity);

        /// <summary>
        /// Patch an existing entity.
        /// </summary>
        /// <param name="key">The key values.</param>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The result.</returns>
        IHttpActionResult Patch(
            string key,
            IEdmEntityObject entity);

        /// <summary>
        /// Delete an existing entity.
        /// </summary>
        /// <param name="key">The key values.</param>
        /// <returns>The result.</returns>
        IHttpActionResult Delete(string key);

        /// <summary>
        /// Get a reference to an entity.
        /// </summary>
        /// <param name="key">The key values.</param>
        /// <returns>The result.</returns>
        IHttpActionResult GetRef(string key);

        /// <summary>
        /// Create a reference to the provided link.
        /// </summary>
        /// <param name="key">The key add the reference to.</param>
        /// <param name="navigationProperty">The relevant navigation property.</param>
        /// <param name="link">The relevant link.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("POST", "PUT")]
        IHttpActionResult CreateRef(
            [FromODataUri] string key,
            string navigationProperty,
            [FromBody]Uri link);

        /// <summary>
        /// Delete a reference.
        /// </summary>
        /// <param name="key">The related key.</param>
        /// <param name="navigationProperty">The navigation property.</param>
        /// <returns>The result.</returns>
        IHttpActionResult DeleteRef(
            string key, 
            string navigationProperty);

        /// <summary>
        /// Invoke a function.
        /// </summary>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        IHttpActionResult InvokeFunction(ODataActionParameters parameters);

        /// <summary>
        /// Invoke a function with a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        IHttpActionResult InvokeKeyFunction(
            [FromODataUri]string key,
            ODataActionParameters parameters);

        /// <summary>
        /// Invoke an action.
        /// </summary>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        IHttpActionResult InvokeAction(ODataActionParameters parameters);

        /// <summary>
        /// Invoke an action with a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        IHttpActionResult InvokeKeyAction(
            [FromODataUri]string key,
            ODataActionParameters parameters);
    }
}
