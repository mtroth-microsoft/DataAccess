// -----------------------------------------------------------------------
// <copyright file="FunctionType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Corresponds to FunctionType in model.
    /// </summary>
    public partial class FunctionType : IPredicatable
    {
        /// <summary>
        /// Gets the name of the function.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of arguments.
        /// </summary>
        public List<object> Arguments
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether to negate the function.
        /// </summary>
        public bool Negate
        {
            get;
            private set;
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
                this.Value = reader.Value;
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns>The serialized function.</returns>
        public string Serialize()
        {
            StringBuilder query = new StringBuilder();
            query.Append(this.Value);

            return query.ToString();
        }

        /// <summary>
        /// Set the prefix on the first nested property.
        /// </summary>
        /// <param name="prefix">The prefix to set.</param>
        public void SetPrefix(string prefix)
        {
            List<PropertyNameType> propertyNames = ((IPredicatable)this).LocatePropertyNames();
            foreach (PropertyNameType propertyName in propertyNames)
            {
                propertyName.Prefix = prefix;
            }
        }

        /// <summary>
        /// Lookup the property name associated with the predicatable.
        /// </summary>
        /// <returns>The property name.</returns>
        public string LookupPropertyName()
        {
            List<PropertyNameType> propertyNames = ((IPredicatable)this).LocatePropertyNames();
            if (propertyNames.Count > 0)
            {
                return propertyNames[0].Value;
            }

            return this.Name;
        }

        /// <summary>
        /// Lookup the value associated with the predicatable.
        /// </summary>
        /// <param name="statement">The containing predicate.</param>
        /// <returns>The associated value.</returns>
        public object LookupPropertyValue(PredicateType statement)
        {
            if (this.Name.Equals("Contains", StringComparison.OrdinalIgnoreCase) == true)
            {
                return this.Arguments[1];
            }

            return this.Arguments;
        }

        /// <summary>
        /// Copy this into new instance.
        /// </summary>
        /// <returns>The new instance.</returns>
        IPredicatable IPredicatable.Copy()
        {
            FunctionType copy = new FunctionType();
            copy.Value = this.Value;

            return copy;
        }

        /// <summary>
        /// Locate the property names nested in the function.
        /// </summary>
        /// <returns>The list of located property names.</returns>
        List<PropertyNameType> IPredicatable.LocatePropertyNames()
        {
            return PredicateType.ExtractPropertyNames(this.Arguments);
        }

        /// <summary>
        /// Replace any parameters in the function with values.
        /// </summary>
        /// <param name="parameters">The parameter map to use.</param>
        internal void Replace(Dictionary<string, object> parameters)
        {
            foreach (string key in parameters.Keys)
            {
                this.Value = this.Value.Replace(
                    key,
                    PredicateType.SerializeValue(parameters[key]));
            }
        }

        /// <summary>
        /// Parse the function body into structured content.
        /// </summary>
        private void Parse()
        {
            FunctionHelper helper = new FunctionHelper(this.Value);
            this.Name = helper.Name;
            this.Arguments = new List<object>(helper.Arguments);
            this.Negate = helper.Negate;
        }
    }
}
