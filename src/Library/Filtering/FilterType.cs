// -----------------------------------------------------------------------
// <copyright file="FilterType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Corresponds to FilterType in model.
    /// </summary>
    public partial class FilterType
    {
        /// <summary>
        /// Merge multiple filters together into a single and clause.
        /// </summary>
        /// <param name="filters">The filters to merge.</param>
        /// <returns>The resulting filter.</returns>
        internal static FilterType Merge(params FilterType[] filters)
        {
            IEnumerable<FilterType> filtersToMerge = filters.Where(p => p != null);
            if (filtersToMerge.Count() == 1)
            {
                return filtersToMerge.Single();
            }
            else if (filtersToMerge.Count() == 0)
            {
                return null;
            }
            else
            {
                AndType and = new AndType();
                foreach (FilterType filter in filtersToMerge)
                {
                    AndType inner = filter.Item as AndType;
                    if (inner != null)
                    {
                        foreach (ExpressionType item in inner.Items)
                        {
                            and.Items.Add(item);
                        }
                    }
                    else
                    {
                        and.Items.Add(filter.Item);
                    }
                }

                return new FilterType() { Item = and };
            }
        }

        /// <summary>
        /// Serializes a predicate group into the correct form.
        /// </summary>
        /// <param name="item">The predicate group instance.</param>
        /// <returns>The serialized string.</returns>
        internal static string SerializePredicateGroup(ExpressionType item)
        {
            string value = null;
            if (item != null)
            {
                value = item.Serialize();
            }

            return value;
        }

        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal virtual void Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
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

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal string Serialize()
        {
            string filter = SerializePredicateGroup(this.Item);
            if (string.IsNullOrEmpty(filter) == false)
            {
                filter = string.Format("$filter={0}", filter);
            }

            return filter;
        }

        /// <summary>
        /// Populate the parameters, converting if necessary.
        /// </summary>
        /// <param name="parameters">The assigned parameters.</param>
        internal void Convert(Dictionary<string, object> parameters)
        {
            if (this.Item != null)
            {
                ConditionType condition = this.Item.Convert(parameters);
                if (condition != null)
                {
                    this.Item = condition;
                }
            }
        }

        /// <summary>
        /// Deep copy the filter.
        /// </summary>
        /// <returns>The copy.</returns>
        internal FilterType DeepCopy()
        {
            FilterType copy = new FilterType();
            copy.Item = this.Item.DeepCopy();

            return copy;
        }
    }
}
