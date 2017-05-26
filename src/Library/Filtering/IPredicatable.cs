// -----------------------------------------------------------------------
// <copyright file="IPredicatable.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// Helper interface to associate Predicatable types.
    /// </summary>
    internal interface IPredicatable
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        void Deserialize(XmlReader reader);

        /// <summary>
        /// Serialize the current into the left or right side of a predicate.
        /// </summary>
        /// <returns>The serialized value.</returns>
        string Serialize();

        /// <summary>
        /// Deep copy this into new instance.
        /// </summary>
        /// <returns>The new instance.</returns>
        IPredicatable Copy();

        /// <summary>
        /// Locate the nested property names in the predicatable.
        /// </summary>
        /// <returns>The list of property names.</returns>
        List<PropertyNameType> LocatePropertyNames();

        /// <summary>
        /// Lookup the property name associated with the predicatable.
        /// </summary>
        /// <returns>The property name.</returns>
        string LookupPropertyName();

        /// <summary>
        /// Lookup the value associated with the predicatable.
        /// </summary>
        /// <param name="statement">The containing predicate.</param>
        /// <returns>The associated value.</returns>
        object LookupPropertyValue(PredicateType statement);
    }
}
