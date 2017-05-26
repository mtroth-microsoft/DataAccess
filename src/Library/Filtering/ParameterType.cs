// -----------------------------------------------------------------------
// <copyright file="ParameterType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to ParameterType in model.
    /// </summary>
    public partial class ParameterType : IPredicatable
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        public void Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
            }

            // move into subtree.
            if (reader.Read() == true)
            {
                reader.Read();
                this.Value = reader.Value;
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringBuilder query = new StringBuilder();
            query.Append(this.Value);

            return query.ToString();
        }

        /// <summary>
        /// Lookup the property name associated with the predicatable.
        /// </summary>
        /// <returns>The property name.</returns>
        public string LookupPropertyName()
        {
            return this.Value;
        }

        /// <summary>
        /// Lookup the value associated with the predicatable.
        /// </summary>
        /// <param name="statement">The containing predicate.</param>
        /// <returns>The associated value.</returns>
        public object LookupPropertyValue(PredicateType statement)
        {
            return statement.FindValue();
        }

        /// <summary>
        /// Copy this into new instance.
        /// </summary>
        /// <returns>The new instance.</returns>
        IPredicatable IPredicatable.Copy()
        {
            ParameterType copy = new ParameterType();
            copy.Value = this.Value;

            return copy;
        }

        /// <summary>
        /// Locate the list of property names.
        /// </summary>
        /// <returns>The list of property names.</returns>
        List<PropertyNameType> IPredicatable.LocatePropertyNames()
        {
            return new List<PropertyNameType>();
        }
    }
}
