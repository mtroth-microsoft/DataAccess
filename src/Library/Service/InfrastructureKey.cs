// -----------------------------------------------------------------------
// <copyright file="InfrastructureKey.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.OData.UriParser;

    /// <summary>
    /// Class to contain the key data for an entity.
    /// </summary>
    public class InfrastructureKey
    {
        /// <summary>
        /// Initializes a new instance of the InfrastructureKey class.
        /// </summary>
        /// <param name="qualifiedEntitySetName">The qualified name of the entity set.</param>
        /// <param name="keys">The list of key value pairs.</param>
        public InfrastructureKey(
            string qualifiedEntitySetName, 
            IEnumerable<KeyValuePair<string, object>> keys)
        {
            this.QualifiedEntitySetName = qualifiedEntitySetName;
            this.EntityKeyValues = keys;
        }

        /// <summary>
        /// Gets the qualified name of the entity set.
        /// </summary>
        public string QualifiedEntitySetName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of key value pairs.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> EntityKeyValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Serialize the key segment into the correct value string.
        /// </summary>
        /// <param name="keySegment">The key segment.</param>
        /// <returns>The serialized key segment.</returns>
        public static string SerializeKeyValues(KeySegment keySegment)
        {
            string separator = string.Empty;
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, object> pair in keySegment.Keys)
            {
                builder.Append(separator);
                builder.Append(pair.Key);
                builder.Append("=");
                builder.Append(pair.Value.ToString());
                separator = ",";
            }

            return builder.ToString();
        }
    }
}
