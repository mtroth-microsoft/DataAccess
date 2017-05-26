// -----------------------------------------------------------------------
// <copyright file="WithType.cs" company="Lensgrinder, Ltd.">
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
    /// Corresponds to WithType in model.
    /// </summary>
    public partial class WithType : IPredicatable
    {
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

            // move into the subtree.
            if (reader.Read() == true)
            {
                string aggName = reader.GetAttribute("AggregateType");
                AggregateType agg;
                if (string.IsNullOrEmpty(aggName) == false &&
                    Enum.TryParse<AggregateType>(aggName, out agg) == true)
                {
                    this.AggregateType = agg;
                }

                while (reader.Read() == true)
                {
                    if (reader.IsStartElement() == true)
                    {
                        IPredicatable predicatable = null;
                        switch (reader.LocalName)
                        {
                            case "PropertyName":
                                predicatable = new PropertyNameType();
                                break;

                            case "Function":
                                predicatable = new FunctionType();
                                break;

                            case "Parameter":
                                predicatable = new ParameterType();
                                break;

                            case "Add":
                                predicatable = new AddType();
                                break;

                            case "Mul":
                                predicatable = new MulType();
                                break;

                            case "Sub":
                                predicatable = new SubType();
                                break;

                            case "Div":
                                predicatable = new DivType();
                                break;

                            case "DivBy":
                                predicatable = new DivByType();
                                break;

                            case "Mod":
                                predicatable = new ModType();
                                break;

                            case "Sum":
                                this.AggregateType = AggregateType.Sum;
                                break;

                            case "Max":
                                this.AggregateType = AggregateType.Max;
                                break;

                            case "Min":
                                this.AggregateType = AggregateType.Min;
                                break;

                            case "Average":
                                this.AggregateType = AggregateType.Average;
                                break;

                            case "Count":
                                this.AggregateType = AggregateType.Count;
                                break;

                            case "CountDistinct":
                                this.AggregateType = AggregateType.CountDistinct;
                                break;

                            case "Merge":
                                this.AggregateType = AggregateType.Merge;
                                break;

                            case "None":
                                this.AggregateType = AggregateType.None;
                                break;
                        }

                        if (predicatable != null)
                        {
                            predicatable.Deserialize(reader.ReadSubtree());
                            this.Predicate = predicatable;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringBuilder builder = new StringBuilder();
            IPredicatable predicatable = this.Predicate as IPredicatable;
            if (predicatable == null)
            {
                builder.Append(PredicateType.SerializeValue(this.Predicate));
            }
            else
            {
                builder.Append(predicatable.Serialize());
            }

            builder.Append(" with ");
            builder.Append(this.AggregateType.ToString().ToLower());

            return builder.ToString();
        }

        /// <summary>
        /// Lookup the property name associated with the predicatable.
        /// </summary>
        /// <returns>The property name.</returns>
        public string LookupPropertyName()
        {
            throw new NotImplementedException();
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
            WithType copy = new WithType();
            copy.AggregateType = this.AggregateType;
            copy.Predicate = this.Predicate;
            IPredicatable predicatable = this.Predicate as IPredicatable;
            if (predicatable != null)
            {
                copy.Predicate = predicatable.Copy();
            }

            return copy;
        }

        /// <summary>
        /// Locate the list of property names.
        /// </summary>
        /// <returns>The list of property names.</returns>
        List<PropertyNameType> IPredicatable.LocatePropertyNames()
        {
            List<PropertyNameType> properties = new List<PropertyNameType>();
            IPredicatable predicatable = this.Predicate as IPredicatable;
            if (predicatable != null)
            {
                properties = predicatable.LocatePropertyNames();
            }

            return properties;
        }
    }
}
