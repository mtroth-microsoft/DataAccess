// -----------------------------------------------------------------------
// <copyright file="ExpandType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    public partial class ExpandType
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal void Deserialize(XmlReader reader)
        {
            reader.MoveToContent();
            string count = reader.GetAttribute("Count");
            if (string.IsNullOrEmpty(count) == false)
            {
                this.Count = bool.Parse(count);
            }

            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true)
                {
                    switch (reader.LocalName)
                    {
                        case "Property":
                            this.Property = new PropertyType();
                            this.Property.Name = reader.GetAttribute("Name");
                            break;

                        case "OrderBy":
                            this.OrderBy = new List<OrderedPropertyType>();
                            this.DeserializeOrderBy(reader.ReadSubtree());
                            break;

                        case "TopOrSkip":
                            this.TopOrSkip = new TopOrSkipType();
                            this.TopOrSkip.Deserialize(reader.ReadSubtree());
                            break;

                        case "Filter":
                            this.Filter = new FilterType();
                            this.Filter.Deserialize(reader.ReadSubtree());
                            break;

                        case "Select":
                            this.Select = new List<PropertyType>();
                            this.DeserializeSelects(reader.ReadSubtree());
                            break;

                        case "Expand":
                            this.Expand = new List<ExpandType>();
                            this.DeserializeExpands(reader.ReadSubtree());
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal string Serialize()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.Property.Name);

            if (this.Select.Count > 0 || this.Expand.Count > 0 || this.Filter != null || 
                this.OrderBy.Count > 0 || this.TopOrSkip.TopSpecified || this.TopOrSkip.SkipSpecified)
            {
                builder.Append("(");
            }

            string separator = string.Empty;
            string selects = this.SerializeSelects();
            if (string.IsNullOrEmpty(selects) == false)
            {
                builder.Append(separator);
                builder.Append(selects);
                separator = ";";
            }

            string expands = this.SerializeExpands();
            if (string.IsNullOrEmpty(expands) == false)
            {
                builder.Append(separator);
                builder.Append(expands);
                separator = ";";
            }

            if (this.Filter != null)
            {
                string filter = this.Filter.Serialize();
                builder.Append(separator);
                builder.Append(filter);
                separator = ";";
            }

            if (this.OrderBy.Count > 0)
            {
                string orderby = this.SerializeOrderBy();
                builder.Append(separator);
                builder.Append(orderby);
                separator = ";";
            }

            if (this.TopOrSkip.SkipSpecified || this.TopOrSkip.TopSpecified)
            {
                string topAndSkip = this.SerializeTopAndSkip();
                builder.Append(separator);
                builder.Append(topAndSkip);
                separator = ";";
            }

            if (this.Count == true)
            {
                builder.Append(separator);
                builder.Append("$count=true");
                separator = ";";
            }

            if (this.Select.Count > 0 || this.Expand.Count > 0 || this.Filter != null ||
                this.OrderBy.Count > 0 || this.TopOrSkip.TopSpecified || this.TopOrSkip.SkipSpecified)
            {
                builder.Append(")");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the selects.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeSelects()
        {
            StringBuilder selects = new StringBuilder();
            string token = string.Empty;
            foreach (PropertyType property in this.Select)
            {
                selects.Append(token);
                selects.Append(property.Name);
                token = ",";
            }

            if (this.Select.Count > 0)
            {
                selects.Insert(0, "$select=");
            }

            return selects.ToString();
        }

        /// <summary>
        /// Deserialize the selects.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeSelects(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Property")
                {
                    PropertyType p = new PropertyType();
                    p.Name = reader.GetAttribute("Name");
                    this.Select.Add(p);
                }
            }
        }

        /// <summary>
        /// Serialize the order by.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeOrderBy()
        {
            StringBuilder order = new StringBuilder();
            string token = string.Empty;
            foreach (OrderedPropertyType property in this.OrderBy)
            {
                order.Append(token);
                order.Append(property.Serialize());
                token = ",";
            }

            if (this.OrderBy.Count > 0)
            {
                order.Insert(0, "$orderby=");
            }

            return order.ToString();
        }

        /// <summary>
        /// Deserialize the order by.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeOrderBy(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Property")
                {
                    OrderedPropertyType p = new OrderedPropertyType();
                    p.Name = reader.GetAttribute("Name");
                    p.Ascending = XmlConvert.ToBoolean(reader.GetAttribute("Ascending"));
                    this.OrderBy.Add(p);
                }
            }
        }

        /// <summary>
        /// Serialize the expands.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeExpands()
        {
            StringBuilder expands = new StringBuilder();
            string token = string.Empty;
            foreach (ExpandType expand in this.Expand)
            {
                expands.Append(token);
                expands.Append(expand.Serialize());
                token = ",";
            }

            if (this.Expand.Count > 0)
            {
                expands.Insert(0, "$expand=");
            }

            return expands.ToString();
        }

        /// <summary>
        /// Deserialize the expands.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeExpands(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Expand")
                {
                    ExpandType expand = new ExpandType();
                    expand.Deserialize(reader.ReadSubtree());
                    this.Expand.Add(expand);
                }
            }
        }

        /// <summary>
        /// Serialize the top and skip.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeTopAndSkip()
        {
            StringBuilder topAndSkip = new StringBuilder();
            string token = string.Empty;
            if (this.TopOrSkip.TopSpecified == true)
            {
                topAndSkip.Append(token);
                topAndSkip.Append("$top=");
                topAndSkip.Append(this.TopOrSkip.Top);
                token = ";";
            }

            if (this.TopOrSkip.SkipSpecified == true)
            {
                topAndSkip.Append(token);
                topAndSkip.Append("$skip=");
                topAndSkip.Append(this.TopOrSkip.Skip);
            }

            return topAndSkip.ToString();
        }
    }
}
