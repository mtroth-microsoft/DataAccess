// -----------------------------------------------------------------------
// <copyright file="PredicateType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to PredicateType in model.
    /// </summary>
    public abstract partial class PredicateType
    {
        /// <summary>
        /// Deserialize reader into object value.
        /// </summary>
        /// <param name="reader">The reader to inspect.</param>
        /// <returns>The deserialized object.</returns>
        internal static object DeserializeValue(XmlReader reader)
        {
            object value = null;
            switch (reader.LocalName)
            {
                case "String":
                    reader.Read();
                    value = reader.Value;
                    break;

                case "Int":
                    reader.Read();
                    value = XmlConvert.ToInt32(reader.Value);
                    break;

                case "Long":
                    reader.Read();
                    value = XmlConvert.ToInt64(reader.Value);
                    break;

                case "Double":
                    reader.Read();
                    value = XmlConvert.ToDouble(reader.Value);
                    break;

                case "Guid":
                    reader.Read();
                    value = XmlConvert.ToGuid(reader.Value);
                    break;

                case "DateTime":
                    reader.Read();
                    value = XmlConvert.ToDateTime(reader.Value, XmlDateTimeSerializationMode.RoundtripKind);
                    break;

                case "DateTimeOffset":
                    reader.Read();
                    value = XmlConvert.ToDateTimeOffset(reader.Value);
                    break;

                case "Boolean":
                    reader.Read();
                    value = XmlConvert.ToBoolean(reader.Value);
                    break;

                case "EnumValue":
                    string type = reader.GetAttribute("Type");
                    reader.Read();
                    value = new EnumValueType() { Value = reader.Value, Type = type };
                    break;

                case "TimeSpan":
                    reader.Read();
                    value = XmlConvert.ToTimeSpan(reader.Value);
                    break;

                case "List":
                    ListType list = new ListType();
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            XmlReader sub = reader.ReadSubtree();
                            sub.MoveToContent();
                            object item = DeserializeValue(sub);
                            list.Items.Add(item);
                        }
                    }

                    value = list;
                    break;
            }

            return value;
        }

        /// <summary>
        /// Serialize the key values.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string SerializeValue(object key)
        {
            if (key == null || key.ToString() == "null" || key.GetType() == typeof(NullType))
            {
                return "null";
            }
            else if (key.GetType() == typeof(string))
            {
                return string.Format("'{0}'", key.ToString());
            }
            else if (key.GetType() == typeof(int))
            {
                return key.ToString();
            }
            else if (key.GetType() == typeof(long))
            {
                return string.Format("{0}L", key.ToString());
            }
            else if (key.GetType() == typeof(double))
            {
                return string.Format("{0}", key.ToString());
            }
            else if (key.GetType() == typeof(Guid))
            {
                return string.Format("{0}", key.ToString());
            }
            else if (key.GetType() == typeof(DateTime))
            {
                return string.Format("{0}", ((DateTime)key).ToString("o")).Replace(' ', 'T');
            }
            else if (key.GetType() == typeof(DateTimeOffset))
            {
                return string.Format("{0}", ((DateTimeOffset)key).UtcDateTime.ToString("o")).Replace(' ', 'T');
            }
            else if (key.GetType() == typeof(bool))
            {
                return XmlConvert.ToString((bool)key);
            }
            else if (key.GetType() == typeof(EnumValueType))
            {
                EnumValueType evt = (EnumValueType)key;
                return string.Format("{0}'{1}'", evt.Type, evt.Value);
            }
            else if (key.GetType() == typeof(TimeSpan))
            {
                TimeSpan ts = (TimeSpan)key;
                return string.Format("duration'{0}'", XmlConvert.ToString(ts));
            }
            else if (key.GetType() == typeof(byte[]))
            {
                byte[] buffer = (byte[])key;
                return "0x" + BitConverter.ToString(buffer).Replace("-", string.Empty);
            }
            else if (key.GetType().IsEnum == true)
            {
                EnumValueType evt = new EnumValueType();
                evt.Type = key.GetType().FullName;
                evt.Value = key.ToString();

                return string.Format("{0}'{1}'", evt.Type, evt.Value);
            }
            else if (key.GetType() == typeof(ListType))
            {
                ListType list = (ListType)key;
                StringBuilder output = new StringBuilder();
                string separator = "(";
                foreach (object item in list.Items)
                {
                    output.Append(separator);
                    output.Append(SerializeValue(item));
                    separator = ", ";
                }

                output.Append(")");
                return output.ToString();
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Helper to extract property names from a list of objects
        /// that may contain predicatables.
        /// </summary>
        /// <param name="list">The list of objects to inspect.</param>
        /// <returns>The list of discovered property names.</returns>
        internal static List<PropertyNameType> ExtractPropertyNames(IEnumerable<object> list)
        {
            List<PropertyNameType> propertyNames = new List<PropertyNameType>();
            Stack<object> stack = new Stack<object>();
            Queue<object> q = new Queue<object>(list);
            stack.Push(q);

            while (stack.Count > 0)
            {
                object item = stack.Pop();
                Queue<object> args = item as Queue<object>;
                if (args != null && args.Count > 0)
                {
                    object temp = args.Dequeue();
                    stack.Push(args);
                    stack.Push(temp);
                    continue;
                }

                IPredicatable predicatable = item as IPredicatable;
                FunctionHelper helper = item as FunctionHelper;
                if (predicatable != null)
                {
                    List<PropertyNameType> located = predicatable.LocatePropertyNames();
                    propertyNames.AddRange(located);
                }
                else if (helper != null)
                {
                    foreach (object arg in helper.Arguments)
                    {
                        stack.Push(arg);
                    }
                }
            }

            return propertyNames;
        }

        /// <summary>
        /// Create a predicatable based on a local name.
        /// </summary>
        /// <param name="localName">The name of the predicatable.</param>
        /// <returns>The predicatable.</returns>
        internal static IPredicatable CreatePredicatable(string localName)
        {
            switch (localName)
            {
                case "PropertyName":
                    return new PropertyNameType();
                case "Mul":
                    return new MulType();
                case "Div":
                    return new DivType();
                case "DivBy":
                    return new DivByType();
                case "Add":
                    return new AddType();
                case "Sub":
                    return new SubType();
                case "Mod":
                    return new ModType();
                case "Function":
                    return new FunctionType();
                case "With":
                    return new WithType();
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Deep copy the current instance.
        /// </summary>
        /// <returns>The copy.</returns>
        internal override ExpressionType DeepCopy()
        {
            PredicateType copy = Activator.CreateInstance(this.GetType()) as PredicateType;
            IPredicatable subject = this.Subject as IPredicatable;
            IPredicatable predicate = this.Predicate as IPredicatable;
            copy.Subject = subject == null ? this.Subject : subject.Copy();
            copy.Predicate = predicate == null ? this.Predicate : predicate.Copy();

            return copy;
        }

        /// <summary>
        /// Returns the first item in the items collection that implements IPredicatable.
        /// </summary>
        /// <returns>The subject of the predicate type.</returns>
        internal IPredicatable FindSubject()
        {
            IPredicatable subject = this.Subject as IPredicatable;
            IPredicatable predicate = this.Predicate as IPredicatable;
            if (subject != null)
            {
                return subject;
            }
            else if (predicate != null)
            {
                return predicate;
            }


            return null;
        }

        /// <summary>
        /// Returns the first item in the items collection that is not predicatable.
        /// </summary>
        /// <returns>The value of the predicate type.</returns>
        internal object FindValue()
        {
            IPredicatable subject = this.Subject as IPredicatable;
            IPredicatable predicate = this.Predicate as IPredicatable;
            if (subject == null)
            {
                return this.Subject;
            }
            else if (predicate == null)
            {
                return this.Predicate;
            }

            return null;
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
                                NullType nt = new NullType();
                                value = nt;
                                break;

                            case "With":
                                WithType wt = new WithType();
                                predicatable = wt;
                                break;

                            default:
                                value = DeserializeValue(reader);
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
        /// Populate the parameters, converting if necessary.
        /// </summary>
        /// <param name="parameters">The assigned parameters.</param>
        internal override ConditionType Convert(Dictionary<string, object> parameters)
        {
            ParameterType left = this.Subject as ParameterType;
            ParameterType right = this.Predicate as ParameterType;
            FunctionType function = this.Subject as FunctionType;

            // Data must have correct type in parameter, unless the predicate
            // will be split into an 'or' statement during the split call.
            // In that case, the values are co-coerced when split below.
            if (left != null)
            {
                // if null value on parameter, make it a tautology.
                object lv;
                if (parameters.TryGetValue(left.Value, out lv) == true)
                {
                    this.Subject = lv ?? this.Predicate;
                    this.Subject = TypeCheck(this.Predicate);
                }
            }
            else if (right != null)
            {
                // if null value on parameter, make it a tautology.
                object rv;
                if (parameters.TryGetValue(right.Value, out rv) == true)
                {
                    this.Predicate = rv ?? this.Subject;
                    this.Predicate = TypeCheck(this.Predicate);
                }
            }
            else if (function != null)
            {
                function.Replace(parameters);
            }

            return this.Split();
        }

        /// <summary>
        /// Set the prefixes on all the property name in the clause.
        /// </summary>
        /// <param name="prefix">The prefix to set.</param>
        internal override void SetPrefixes(string prefix)
        {
            PropertyNameType pnt = this.Subject as PropertyNameType;
            FunctionType ft = this.Subject as FunctionType;
            if (pnt != null)
            {
                pnt.Prefix = prefix;
            }
            else if (ft != null)
            {
                ft.SetPrefix(prefix);
            }
            else
            {
                pnt = this.Predicate as PropertyNameType;
                ft = this.Predicate as FunctionType;
                if (pnt != null)
                {
                    pnt.Prefix = prefix;
                }
                else if (ft != null)
                {
                    ft.SetPrefix(prefix);
                }
            }
        }

        /// <summary>
        /// Split the predicate into a or statement.
        /// </summary>
        /// <returns>The or statement.</returns>
        protected abstract ConditionType Split();

        /// <summary>
        /// Helper for splitting predicates.
        /// </summary>
        /// <typeparam name="T">The type of the predicate.</typeparam>
        /// <param name="initialized">An initialized condition type to use for the split.</param>
        /// <returns>The split condition.</returns>
        protected ConditionType Split<T>(ConditionType initialized) where T : PredicateType, new()
        {
            ConditionType condition = null;
            IPredicatable left = this.Subject as IPredicatable;
            IPredicatable right = this.Predicate as IPredicatable;

            IList<string> values = null;
            if (left == null && this.Subject != null)
            {
                values = DataFilterParsingHelper.SplitArgs(this.Subject.ToString());
            }
            else if (right == null && this.Predicate != null)
            {
                values = DataFilterParsingHelper.SplitArgs(this.Predicate.ToString());
            }

            if (values != null && values.Count > 1)
            {
                condition = initialized;
                foreach (string value in values)
                {
                    object coerced = Coerce(value);
                    T item = new T();
                    if (left != null)
                    {
                        item.Subject = left.Copy();
                        item.Predicate = coerced;
                    }
                    else if (right != null)
                    {
                        item.Subject = coerced;
                        item.Predicate = right.Copy();
                    }

                    condition.Items.Add(item);
                }
            }

            return condition;
        }

        /// <summary>
        /// Serialize the predicate, given the operator.
        /// </summary>
        /// <param name="op">The operator to use.</param>
        /// <returns>The serialized predicate.</returns>
        protected string SerializePredicate(string op)
        {
            StringBuilder builder = new StringBuilder();

            object left = this.Subject;
            object right = this.Predicate;

            IPredicatable subject = left as IPredicatable;
            IPredicatable predicate = right as IPredicatable;

            string leftvalue = null, rightvalue = null;
            if (subject == null)
            {
                leftvalue = PredicateType.SerializeValue(left);
            }
            else
            {
                leftvalue = subject.Serialize();
            }

            builder.Append(leftvalue);
            builder.Append(op);

            if (predicate == null)
            {
                rightvalue = PredicateType.SerializeValue(right);
            }
            else
            {
                rightvalue = predicate.Serialize();
            }

            builder.Append(rightvalue);

            return builder.ToString();
        }

        /// <summary>
        /// Check the type of the value and coerce if required.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>The resulting value.</returns>
        private static object TypeCheck(object value)
        {
            object typed = value;
            if (value is string &&
                value.ToString().Contains(",") == false)
            {
                typed = Coerce(value.ToString());
            }

            return typed;
        }

        /// <summary>
        /// Coerce the provided value into a typed value.
        /// </summary>
        /// <param name="value">The string version of the value.</param>
        /// <returns>The coerced value.</returns>
        private static object Coerce(string value)
        {
            long longresult;
            if (long.TryParse(value, out longresult) == true)
            {
                return longresult;
            }

            decimal decimalresult;
            if (decimal.TryParse(value, out decimalresult) == true)
            {
                return decimalresult;
            }

            bool boolresult;
            if (bool.TryParse(value, out boolresult) == true)
            {
                return boolresult;
            }

            DateTime datetimeresult;
            if (DateTime.TryParseExact(
                value, 
                "o", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.RoundtripKind, 
                out datetimeresult) == true)
            {
                return datetimeresult;
            }

            return value;
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
