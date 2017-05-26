// -----------------------------------------------------------------------
// <copyright file="ArithmeticType.cs" company="Lensgrinder, Ltd.">
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
    /// Corresponds to Arithmetic in model.
    /// </summary>
    public abstract partial class ArithmeticType : IPredicatable
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        public virtual void Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
            }

            // move into the subtree.
            if (reader.Read() == true)
            {
                while (reader.Read() == true)
                {
                    if (reader.IsStartElement() == true)
                    {
                        object value = null;
                        IPredicatable predicatable = null;
                        switch (reader.LocalName)
                        {
                            case "Add":
                                predicatable = new AddType();
                                break;

                            case "Sub":
                                predicatable = new SubType();
                                break;

                            case "Mul":
                                predicatable = new MulType();
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

                            case "PropertyName":
                                predicatable = new PropertyNameType();
                                break;

                            case "Function":
                                predicatable = new FunctionType();
                                break;

                            case "Parameter":
                                predicatable = new ParameterType();
                                break;

                            case "Null":
                                value = new NullType();
                                break;

                            case "With":
                                predicatable = new WithType();
                                break;

                            default:
                                value = PredicateType.DeserializeValue(reader);
                                break;
                        }

                        if (predicatable != null)
                        {
                            predicatable.Deserialize(reader.ReadSubtree());
                            this.Assign(predicatable);
                        }

                        if (value != null)
                        {
                            this.Assign(value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Serialize the predicate, given the operator.
        /// </summary>
        /// <returns>The serialized predicate.</returns>
        public string Serialize()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            string subject = this.Subject.ToString();
            IPredicatable s = this.Subject as IPredicatable;
            if (s != null)
            {
                subject = s.Serialize();
            }

            builder.Append(subject);

            switch (this.GetType().Name)
            {
                case "AddType":
                    builder.Append(" add ");
                    break;

                case "SubType":
                    builder.Append(" sub ");
                    break;

                case "MulType":
                    builder.Append(" mul ");
                    break;

                case "DivType":
                    builder.Append(" div ");
                    break;

                case "DivByType":
                    builder.Append(" divby ");
                    break;

                case "ModType":
                    builder.Append(" mod ");
                    break;
            }

            string predicate = this.Predicate.ToString();
            IPredicatable p = this.Predicate as IPredicatable;
            if (p != null)
            {
                predicate = p.Serialize();
            }

            builder.Append(predicate);
            builder.Append(")");

            return builder.ToString();
        }

        /// <summary>
        /// Lookup the property name associated with the predicatable.
        /// </summary>
        /// <returns>The property name.</returns>
        public string LookupPropertyName()
        {
            IPredicatable subject = this.Subject as IPredicatable;
            IPredicatable predicate = this.Predicate as IPredicatable;
            if (subject != null)
            {
                return subject.LookupPropertyName();
            }
            else if (predicate != null)
            {
                return predicate.LookupPropertyName();
            }

            return null;
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
            ArithmeticType copy = Activator.CreateInstance(this.GetType()) as ArithmeticType;
            copy.Subject = this.Subject;
            copy.Predicate = this.Predicate;

            return copy;
        }

        /// <summary>
        /// Locate the property names nested in the function.
        /// </summary>
        /// <returns>The list of located property names.</returns>
        List<PropertyNameType> IPredicatable.LocatePropertyNames()
        {
            return PredicateType.ExtractPropertyNames(this.Items.Values);
        }

        /// <summary>
        /// Assign the value to the open slot.
        /// </summary>
        /// <param name="value">The value to assign.</param>
        private void Assign(object value)
        {
            if (this.Subject == null)
            {
                this.Subject = value;
            }
            else if (this.Predicate == null)
            {
                this.Predicate = value;
            }
        }
    }
}
