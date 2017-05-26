// -----------------------------------------------------------------------
// <copyright file="EntityType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to EntityType in model.
    /// </summary>
    public partial class EntityType
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal void Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
            }

            // move into the subtree.
            if (reader.Read() == true)
            {
                this.Model = reader.GetAttribute("Model");
                this.Name = reader.GetAttribute("Name");
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal string Serialize()
        {
            StringBuilder query = new StringBuilder();
            query.AppendFormat("{0}.{1}", this.LookupNamespace(), this.Name);

            return query.ToString();
        }

        /// <summary>
        /// Lookup the provided model's namespace.
        /// </summary>
        /// <returns>The namespace.</returns>
        private string LookupNamespace()
        {
            return this.Model;
        }
    }
}
