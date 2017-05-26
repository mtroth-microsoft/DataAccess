// -----------------------------------------------------------------------
// <copyright file="ConditionType.cs" company="Lensgrinder, Ltd.">
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
    /// Corresponds to ConditionType in model.
    /// </summary>
    public abstract partial class ConditionType
    {
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
                this.Items = new List<ExpressionType>();
                while (reader.Read() == true)
                {
                    if (reader.IsStartElement() == true)
                    {
                        ExpressionType expression = ExpressionType.Create(reader.LocalName);
                        if (expression != null)
                        {
                            expression.Deserialize(reader.ReadSubtree());
                            this.Items.Add(expression);
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
            for (int i = 0; i < this.Items.Count; i++)
            {
                ConditionType condition = this.Items[i].Convert(parameters);
                if (condition != null)
                {
                    this.Items[i] = condition;
                }
            }

            return null;
        }

        /// <summary>
        /// Set the prefixes on all the property name in the clause.
        /// </summary>
        /// <param name="prefix">The prefix to set.</param>
        internal override void SetPrefixes(string prefix)
        {
            foreach (ExpressionType item in this.Items)
            {
                item.SetPrefixes(prefix);
            }
        }

        /// <summary>
        /// Deep copy the current instance.
        /// </summary>
        /// <returns>The copy.</returns>
        internal override ExpressionType DeepCopy()
        {
            ConditionType copy = Activator.CreateInstance(this.GetType()) as ConditionType;
            foreach (ExpressionType item in this.Items)
            {
                copy.Items.Add(item.DeepCopy());
            }

            return copy;
        }

        /// <summary>
        /// Serialize the condition given the provided conjunction.
        /// </summary>
        /// <param name="conjunctionType">The conjunction to use. And/Or</param>
        /// <returns>The serialized string.</returns>
        protected string SerializeCondition(string conjunctionType)
        {
            StringBuilder builder = new StringBuilder();
            string conjunction = string.Empty;

            builder.Append("(");
            foreach (ExpressionType item in this.Items)
            {
                string result = item.Serialize();
                builder.Append(conjunction);
                builder.Append(result);
                conjunction = conjunctionType;
            }

            builder.Append(")");
            return builder.ToString();
        }
    }
}
