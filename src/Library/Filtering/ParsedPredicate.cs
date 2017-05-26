// -----------------------------------------------------------------------
// <copyright file="ParsedPredicate.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Collections.Generic;

    /// <summary>
    /// Helper class to contain data about a predicate.
    /// </summary>
    internal sealed class ParsedPredicate
    {
        /// <summary>
        /// Initializes a new instance of the ParsedPredicate class.
        /// </summary>
        public ParsedPredicate()
        {
            this.PropertyNames = new List<PropertyNameType>();
        }

        /// <summary>
        /// Gets the item that has been parsed.
        /// </summary>
        public PredicateType Item
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the list of property names.
        /// </summary>
        public List<PropertyNameType> PropertyNames
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the value associated with the predicate.
        /// </summary>
        public object Value
        {
            get;
            internal set;
        }
    }
}
