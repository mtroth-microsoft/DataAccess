// -----------------------------------------------------------------------
// <copyright file="GenericController.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Extensions;
    using System.Web.OData.Routing;
    using FastMember;
    using Microsoft.OData.Edm;
    using Newtonsoft.Json.Linq;
    using UriParser = Microsoft.OData.UriParser;

    /// <summary>
    /// A generic controller for use by any datasource.
    /// </summary>
    public class GenericController<T> : InfrastructureController, IController
        where T : class
    {
        /// <summary>
        /// Message for unsupported functions.
        /// </summary>
        private const string UnsupportedFunctionMessage = "Can not support this type of function, " +
            "derive from Generic Controller and implement method directly.";

        /// <summary>
        /// The logger to use.
        /// </summary>
        private static IMessageLogger logger = Container.Get<IMessageLogger>();

        /// <summary>
        /// Cache of method reflection based method calls.
        /// </summary>
        private static ConcurrentDictionary<KeyValuePair<Type, string>, MethodInfo> reflectionCache =
            new ConcurrentDictionary<KeyValuePair<Type, string>, MethodInfo>();

        /// <summary>
        /// Cache of type accessors.
        /// </summary>
        private static ConcurrentDictionary<Type, TypeAccessor> accessorCache =
            new ConcurrentDictionary<Type, TypeAccessor>();

        /// <summary>
        /// Cache of models.
        /// </summary>
        private static ConcurrentDictionary<Type, IEdmModel> modelCache =
            new ConcurrentDictionary<Type, IEdmModel>();

        /// <summary>
        /// Gets the model for the current datasource.
        /// </summary>
        public IEdmModel Model
        {
            get
            {
                if (this.Datasource != null)
                {
                    return modelCache.GetOrAdd(this.Datasource.ModelType, this.Datasource.GetModel());
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get the iqueryable for the defined type.
        /// </summary>
        /// <returns>The iqueryable data set.</returns>
        [EnableQuery]
        public virtual IHttpActionResult Get()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                ODataPath path = this.Request.ODataProperties().Path;
                EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                if (entitySet == null)
                {
                    entitySet = path.Segments[0].GetNavigationSource(null) as EdmEntitySet;
                }

                this.VerifyRead(entitySet);

                if (path.PathTemplate.Equals("~/entityset/cast") == true)
                {
                    return this.HandleDerivedTypeQuery(path);
                }
                else
                {
                    IQueryable<T> content = this.Datasource.Get<T>();

                    return this.Ok<IQueryable<T>>(content);
                }
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "Get", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Get the entity by the key.
        /// </summary>
        /// <param name="key">The key argument.</param>
        /// <returns>The result.</returns>
        [EnableQuery]
        public virtual IHttpActionResult Get([FromODataUri]string key)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                ODataPath path = this.Request.ODataProperties().Path;
                IEdmEntityType entityType = this.Convert(path.EdmType);
                EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                if (entitySet == null)
                {
                    entitySet = path.Segments[0].GetNavigationSource(null) as EdmEntitySet;
                }

                this.VerifyRead(entitySet);
                T content = this.ReadEntity(entitySet, key, entityType);
                if (content == null)
                {
                    return this.NotFound();
                }

                return this.Ok<T>(content);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "GetByKey", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Get the property target for the entity with the provided key.
        /// </summary>
        /// <param name="key">The string supplied as the entity key.</param>
        /// <returns>The entity result.</returns>
        [EnableQuery]
        public virtual IHttpActionResult GetPropertyFrom([FromODataUri]string key)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                return this.GetFrom(key);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "GetPropertyFrom", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Get the navigation target for the entity with the provided key.
        /// </summary>
        /// <param name="key">The string supplied as the entity key.</param>
        /// <returns>The entity result.</returns>
        [EnableQuery]
        public virtual IHttpActionResult GetNavigationFrom([FromODataUri]string key)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                return this.GetFrom(key);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "GetNavigationFrom", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Get the reference described in the url via the related key.
        /// </summary>
        /// <param name="key">The string supplied as the entity key.</param>
        /// <param name="relatedKey">The string supplied as the related entity key.</param>
        /// <returns>The result.</returns>
        [EnableQuery]
        public virtual IHttpActionResult GetRelated(
            [FromODataUri]string key,
            string relatedKey)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                ODataPath path = this.Request.ODataProperties().Path;
                if (path.Segments.Last() is UriParser.NavigationPropertyLinkSegment)
                {
                    IEdmType edm = null;
                    IEdmNavigationSource src = null;
                    foreach (UriParser.ODataPathSegment segment in path.Segments)
                    {
                        src = segment.GetNavigationSource(src);
                        edm = segment.EdmType;
                    }

                    IEdmEntitySet entitySet = (IEdmEntitySet)src;
                    IEdmEntityType entityType = this.Convert(edm);
                    string entitySetName = this.Datasource.ModelType.Name + '.' + entitySet.Name;
                    InfrastructureKey otherKey = new InfrastructureKey(
                        entitySetName,
                        this.BuildKey(relatedKey, entityType));

                    this.VerifyRead(entitySet);
                    Uri address = this.ConstructUri(entitySet.Name, otherKey);
                    return this.Ok(address);
                }

                return this.Get(relatedKey);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "GetRelated", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// The bulk load handler.
        /// </summary>
        /// <returns>The result.</returns>
        [AcceptVerbs("PUT")]
        public IHttpActionResult BulkLoad()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                this.ConfigureDatasource();
                IMessageProcessor processor = this.Datasource as IMessageProcessor;
                if (processor == null)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    ODataPath path = this.Request.ODataProperties().Path;
                    EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                    string entitySetName = this.Datasource.ModelType.Name + '.' + entitySet.Name;

                    this.VerifyWrite(entitySet);

                    return this.Ok(processor.BulkLoad<T>());
                }
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "BulkLoad", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Post a new entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("POST")]
        public virtual IHttpActionResult Post([FromBody]IEdmEntityObject entity)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                if (entity == null)
                {
                    return this.BadRequest();
                }

                this.ConfigureDatasource();

                ODataPath path = this.Request.ODataProperties().Path;
                EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                string entitySetName = this.Datasource.ModelType.Name + '.' + entitySet.Name;

                this.VerifyWrite(entitySet);

                // Extract references only works if we are in a $batch post.
                T added = this.Datasource.Post<T>(WriteRequest.Create(entitySetName, entity, null, this.Request));
                this.ExtractReferences(added);

                // Controller adds to transaction, but datasource should do it.
                if (this.Datasource.CurrentTransaction != null)
                {
                    object contentId;
                    this.Request.Properties.TryGetValue("ContentId", out contentId);
                    this.Datasource.CurrentTransaction.AddObject(int.Parse(contentId.ToString()), added);
                }

                return this.Created<T>(added);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "Post", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Put an existing entity.
        /// </summary>
        /// <param name="key">The entity key.</param>
        /// <param name="entity">The entity to put.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("PUT")]
        public virtual IHttpActionResult Put(
            [FromODataUri]string key,
            [FromBody]IEdmEntityObject entity)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                if (entity == null)
                {
                    return this.BadRequest();
                }

                this.ConfigureDatasource();

                ODataPath path = this.Request.ODataProperties().Path;
                IEdmEntityType entityType = this.Convert(path.EdmType);

                EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                string entitySetName = this.Datasource.ModelType.Name + '.' + entitySet.Name;
                InfrastructureKey entityKey = new InfrastructureKey(entitySetName, this.BuildKey(key, entityType));

                this.VerifyWrite(entitySet);
                T updated = this.Datasource.Put<T>(WriteRequest.Create(entitySetName, entity, entityKey, this.Request));

                return this.Updated<T>(updated);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "Put", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Patch an existing entity.
        /// </summary>
        /// <param name="key">The key values.</param>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("PATCH", "MERGE")]
        public virtual IHttpActionResult Patch(
            [FromODataUri]string key,
            [FromBody]IEdmEntityObject entity)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                if (entity == null)
                {
                    return this.BadRequest();
                }

                this.ConfigureDatasource();

                ODataPath path = this.Request.ODataProperties().Path;
                IEdmEntityType entityType = this.Convert(path.EdmType);

                EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                string entitySetName = this.Datasource.ModelType.Name + '.' + entitySet.Name;
                InfrastructureKey entityKey = new InfrastructureKey(entitySetName, this.BuildKey(key, entityType));

                this.VerifyWrite(entitySet);
                T originalEntity = this.Datasource.Patch<T>(WriteRequest.Create(entitySetName, entity, entityKey, this.Request));

                return this.Updated<T>(originalEntity);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "Patch", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Delete an existing entity.
        /// </summary>
        /// <param name="key">The entity key.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("DELETE")]
        public virtual IHttpActionResult Delete([FromODataUri]string key)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                this.ConfigureDatasource();

                ODataPath path = this.Request.ODataProperties().Path;
                IEdmEntityType entityType = this.Convert(path.EdmType);

                EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                string entitySetName = this.Datasource.ModelType.Name + '.' + entitySet.Name;
                InfrastructureKey entityKey = new InfrastructureKey(entitySetName, this.BuildKey(key, entityType));

                this.VerifyWrite(entitySet);
                this.Datasource.Delete<T>(WriteRequest.Create(entitySetName, null, entityKey, this.Request));

                return this.StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "Delete", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Get the reference described in the url.
        /// </summary>
        /// <param name="key">The string supplied as the entity key.</param>
        /// <returns>The result.</returns>
        public virtual IHttpActionResult GetRef([FromODataUri]string key)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                ODataPath path = this.Request.ODataProperties().Path;
                EdmEntitySet rootSet = path.Segments[0].GetNavigationSource(null) as EdmEntitySet;
                IEdmEntityType root = this.Convert(path.Segments[0].EdmType);
                EdmEntitySet entitySet = path.NavigationSource as EdmEntitySet;
                if (entitySet == null)
                {
                    entitySet = path.Segments[0].GetNavigationSource(null) as EdmEntitySet;
                }

                this.VerifyRead(entitySet);
                string propertyName = path.Segments.Last().Identifier;
                if (propertyName.Equals("$ref") == true)
                {
                    if (path.PathTemplate.EndsWith("key/$ref") == true)
                    {
                        propertyName = path.Segments[path.Segments.Count - 3].Identifier;
                    }
                    else
                    {
                        propertyName = path.Segments[path.Segments.Count - 2].Identifier;
                    }
                }

                T entity = this.ReadEntity(rootSet, key, root);
                Type t = entity.GetType();
                TypeAccessor accessor = accessorCache.GetOrAdd(t, p => TypeAccessor.Create(t));
                object value = accessor[entity, propertyName];

                IEnumerable list = value as IEnumerable;
                if (list == null)
                {
                    InfrastructureKey otherKey = this.Datasource.CreateKey(entitySet.Name, value);
                    Uri address = this.ConstructUri(entitySet.Name, otherKey);
                    return this.Ok(address);
                }
                else
                {
                    List<Uri> addresses = new List<Uri>();
                    foreach (object item in list)
                    {
                        InfrastructureKey itemKey = this.Datasource.CreateKey(entitySet.Name, item);
                        Uri address = this.ConstructUri(entitySet.Name, itemKey);
                        addresses.Add(address);
                    }

                    return this.Ok(addresses);
                }
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "GetRef", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Create a reference to the provided link.
        /// </summary>
        /// <param name="key">The key add the reference to.</param>
        /// <param name="navigationProperty">The relevant navigation property.</param>
        /// <param name="link">The relevant link.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("POST", "PUT")]
        public virtual IHttpActionResult CreateRef(
            [FromODataUri] string key,
            string navigationProperty,
            [FromBody]Uri link)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                this.ConfigureDatasource();

                ODataPath path = this.Request.ODataProperties().Path;
                IEdmEntityType entityType = this.Convert(path.Segments[0].EdmType);
                EdmEntitySet entitySet = path.Segments[0].GetNavigationSource(null) as EdmEntitySet;
                this.VerifyWrite(entitySet);

                T entity = this.ReadEntity(entitySet, key, entityType);
                this.AssignReference(entity, navigationProperty, link);
                if (this.Datasource.CurrentTransaction == null)
                {
                    this.Datasource.SaveChanges();
                }

                return this.StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "CreateRef", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Delete the reference to the provided link.
        /// </summary>
        /// <param name="key">The related key.</param>
        /// <param name="navigationProperty">The relevant navigation property.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("DELETE")]
        public virtual IHttpActionResult DeleteRef(
            [FromODataUri]string key,
            string navigationProperty)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                this.ConfigureDatasource();

                ODataPath path = this.Request.ODataProperties().Path;
                IEdmEntityType entityType = this.Convert(path.Segments[0].EdmType);
                EdmEntitySet entitySet = path.Segments[0].GetNavigationSource(null) as EdmEntitySet;
                this.VerifyWrite(entitySet);

                T entity = this.ReadEntity(entitySet, key, entityType);
                IEdmEntityType otherType = this.Convert(path.EdmType);
                Type type = this.Datasource.ModelType.Assembly.GetType(otherType.FullTypeName());

                KeyValuePair<Type, string> reflectionKey = new KeyValuePair<Type, string>(type, "Remove");
                MethodInfo generic = reflectionCache.GetOrAdd(
                    reflectionKey,
                    p =>
                        this
                        .GetType()
                        .GetMethod("Remove", BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod(type));

                generic.Invoke(this, new object[] { entity, navigationProperty });
                if (this.Datasource.CurrentTransaction == null)
                {
                    this.Datasource.SaveChanges();
                }

                return this.StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "DeleteRef", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Invoke a function.
        /// </summary>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("GET")]
        [EnableQuery]
        public virtual IHttpActionResult InvokeFunction(ODataActionParameters parameters)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                this.ExtractParametersFromUri(ref parameters);
                return this.InvokeOperation(parameters);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "InvokeAction", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Invoke a function with a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("GET")]
        [EnableQuery]
        public virtual IHttpActionResult InvokeKeyFunction(
            [FromODataUri]string key,
            ODataActionParameters parameters)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                this.ExtractParametersFromUri(ref parameters);
                return this.InvokeKeyOperation(key, parameters);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "InvokeKeyAction", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Invoke an action.
        /// </summary>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("POST")]
        [EnableQuery]
        public virtual IHttpActionResult InvokeAction(ODataActionParameters parameters)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                return this.InvokeOperation(parameters);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "InvokeAction", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Invoke an action with a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        [AcceptVerbs("POST")]
        [EnableQuery]
        public virtual IHttpActionResult InvokeKeyAction(
            [FromODataUri]string key,
            ODataActionParameters parameters)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                return this.InvokeKeyOperation(key, parameters);
            }
            catch (Exception e)
            {
                exception = e;
                IHttpActionResult result = this.Handle(e);
                if (result == null)
                {
                    throw;
                }

                return result;
            }
            finally
            {
                logger.FireQosEvent(this.GetType().FullName, "InvokeKeyAction", sw.Elapsed, exception);
            }
        }

        /// <summary>
        /// Query for derived types of T.
        /// </summary>
        /// <typeparam name="U">The base type.</typeparam>
        /// <param name="outerIsBase">True if T is base, otherwise false.</param>
        /// <returns>The result set.</returns>
        protected IHttpActionResult Derive<U>(bool outerIsBase)
            where U : class
        {
            if (outerIsBase == true)
            {
                IQueryable<U> content = this.Datasource.Get<T>().OfType<U>();
                return this.Ok(content);
            }
            else
            {
                IQueryable<T> content = this.Datasource.Get<U>().OfType<T>();
                return this.Ok(content);
            }
        }

        /// <summary>
        /// Helper method to cast a result into a dynamically determined type.
        /// </summary>
        /// <typeparam name="U">The dynamically determined type.</typeparam>
        /// <param name="instance">The object instance.</param>
        /// <returns>The result wrapped the casted object.</returns>
        protected IHttpActionResult Cast<U>(object instance)
        {
            return this.Ok<U>((U)instance);
        }

        /// <summary>
        /// Assign the value to the navigation property.
        /// </summary>
        /// <typeparam name="U">The type of the target.</typeparam>
        /// <param name="otherKey">The other entity's key.</param>
        /// <param name="entity">The current entity.</param>
        /// <param name="navigationProperty">The property to assign.</param>
        protected void Assign<U>(InfrastructureKey otherKey, T entity, string navigationProperty)
            where U : class
        {
            U otherEntity = null;
            object map;
            bool hastarget = this.Request.Properties.TryGetValue("ContentIdMapping", out map);
            Dictionary<string, string> target = map as Dictionary<string, string>;
            if (target != null)
            {
                hastarget = target.Count > 0;
            }

            int value;
            if (this.Datasource.CurrentTransaction != null && hastarget == true &&
                int.TryParse(target.Keys.Last().ToString(), out value) == true)
            {
                otherEntity = this.Datasource.CurrentTransaction.GetObject(value) as U;
            }

            if (otherEntity == null)
            {
                otherEntity = this.Datasource.GetByKey<U>(otherKey);
            }

            IReferenceManager manager = this.Datasource as IReferenceManager;
            if (manager != null)
            {
                manager.CreateRef<T, U>(entity, otherEntity, navigationProperty);
            }
            else
            {
                Type t = entity.GetType();
                TypeAccessor accessor = accessorCache.GetOrAdd(t, p => TypeAccessor.Create(t));
                Type type = accessor.GetMembers().Single(p => p.Name.Equals(navigationProperty) == true).Type;
                if (typeof(ICollection<U>).IsAssignableFrom(type) == true)
                {
                    if (accessor[entity, navigationProperty] == null)
                    {
                        accessor[entity, navigationProperty] = Activator.CreateInstance(type);
                    }

                    ((ICollection<U>)accessor[entity, navigationProperty]).Add(otherEntity);
                }
                else
                {
                    accessor[entity, navigationProperty] = otherEntity;
                }
            }
        }

        /// <summary>
        /// Remove the value from the navigation property.
        /// </summary>
        /// <typeparam name="U">The type of the target.</typeparam>
        /// <param name="entity">The current entity.</param>
        /// <param name="navigationProperty">The property to assign.</param>
        protected void Remove<U>(T entity, string navigationProperty)
            where U : class
        {
            IReferenceManager manager = this.Datasource as IReferenceManager;
            U otherEntity = null;
            Type t = entity.GetType();
            TypeAccessor accessor = accessorCache.GetOrAdd(t, p => TypeAccessor.Create(t));
            Type type = accessor.GetMembers().Single(p => p.Name.Equals(navigationProperty) == true).Type;
            if (typeof(ICollection<U>).IsAssignableFrom(type) == true)
            {
                ODataPath path = this.Request.ODataProperties().Path;
                IEdmEntitySet otherSet = (EdmEntitySet)path.NavigationSource;
                IEdmEntityType otherType = this.Convert(path.EdmType);

                IEnumerable<KeyValuePair<string, object>> otherKeys = this.BuildKey(null, otherType);
                string otherSetName = this.Datasource.ModelType.Name + '.' + otherSet.Name;
                InfrastructureKey otherKey = new InfrastructureKey(otherSetName, otherKeys);

                otherEntity = this.Datasource.GetByKey<U>(otherKey);
                if (manager != null)
                {
                    manager.DeleteRef<T, U>(entity, otherEntity, navigationProperty);
                }
                else if (accessor[entity, navigationProperty] != null &&
                    ((ICollection<U>)accessor[entity, navigationProperty]).Count > 0)
                {
                    ((ICollection<U>)accessor[entity, navigationProperty]).Remove(otherEntity);
                }
            }
            else if (manager != null)
            {
                manager.DeleteRef<T, U>(entity, otherEntity, navigationProperty);
            }
            else
            {
                // This must be a race condition of some kind. If you try to debug this, it won't repro,
                // but if you just let it run once, the value will not get set.
                accessor[entity, navigationProperty] = default(U);
                while (accessor[entity, navigationProperty] != default(U))
                {
                    accessor[entity, navigationProperty] = default(U);
                }
            }
        }

        /// <summary>
        /// Handle query for derived types.
        /// </summary>
        /// <param name="path">The current odata path.</param>
        /// <returns>The action result.</returns>
        private IHttpActionResult HandleDerivedTypeQuery(ODataPath path)
        {
            IEdmEntitySet rootSet = (IEdmEntitySet)path.NavigationSource;
            IEdmType baseType = (IEdmType)rootSet.EntityType();
            IEdmType derivedType = ((IEdmCollectionType)path.EdmType).ElementType.Definition;

            Type clr = this.Datasource.ModelType.Assembly.GetType(baseType.FullTypeName());
            Type derivedClr = this.Datasource.ModelType.Assembly.GetType(derivedType.FullTypeName());

            KeyValuePair<Type, string> key = new KeyValuePair<Type, string>(derivedClr, "Derive");
            Type type = null;
            bool outerIsBase = false;
            if (typeof(T) == clr)
            {
                type = derivedClr;
                outerIsBase = true;
            }
            else
            {
                type = clr;
                outerIsBase = false;
            }

            MethodInfo generic = reflectionCache.GetOrAdd(
                key,
                p => this
                    .GetType()
                    .GetMethod("Derive", BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(type));

            return generic.Invoke(this, new object[] { outerIsBase }) as IHttpActionResult;
        }

        /// <summary>
        /// Build a key from a given set of values.
        /// </summary>
        /// <param name="key">The key to parse.</param>
        /// <param name="entityType">The entity type.</param>
        /// <returns>The strongly typed, structured key values.</returns>
        private IEnumerable<KeyValuePair<string, object>> BuildKey(
            string key,
            IEdmEntityType entityType)
        {
            if (string.IsNullOrEmpty(key) == true)
            {
                ODataPath path = this.Request.ODataProperties().Path;
                foreach (UriParser.ODataPathSegment segment in path.Segments)
                {
                    if (segment is UriParser.KeySegment)
                    {
                        key = InfrastructureKey.SerializeKeyValues(segment as UriParser.KeySegment);
                    }
                }
            }

            Dictionary<string, object> entityKey = new Dictionary<string, object>();
            IEnumerable<IEdmStructuralProperty> keys = entityType.Key();
            string[] values = key.Split(',');

            for (int i = 0; i < keys.Count(); i++)
            {
                IEdmStructuralProperty member = keys.ElementAt(i);
                string value = values[i];
                int pos = value.IndexOf('=');
                if (pos > 0)
                {
                    value = value.Substring(pos + 1);
                }

                // These are the data types we support for keys.
                string fullTypeName = member.Type.Definition.FullTypeName();
                switch (fullTypeName)
                {
                    case "Edm.Int16":
                        entityKey.Add(member.Name, short.Parse(value));
                        break;
                    case "Edm.Int32":
                        entityKey.Add(member.Name, int.Parse(value));
                        break;
                    case "Edm.Int64":
                        entityKey.Add(member.Name, long.Parse(value));
                        break;
                    case "Edm.Guid":
                        entityKey.Add(member.Name, Guid.Parse(value));
                        break;
                    case "Edm.String":
                        string parsed = value;
                        if (value.StartsWith("'") && value.EndsWith("'"))
                        {
                            parsed = value.Substring(1, value.Length - 2);
                        }

                        entityKey.Add(member.Name, parsed);
                        break;
                    case "Edm.DateTime":
                        entityKey.Add(member.Name, DateTime.Parse(value));
                        break;
                    case "Edm.DateTimeOffset":
                        entityKey.Add(member.Name, DateTimeOffset.Parse(value));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return entityKey;
        }

        /// <summary>
        /// Convert an IEdmType into an IEdmEntityType.
        /// </summary>
        /// <param name="edmType">The IEdmType instance.</param>
        /// <returns>The converted/nested IEdmEntityType.</returns>
        private IEdmEntityType Convert(IEdmType edmType)
        {
            IEdmEntityType entityType;
            if (edmType.TypeKind == EdmTypeKind.Collection)
            {
                IEdmCollectionType collectionType = (IEdmCollectionType)edmType;
                entityType = (IEdmEntityType)collectionType.ElementType.Definition;
            }
            else
            {
                entityType = (IEdmEntityType)edmType;
            }

            return entityType;
        }

        /// <summary>
        /// Read an entity from the datasource.
        /// </summary>
        /// <param name="entitySet">The entity set where the entity lives.</param>
        /// <param name="key">The key data.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>The instance.</returns>
        private T ReadEntity(EdmEntitySet entitySet, string key, IEdmEntityType entityType)
        {
            string entitySetName = this.Datasource.ModelType.Name + '.' + entitySet.Name;
            InfrastructureKey entityKey = new InfrastructureKey(entitySetName, this.BuildKey(key, entityType));

            T entity = null;
            object map;
            bool hassource = this.Request.Properties.TryGetValue("ContentIdMapping", out map);
            Dictionary<string, string> source = map as Dictionary<string, string>;
            if (source != null)
            {
                hassource = source.Count > 0;
            }

            int value;
            if (this.Datasource.CurrentTransaction != null && hassource &&
                int.TryParse(source.Keys.First().ToString(), out value) == true)
            {
                entity = this.Datasource.CurrentTransaction.GetObject(value) as T;
            }

            if (entity == null)
            {
                entity = this.Datasource.GetByKey<T>(entityKey);
            }

            return entity;
        }

        /// <summary>
        /// Parse the operation name from the odata path.
        /// </summary>
        /// <returns>The operation name.</returns>
        private ParsedOperation ParseOperation()
        {
            ODataPath path = this.Request.ODataProperties().Path;
            ParsedOperation operation = new ParsedOperation(
                this.Model,
                this.Datasource.ModelType,
                path);

            return operation;
        }

        /// <summary>
        /// Extracts the parameters from the uri and adds them to the parameters collection.
        /// </summary>
        /// <param name="parameters">The parameter collection to extend.</param>
        private void ExtractParametersFromUri(ref ODataActionParameters parameters)
        {
            Uri uri = this.Request.RequestUri;
            parameters = parameters ?? new ODataActionParameters();

            string pattern = @"(?<=\()(.*?)(?=\))";
            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(uri.LocalPath);
            if (matches.Count > 0)
            {
                Match match = matches[matches.Count - 1];
                if (match.Value.Length > 0)
                {
                    string[] tokens = match.Value.Split(',');
                    foreach (string token in tokens)
                    {
                        string[] pair = token.Split('=');
                        if (pair.Count() == 2)
                        {
                            parameters.Add(pair[0], pair[1]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate that the function can be supported using the generic controller.
        /// </summary>
        /// <param name="operation">The parsed operation.</param>
        private void ValidateFunction(ParsedOperation operation)
        {
            string thisreturn = typeof(T).FullName;
            string opreturn = operation.ReturnTypeName;
            if (string.IsNullOrEmpty(opreturn) == true)
            {
                return;
            }

            Type optype = this.Datasource.ModelType.Assembly.GetType(opreturn);
            Type thistype = this.Datasource.ModelType.Assembly.GetType(thisreturn);

            bool derivedType = false;
            if (optype != null && thistype != null && optype.IsSubclassOf(thistype) == true)
            {
                derivedType = true;
            }

            if (thisreturn.Equals(opreturn) == false && derivedType == false)
            {
                HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.NotImplemented);
                message.ReasonPhrase = UnsupportedFunctionMessage;
                throw new HttpResponseException(message);
            }
        }

        /// <summary>
        /// Get the target based off a given key.
        /// </summary>
        /// <param name="entitySet">The entity set for the key based entity.</param>
        /// <param name="entityKey">The key of the entity to start from.</param>
        /// <param name="propertyname">The name of the navigational property.</param>
        /// <returns>The result.</returns>
        private IHttpActionResult GetFrom(string key)
        {
            ODataPath path = this.Request.ODataProperties().Path;
            EdmEntitySet rootSet = path.Segments[0].GetNavigationSource(null) as EdmEntitySet;
            IEdmEntityType root = this.Convert(path.Segments[0].EdmType);
            this.VerifyRead(rootSet);

            string secondary = null;
            string propertyName = path.Segments.Last().Identifier;
            if (path.Segments.Last().Identifier.Equals("$value") == true ||
                path.Segments.Last() is UriParser.TypeSegment ||
                path.PathTemplate.EndsWith("navigation/property") == true)
            {
                if (path.PathTemplate.EndsWith("navigation/property/$value") == true)
                {
                    propertyName = path.Segments[path.Segments.Count - 3].Identifier;
                    secondary = path.Segments[path.Segments.Count - 2].Identifier;
                }
                else if (path.PathTemplate.EndsWith("navigation/property") == true)
                {
                    propertyName = path.Segments[path.Segments.Count - 2].Identifier;
                    secondary = path.Segments[path.Segments.Count - 1].Identifier;
                }
                else
                {
                    propertyName = path.Segments[path.Segments.Count - 2].Identifier;
                }
            }

            T entity = this.ReadEntity(rootSet, key, root);
            Type t = entity.GetType();
            TypeAccessor accessor = accessorCache.GetOrAdd(t, p => TypeAccessor.Create(t));
            object value = accessor[entity, propertyName];

            Type propertyType = accessor.GetMembers().Single(p => p.Name.Equals(propertyName) == true).Type;
            if (secondary != null)
            {
                PropertyInfo pi = propertyType.GetProperty(secondary);
                value = value == null ? value : TypeCache.GetValue(propertyType, secondary, value);
                propertyType = pi.PropertyType;
            }

            KeyValuePair<Type, string> reflectionKey = new KeyValuePair<Type, string>(propertyType, "Cast");
            MethodInfo generic = reflectionCache.GetOrAdd(
                reflectionKey,
                p => this
                    .GetType()
                    .GetMethod("Cast", BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(propertyType));

            IHttpActionResult result = generic.Invoke(this, new object[] { value }) as IHttpActionResult;

            return result;
        }

        /// <summary>
        /// Construct a Uri for the given entity key in the given entity set.
        /// </summary>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entityKey">The entity key.</param>
        /// <returns>The constructed uri.</returns>
        private Uri ConstructUri(string entitySetName, InfrastructureKey entityKey)
        {
            string[] tokens = this.Request.RequestUri.Segments;
            string separ = string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append(this.Request.RequestUri.GetLeftPart(UriPartial.Authority));
            builder.Append(tokens[0] + tokens[1]);
            builder.Append(entitySetName);
            builder.Append("(");
            foreach (KeyValuePair<string, object> member in entityKey.EntityKeyValues)
            {
                string item = member.Key + '=' + this.Format(member.Value);
                builder.Append(separ);
                builder.Append(item);
                separ = ",";
            }

            builder.Append(")");

            return new Uri(builder.ToString());
        }

        /// <summary>
        /// Format the value for serialization.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The serialized value.</returns>
        private string Format(object value)
        {
            if (value.GetType() == typeof(int) ||
                value.GetType() == typeof(long))
            {
                return value.ToString();
            }
            else
            {
                return "'" + value.ToString() + "'";
            }
        }

        /// <summary>
        /// Invoke an operation.
        /// </summary>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        private IHttpActionResult InvokeOperation(ODataActionParameters parameters)
        {
            ParsedOperation operation = this.ParseOperation();
            operation.ConfigureParameters(parameters ?? new ODataActionParameters());
            if (operation.AllowsReturnData == false)
            {
                int content = this.Datasource.Execute(operation);

                return this.Ok(content);
            }
            else
            {
                this.ValidateFunction(operation);
                IQueryable<T> content = this.Datasource.Execute<T>(operation);

                return this.Ok(content);
            }
        }

        /// <summary>
        /// Invoke an operation with a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="parameters">The parameters sent with the call.</param>
        /// <returns>The result.</returns>
        private IHttpActionResult InvokeKeyOperation(
            [FromODataUri]string key,
            ODataActionParameters parameters)
        {
            ParsedOperation operation = this.ParseOperation();
            operation.ConfigureParameters(parameters ?? new ODataActionParameters());
            operation.Key = new InfrastructureKey(
                this.Datasource.ModelType.Name + '.' + operation.EntitySet.Name,
                this.BuildKey(key, operation.EntitySet.EntityType()));

            if (operation.AllowsReturnData == false)
            {
                int content = this.Datasource.Execute(operation);

                return this.Ok(content);
            }
            else
            {
                this.ValidateFunction(operation);
                IQueryable<T> content = this.Datasource.Execute<T>(operation);

                return this.Ok(content);
            }
        }

        /// <summary>
        /// Configure the datasource for batch requests.
        /// </summary>
        private void ConfigureDatasource()
        {
            if (this.Request.Properties.ContainsKey("MS_BatchRequest") == true &&
                this.Request.Properties.ContainsKey("ChangesetId") == true)
            {
                object batchRequest = this.Request.Properties["MS_BatchRequest"];
                if (batchRequest != null && ((bool)batchRequest) == true)
                {
                    this.Datasource = this.Request.Properties["Batch_DbContext"] as IDatasource;
                    if (this.Datasource == null)
                    {
                        throw new InvalidOperationException("Metadata Providers that are not datasources must include custom controllers.");
                    }

                    IMessageProcessor processor = this.Datasource as IMessageProcessor;
                    if (processor != null)
                    {
                        processor.Message = this.Request;
                    }
                }
            }
        }

        /// <summary>
        /// Get the keys from a provided Uri.
        /// </summary>
        /// <param name="uri">The uri to inspect.</param>
        /// <param name="entitySet">The relevant entityset.</param>
        /// <returns>The list of keys.</returns>
        private IEnumerable<KeyValuePair<string, object>> GetKeyValue(Uri uri, out IEdmEntitySet entitySet)
        {
            // Calculate root Uri
            entitySet = null;
            string rootPath = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.LastIndexOf('/') + 1);

            UriParser.ODataUriParser odataUriParser = new UriParser.ODataUriParser(this.Model, new Uri(rootPath), uri);
            UriParser.ODataPath odataPath = odataUriParser.ParsePath();
            UriParser.KeySegment keySegment = odataPath.LastSegment as UriParser.KeySegment;

            foreach (UriParser.ODataPathSegment segment in odataPath)
            {
                if (segment is UriParser.EntitySetSegment)
                {
                    UriParser.EntitySetSegment ess = segment as UriParser.EntitySetSegment;
                    entitySet = ess.EntitySet;
                }
            }

            if (keySegment == null)
            {
                throw new InvalidOperationException("The link does not contain a key.");
            }

            return keySegment.Keys;
        }

        /// <summary>
        /// Verify the datasource configuration for the current method.
        /// </summary>
        /// <param name="entitySet">The current entity set.</param>
        private void VerifyWrite(IEdmEntitySet entitySet)
        {
            InfrastructureConfigType config = this.Datasource.GetConfig();
            if (config != null &&
                config.Access != null &&
                config.Access.WriteEnabled == true &&
                config.Access.EntitySets != null)
            {
                foreach (EntitySetAccessType es in config.Access.EntitySets)
                {
                    if ((es.Name == "*" || es.Name == entitySet.Name) &&
                        (es.Access == EntitySetAccessibility.All ||
                        es.Access == EntitySetAccessibility.AllWrite))
                    {
                        return;
                    }
                }
            }

            throw new InvalidOperationException("Write Access not allowed.");
        }

        /// <summary>
        /// Verify the datasource configuration for the current method.
        /// </summary>
        /// <param name="entitySet">The current entity set.</param>
        private void VerifyRead(IEdmEntitySet entitySet)
        {
            InfrastructureConfigType config = this.Datasource.GetConfig();
            if (config == null || config.Access == null || config.Access.EntitySets == null)
            {
                return;
            }

            foreach (EntitySetAccessType es in config.Access.EntitySets)
            {
                if ((es.Name == "*" || es.Name == entitySet.Name) &&
                    (es.Access == EntitySetAccessibility.All ||
                    es.Access == EntitySetAccessibility.AllRead))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Read Access not allowed.");
        }

        /// <summary>
        /// Extract the references from the posted entity.
        /// </summary>
        /// <param name="entity">The posted entity.</param>
        private void ExtractReferences(T entity)
        {
            System.Threading.Tasks.Task<string> body = this.Request.Content.ReadAsStringAsync();
            if (body != null && string.IsNullOrEmpty(body.Result) == false)
            {
                JObject d = JObject.Parse(body.Result);
                foreach (JToken token in d.Children())
                {
                    JProperty jp = token as JProperty;
                    if (jp != null && jp.Name.EndsWith("@odata.bind") == true)
                    {
                        string[] pair = jp.Name.Split('@');
                        string name = pair[0];
                        JArray array = token.First as JArray;
                        if (array != null)
                        {
                            foreach (JToken item in array.Children())
                            {
                                string link = item.ToString();
                                this.AssignReference(entity, name, new Uri(link));
                            }
                        }
                        else
                        {
                            string link = token.First.ToString();
                            this.AssignReference(entity, name, new Uri(link));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Assign the reference to an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="navigationProperty">The property to assign.</param>
        /// <param name="link">The entity to be assigned.</param>
        private void AssignReference(T entity, string navigationProperty, Uri link)
        {
            IEdmEntitySet value;
            IEnumerable<KeyValuePair<string, object>> keys = this.GetKeyValue(link, out value);
            string name = this.Datasource.ModelType.Name + '.' + value.Name;
            InfrastructureKey otherKey = new InfrastructureKey(name, keys);
            IEdmEntityType entityType = this.Convert(value.EntityType());
            Type type = this.Datasource.ModelType.Assembly.GetType(entityType.FullTypeName());

            KeyValuePair<Type, string> reflectionKey = new KeyValuePair<Type, string>(type, "Assign");
            MethodInfo generic = reflectionCache.GetOrAdd(
                reflectionKey,
                p => this
                    .GetType()
                    .GetMethod("Assign", BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(type));

            generic.Invoke(this, new object[] { otherKey, entity, navigationProperty });
        }

        /// <summary>
        /// Handle the exception.
        /// </summary>
        /// <param name="e">The exception to handle.</param>
        /// <returns>The action result.</returns>
        private IHttpActionResult Handle(Exception e)
        {
            if (this.ErrorHandler != null)
            {
                return this.ErrorHandler.Handle(e, this);
            }

            return null;
        }
    }
}
