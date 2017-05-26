// -----------------------------------------------------------------------
// <copyright file="EntityKeyType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// Corresponds to EntityKeyType in model.
    /// </summary>
    public partial class EntityKeyType
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
                this.Items = new List<object>();
                while (reader.Read() == true)
                {
                    if (reader.IsStartElement() == true &&
                        reader.LocalName == "Equals")
                    {
                        EqualType et = new EqualType();
                        et.Deserialize(reader.ReadSubtree());
                        this.Items.Add(et);
                    }
                    else if (reader.IsStartElement() == true)
                    {
                        object value = PredicateType.DeserializeValue(reader);
                        if (value != null)
                        {
                            this.Items.Add(value);
                        }
                    }
                }
            }
        }
    }
}
