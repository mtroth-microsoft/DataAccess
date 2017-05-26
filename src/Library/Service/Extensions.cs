// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Lensgrinder">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.OData.Routing;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;

    /// <summary>
    /// Helper class for extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Converts a null IEnumerable reference to an empty sequence.
        /// </summary>
        internal static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> elements)
        {
            return elements ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Extension to provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        internal static IEdmNavigationSource GetNavigationSource(
            this ODataPathSegment segment, 
            IEdmNavigationSource source)
        {
            switch (segment.GetType().Name)
            {
                case "BatchSegment":
                    return GetNavigationSourceEx(segment as BatchSegment, source);
                case "BatchReferenceSegment":
                    return GetNavigationSourceEx(segment as BatchReferenceSegment, source);
                case "CountSegment":
                    return GetNavigationSourceEx(segment as CountSegment, source);
                case "DynamicPathSegment":
                    return GetNavigationSourceEx(segment as DynamicPathSegment, source);
                case "EntitySetSegment":
                    return GetNavigationSourceEx(segment as EntitySetSegment, source);
                case "KeySegment":
                    return GetNavigationSourceEx(segment as KeySegment, source);
                case "MetadataSegment":
                    return GetNavigationSourceEx(segment as MetadataSegment, source);
                case "NavigationPropertyLinkSegment":
                    return GetNavigationSourceEx(segment as NavigationPropertyLinkSegment, source);
                case "NavigationPropertySegment":
                    return GetNavigationSourceEx(segment as NavigationPropertySegment, source);
                case "OperationImportSegment":
                    return GetNavigationSourceEx(segment as OperationImportSegment, source);
                case "OperationSegment":
                    return GetNavigationSourceEx(segment as OperationSegment, source);
                case "PathTemplateSegment":
                    return GetNavigationSourceEx(segment as PathTemplateSegment, source);
                case "PropertySegment":
                    return GetNavigationSourceEx(segment as PropertySegment, source);
                case "SingletonSegment":
                    return GetNavigationSourceEx(segment as SingletonSegment, source);
                case "TypeSegment":
                    return GetNavigationSourceEx(segment as TypeSegment, source);
                case "UnresolvedPathSegment":
                    return GetNavigationSourceEx(segment as UnresolvedPathSegment, source);
                case "ValueSegment":
                    return GetNavigationSourceEx(segment as ValueSegment, source);
            }

            throw new ArgumentOutOfRangeException(segment.GetType().Name);
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(BatchSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(BatchReferenceSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(CountSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(DynamicPathSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(EntitySetSegment segment, IEdmNavigationSource source)
        {
            return segment.EntitySet as EdmEntitySet;
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(KeySegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(MetadataSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(NavigationPropertyLinkSegment segment, IEdmNavigationSource source)
        {
            return segment.NavigationSource;
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(NavigationPropertySegment segment, IEdmNavigationSource source)
        {
            return segment.NavigationSource;
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(OperationImportSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(OperationSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(PathTemplateSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(PropertySegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(SingletonSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(TypeSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(UnresolvedPathSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provide legacy support for ODataPathSegment navigation source.
        /// </summary>
        /// <param name="segment">The segment to inspect.</param>
        /// <param name="source">The source to get the source for, null if self.</param>
        /// <returns>The navigational source of the segment.</returns>
        private static IEdmNavigationSource GetNavigationSourceEx(ValueSegment segment, IEdmNavigationSource source)
        {
            throw new NotSupportedException();
        }
    }
}
