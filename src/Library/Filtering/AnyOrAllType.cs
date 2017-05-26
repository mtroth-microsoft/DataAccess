// -----------------------------------------------------------------------
// <copyright file="AnyOrAllType.cs" company="Lensgrinder, Ltd.">
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
    /// Corresponds to AnyOrAllType in model.
    /// </summary>
    public abstract partial class AnyOrAllType
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
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal override void Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
            }

            // move into the subtree.
            if (reader.Read() == true)
            {
                this.Name = reader.GetAttribute("Name");
                this.Value = XmlConvert.ToBoolean(reader.GetAttribute("Value"));
                int pos = this.Name.LastIndexOf('/');
                if (pos > 0)
                {
                    this.Prefix = this.Name.Substring(0, pos);
                    this.Name = this.Name.Substring(pos + 1);
                }

                while (reader.Read() == true)
                {
                    if (reader.IsStartElement() == true)
                    {
                        ExpressionType expression = ExpressionType.Create(reader.LocalName);
                        if (expression != null)
                        {
                            expression.Deserialize(reader.ReadSubtree());
                            this.Item = expression;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Populate the parameters, converting if necessary.
        /// </summary>
        /// <param name="parameters">The assigned parameters.</param>
        /// <returns>The ortype instance resulting from conversion.</returns>
        internal override ConditionType Convert(Dictionary<string, object> parameters)
        {
            ConditionType condition = this.Item.Convert(parameters);
            if (condition != null)
            {
                this.Item = condition;
            }

            return null;
        }

        /// <summary>
        /// Set the prefixes on all the property name in the clause.
        /// </summary>
        /// <param name="prefix">The prefix to set.</param>
        internal override void SetPrefixes(string prefix)
        {
            this.Item.SetPrefixes(prefix);
        }

        /// <summary>
        /// Deep copy the current instance.
        /// </summary>
        /// <returns>The copy.</returns>
        internal override ExpressionType DeepCopy()
        {
            AnyOrAllType copy = Activator.CreateInstance(this.GetType()) as AnyOrAllType;
            copy.Alias = this.Alias;
            copy.ElementType = this.ElementType;
            copy.Name = this.Name;
            copy.Prefix = this.Prefix;
            copy.Value = this.Value;
            if (this.Item != null)
            {
                copy.Item = this.Item.DeepCopy();
            }

            return copy;
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <param name="searchType">Any or all search type.</param>
        /// <returns>The serialized string.</returns>
        protected string SerializeCollectionSearch(string searchType)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.Name);
            builder.Append("/");
            builder.Append(searchType);
            builder.Append("(o:");
            this.Item.SetPrefixes("o");

            builder.Append(FilterType.SerializePredicateGroup(this.Item));

            builder.Append(") eq ");
            builder.Append(this.Value.ToString().ToLower());

            return builder.ToString();
        }
    }
}
