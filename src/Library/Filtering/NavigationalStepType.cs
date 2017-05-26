// -----------------------------------------------------------------------
// <copyright file="NavigationalStepType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to NavigationalStepType in model.
    /// </summary>
    public partial class NavigationalStepType
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal virtual void Deserialize(XmlReader reader)
        {
            this.Deserialize(reader, null);
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal virtual string Serialize()
        {
            return this.Serialize(null);
        }

        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        /// <param name="action">Callback for additional nodes.</param>
        protected void Deserialize(XmlReader reader, Action<XmlReader> action)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
            }

            // move into the subtree.
            if (reader.Read() == true)
            {
                this.Name = reader.GetAttribute("Name");
                while (reader.Read() == true)
                {
                    if (reader.IsStartElement() == true)
                    {
                        switch (reader.LocalName)
                        {
                            case "Key":
                                this.Key = new EntityKeyType();
                                this.Key.Deserialize(reader.ReadSubtree());
                                break;

                            case "NavigationalStep":
                                this.NavigationalStep = new NavigationalStepType();
                                this.NavigationalStep.Deserialize(reader.ReadSubtree());
                                break;

                            default:
                                if (action != null)
                                {
                                    action(reader);
                                }

                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Serialize this instance, inserting a subtype.
        /// </summary>
        /// <param name="subtype">The subtype to insert.</param>
        /// <returns>The serialized string.</returns>
        protected string Serialize(string subtype)
        {
            StringBuilder query = new StringBuilder();
            query.AppendFormat("/{0}", this.Name);

            if (string.IsNullOrEmpty(subtype) == false)
            {
                query.Append("/");
                query.Append(subtype);
            }

            if (this.Key != null)
            {
                if (this.Key.Items.Count > 0)
                {
                    query.Append("(");
                }

                string token = string.Empty;
                foreach (object key in this.Key.Items)
                {
                    query.Append(token);
                    if (key is EqualType)
                    {
                        string value = (key as EqualType).Serialize();
                        value = value.Replace("' eq ", "=");
                        query.Append(value.Substring(1));
                    }
                    else
                    {
                        query.Append(PredicateType.SerializeValue(key));
                    }

                    token = ",";
                }

                if (this.Key.Items.Count > 0)
                {
                    query.Append(")");
                }
            }

            if (this.NavigationalStep != null)
            {
                query.Append(this.NavigationalStep.Serialize());
            }

            return query.ToString();
        }
    }
}
