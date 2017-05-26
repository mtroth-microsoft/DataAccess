// -----------------------------------------------------------------------
// <copyright file="PropertyNameType.cs" company="Lensgrinder, Ltd.">
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
    /// Corresponds to PropertyNameType in model.
    /// </summary>
    public partial class PropertyNameType : IPredicatable
    {
        /// <summary>
        /// Gets or sets an alias for the parent of the property.
        /// </summary>
        internal string Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a context dependent prefix to the property name.
        /// </summary>
        internal string Prefix
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the containing type of the collection property.
        /// </summary>
        internal Type ElementType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the aggregate column reference underlying this property name, if applicable.
        /// </summary>
        internal AggregateColumnReference AggregateColumnReference
        {
            get;
            set;
        }

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
                int pos = reader.Value.LastIndexOf('/');
                if (pos > 0)
                {
                    this.Prefix = reader.Value.Substring(0, pos);
                    this.Value = reader.Value.Substring(pos + 1);
                }
                else
                {
                    this.Value = reader.Value;
                }
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringBuilder query = new StringBuilder();
            if (string.IsNullOrEmpty(this.Prefix) == false)
            {
                query.AppendFormat("{0}/", this.Prefix);
            }

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
            PropertyNameType copy = new PropertyNameType();
            copy.Prefix = this.Prefix;
            copy.Value = this.Value;
            copy.Alias = this.Alias;
            copy.ElementType = this.ElementType;
            copy.AggregateColumnReference = this.AggregateColumnReference;

            return copy;
        }

        /// <summary>
        /// Locate the list of property names.
        /// </summary>
        /// <returns>The list of property names.</returns>
        List<PropertyNameType> IPredicatable.LocatePropertyNames()
        {
            return new List<PropertyNameType>() { this };
        }
    }
}
