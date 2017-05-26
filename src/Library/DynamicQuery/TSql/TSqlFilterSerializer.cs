// -----------------------------------------------------------------------
// <copyright file="TSqlFilterSerializer.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using OdataExpressionModel;

    /// <summary>
    /// Class for declaring a conditional statement.
    /// </summary>
    internal sealed class TSqlFilterSerializer
    {
        /// <summary>
        /// Mapping of data about enums discovered during query serialization.
        /// </summary>
        private static Dictionary<string, Tuple<Type, bool>> enumMappings = new Dictionary<string, Tuple<Type, bool>>();

        /// <summary>
        /// Initializes a new instance of the TSqlFilterSerializer class.
        /// </summary>
        /// <param name="queryNode">The container query's composite node.</param>
        public TSqlFilterSerializer(CompositeNode queryNode)
        {
            this.QueryNode = queryNode;
        }

        /// <summary>
        /// Gets or sets the filter conditions.
        /// </summary>
        public FilterType Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the container query's composite node.
        /// </summary>
        public CompositeNode QueryNode
        {
            get;
            private set;
        }

        /// <summary>
        /// Serialize the conditional statement.
        /// </summary>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized statement.</returns>
        internal string Serialize(ParameterContext context, SqlFormatter formatter)
        {
            if (this.Filter == null || this.Filter.Item == null)
            {
                return null;
            }

            return this.SerializeExpression(this.Filter.Item, context, formatter);
        }

        /// <summary>
        /// Serialize a predicatable.
        /// </summary>
        /// <param name="subject">The predicatable to serialize.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <returns>The resulting string.</returns>
        internal string SerializePredicatable(object subject, ParameterContext context)
        {
            PropertyNameType propertyName = subject as PropertyNameType;
            ParameterType parameter = subject as ParameterType;
            FunctionType function = subject as FunctionType;
            ArithmeticType arithmetic = subject as ArithmeticType;
            FunctionHelper helper = subject as FunctionHelper;
            EnumValueType enumValue = subject as EnumValueType;
            WithType withType = subject as WithType;
            ListType listType = subject as ListType;
            if (propertyName != null && propertyName.AggregateColumnReference == null)
            {
                return string.Format(
                    "{0}[{1}]",
                    string.IsNullOrEmpty(propertyName.Alias) == true ? null : string.Concat("[", propertyName.Alias, "]."),
                    propertyName.Value);
            }
            else if (propertyName != null && propertyName.AggregateColumnReference != null)
            {
                TSqlSerializer serializer = new TSqlSerializer();
                return serializer.SerializeAggregateColumn(
                    propertyName.AggregateColumnReference, 
                    propertyName.AggregateColumnReference.Column.NestedColumns, 
                    context);
            }
            else if (parameter != null)
            {
                context.Assign(parameter.Value, parameter);
                return parameter.Value;
            }
            else if (function != null && IsContains(function) == false)
            {
                return this.SerializeScalarFunction(function.Name, function.Arguments, context);
            }
            else if (helper != null)
            {
                return this.SerializeScalarFunction(helper.Name, helper.Arguments, context);
            }
            else if (arithmetic != null)
            {
                return this.SerializeArithmetic(arithmetic, context);
            }
            else if (withType != null)
            {
                return this.SerializeAggregate(withType, context);
            }
            else if (listType != null)
            {
                StringBuilder builder = new StringBuilder();
                string separator = "(";
                foreach (object item in listType.Items)
                {
                    string value = SerializePredicatable(item, context);
                    builder.Append(separator);
                    builder.Append(value);
                    separator = ", ";
                }

                builder.Append(")");
                return builder.ToString();
            }
            else if (enumValue != null)
            {
                Tuple<Type, bool> properties = InventoryEnum(this.QueryNode, enumValue);
                object value = Enum.Parse(properties.Item1, enumValue.Value);
                if (properties.Item2 == true)
                {
                    return string.Concat(" & ", ConvertEnum(properties.Item1, value), " != 0");
                }
                else
                {
                    return ConvertEnum(properties.Item1, value).ToString();
                }
            }
            else
            {
                string parameterName = null;
                if ((subject is NullType) == false)
                {
                    parameterName = context.GetNextName();
                    context.Assign(parameterName, subject);
                }

                return parameterName;
            }
        }

        /// <summary>
        /// Convert the enum value to the correct typed value.
        /// </summary>
        /// <param name="type">The type of the enum.</param>
        /// <param name="value">The value of the enum.</param>
        /// <returns></returns>
        private static object ConvertEnum(Type type, object value)
        {
            Type underlying = Enum.GetUnderlyingType(type);
            switch (underlying.Name)
            {
                case "Byte":
                    return (byte)value;
                case "Int16":
                    return (short)value;
                case "Int32":
                    return (int)value;
                case "Int64":
                    return (long)value;
                default:
                    throw new InvalidDataFilterException("Invalid Enum Type: " + type.Name);
            }
        }

        /// <summary>
        /// Get data about a given enum.
        /// </summary>
        /// <param name="node">The containing node of the enum value type.</param>
        /// <param name="enumValue">The enum value type.</param>
        /// <returns>Properties about the enum value type.</returns>
        private static Tuple<Type, bool> InventoryEnum(CompositeNode node, EnumValueType enumValue)
        {
            if (enumMappings.ContainsKey(enumValue.Type) == false)
            {
                bool flags = false;
                Type enumType = TypeCache.LocateType(enumValue.Type);
                object[] att = enumType.GetCustomAttributes(typeof(FlagsAttribute), true);
                if (att.Length == 1)
                {
                    flags = true;
                }

                Tuple<Type, bool> properties = new Tuple<Type, bool>(enumType, flags);
                enumMappings[enumValue.Type] = properties;
            }

            return enumMappings[enumValue.Type];
        }

        /// <summary>
        /// Compute the correct sql operator to use.
        /// </summary>
        /// <param name="predicate">The predicate to inspect.</param>
        /// <param name="value">The value to the right of the predicate.</param>
        /// <returns>The corresponding operator.</returns>
        private static string LookupOperator(PredicateType predicate, object value)
        {
            string op = null;
            if (predicate is EqualType)
            {
                op = value is NullType ? " IS NULL" : " = ";
            }
            else if (predicate is NotEqualType)
            {
                op = value is NullType ? " IS NOT NULL" : " != ";
            }
            else if (predicate is GreaterThanOrEqualType)
            {
                op = " >= ";
            }
            else if (predicate is GreaterThanType)
            {
                op = " > ";
            }
            else if (predicate is LessThanOrEqualType)
            {
                op = " <= ";
            }
            else if (predicate is LessThanType)
            {
                op = " < ";
            }
            else if (predicate is HasType)
            {
                op = string.Empty;
            }
            else if (predicate is InType)
            {
                op = " IN ";
            }

            return op;
        }

        /// <summary>
        /// Test whether the provided function is a contains function.
        /// </summary>
        /// <param name="function">The function to test.</param>
        /// <returns>True if it is a contains family function, otherwise false.</returns>
        private static bool IsContains(FunctionType function)
        {
            string test = function.Name.ToLower();
            return test.Equals("contains") == true ||
                test.Equals("not contains") == true ||
                test.Equals("startswith") == true ||
                test.Equals("not startswith") == true ||
                test.Equals("endswith") == true ||
                test.Equals("not endswith") == true ||
                test.Equals("isof") == true ||
                test.Equals("not isof") == true;
        }

        /// <summary>
        /// Serialize the expression.
        /// </summary>
        /// <param name="expression">The expression to serialize.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized expression.</returns>
        private string SerializeExpression(
            ExpressionType expression, 
            ParameterContext context, 
            SqlFormatter formatter)
        {
            ConditionType condition = expression as ConditionType;
            PredicateType predicate = expression as PredicateType;
            AnyOrAllType anyorall = expression as AnyOrAllType;
            if (condition != null)
            {
                return this.SerializeCondition(condition, context, formatter);
            }
            else if (predicate != null)
            {
                return this.SerializePredicate(predicate, context, formatter);
            }
            else if (anyorall != null)
            {
                return this.SerializeAnyOrAll(anyorall, context, formatter);
            }

            return null;
        }

        /// <summary>
        /// Serialize the any or all type.
        /// </summary>
        /// <param name="anyorall">The any or all type.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized expression.</returns>
        private string SerializeAnyOrAll(
            AnyOrAllType anyorall, 
            ParameterContext context, 
            SqlFormatter formatter)
        {
            return TSqlAnyOrAllSerializer.Serialize(anyorall, this.QueryNode, context, formatter);
        }

        /// <summary>
        /// Serialize the predicate.
        /// </summary>
        /// <param name="predicate">The predicate to serialize.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized predicate.</returns>
        private string SerializePredicate(
            PredicateType predicate, 
            ParameterContext context, 
            SqlFormatter formatter)
        {
            StringBuilder builder = new StringBuilder();
            IPredicatable subject = predicate.FindSubject();
            object value = predicate.FindValue();
            FunctionType function = subject as FunctionType;
            if (function != null && IsContains(function) == true)
            {
                bool bvalue = (bool)value;
                if (function.Negate == true)
                {
                    bvalue = !bvalue;
                }

                string statement = this.SerializeUnaryFunction(function, bvalue, context, formatter);
                builder.Append(statement);
            }
            else
            {
                builder.Append(this.SerializePredicatable(predicate.Subject, context));
                string op = LookupOperator(predicate, value);
                builder.Append(op);
                builder.Append(this.SerializePredicatable(predicate.Predicate, context));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize an aggregate.
        /// </summary>
        /// <param name="with">The with type.</param>
        /// <param name="context">The parameter context.</param>
        /// <returns>The serialized string.</returns>
        private string SerializeAggregate(WithType with, ParameterContext context)
        {
            string predicate = this.SerializePredicatable(with.Predicate, context);
            TSqlSerializer serializer = new TSqlSerializer();

            return serializer.GenerateWithAggregate(with, predicate);
        }

        /// <summary>
        /// Serialize an arithmetic type.
        /// </summary>
        /// <param name="arithmetic">The arithmetic instance to serialize to sql.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <returns>The serialized arithmetic.</returns>
        private string SerializeArithmetic(ArithmeticType arithmetic, ParameterContext context)
        {
            StringBuilder builder = this.CustomDateConvert(
                arithmetic.Subject as ParameterType,
                arithmetic.GetType(),
                arithmetic.Predicate,
                context);

            if (builder.Length == 0)
            {
                string subject = this.SerializePredicatable(arithmetic.Subject, context);
                string predicate = this.SerializePredicatable(arithmetic.Predicate, context);

                builder.Append("(");
                builder.Append(subject);

                switch (arithmetic.GetType().Name)
                {
                    case "AddType":
                        builder.Append(" + ");
                        break;

                    case "SubType":
                        builder.Append(" - ");
                        break;

                    case "MulType":
                        builder.Append(" * ");
                        break;

                    case "DivByType":
                        subject = "CAST(" + subject + " AS decimal)";
                        predicate = "CAST(" + predicate + " AS decimal)";
                        return string.Concat('(', subject, " / ", predicate, ')');

                    case "DivType":
                        builder.Append(" / ");
                        break;

                    case "ModType":
                        builder.Append(" % ");
                        break;
                }

                builder.Append(predicate);
                builder.Append(")");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the condition.
        /// </summary>
        /// <param name="condition">The condition to serialize.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized condition.</returns>
        private string SerializeCondition(
            ConditionType condition, 
            ParameterContext context, 
            SqlFormatter formatter)
        {
            StringBuilder builder = new StringBuilder();

            AndType and = condition as AndType;
            OrType or = condition as OrType;
            string separator = string.Empty;

            builder.Append("(");
            foreach (ExpressionType expression in condition.Items)
            {
                ConditionType inner = expression as ConditionType;
                if (and != null || (inner != null && inner is AndType && inner.Items.Count == 1))
                {
                    separator = string.Concat("\n", formatter.Indent, "AND   ");
                }
                else if (or != null || (inner != null && inner is OrType && inner.Items.Count == 1))
                {
                    separator = string.Concat("\n", formatter.Indent, "OR    ");
                }

                string statement = this.SerializeExpression(expression, context, formatter);
                if (expression != condition.Items[0])
                {
                    builder.Append(separator);
                }

                builder.Append(statement);
            }

            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Serialize a function that has implicit predicate in sql.
        /// </summary>
        /// <param name="function">The function to serialize.</param>
        /// <param name="value">The value of the function.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized function.</returns>
        private string SerializeUnaryFunction(
            FunctionType function,
            bool value,
            ParameterContext context,
            SqlFormatter formatter)
        {
            if (function.Name.Equals("isof", StringComparison.OrdinalIgnoreCase) == true)
            {
                return SerializeIsOf(function, value, context, formatter);
            }
            else
            {
                return SerializeContains(function, value, context, formatter);
            }
        }

        /// <summary>
        /// Serialize the isof function to sql.
        /// </summary>
        /// <param name="function">The function to serialize.</param>
        /// <param name="value">The value of the function.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized function.</returns>
        private string SerializeIsOf(
            FunctionType function,
            bool value,
            ParameterContext context,
            SqlFormatter formatter)
        {
            StringBuilder builder = new StringBuilder();
            string op = value == true ? "= " : "!= ";
            string endop = value == true ? "LIKE " : "NOT LIKE ";
            string name = "[$TypeName] ";
            PropertyNameType pnt = function.Arguments[0] as PropertyNameType;
            if (pnt != null)
            {
                name = string.Concat("[", pnt.Alias, "].", name);
            }

            string data = this.SerializePredicatable(function.Arguments.Count == 1 ? function.Arguments[0] : function.Arguments[1], context);
            builder.Append("(");
            builder.Append(name);
            builder.Append(op);
            builder.Append(data);
            builder.Append(" OR ");
            builder.AppendFormat("{0} ", data);
            builder.Append(endop);
            builder.AppendFormat("'%.' + {0}", name.Trim());
            builder.Append(")");

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the contains, startswith, or endswith function calls.
        /// </summary>
        /// <param name="function">The function to serialize.</param>
        /// <param name="value">The value of the function.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized function.</returns>
        private string SerializeContains(
            FunctionType function, 
            bool value, 
            ParameterContext context, 
            SqlFormatter formatter)
        {
            string name = this.SerializePredicatable(function.Arguments[0], context);
            StringBuilder builder = new StringBuilder(name);
            IList list = function.Arguments[1] as IList;
            if (list != null)
            {
                if (value == false)
                {
                    builder.Append(" NOT IN (");
                }
                else
                {
                    builder.Append(" IN (");
                }

                string separator = string.Empty;
                foreach (object item in list)
                {
                    builder.Append(separator);
                    string parameterName = context.GetNextName();
                    context.Assign(parameterName, item);
                    builder.Append(parameterName);
                    separator = ",";
                }

                builder.Append(")");
            }
            else
            {
                if (value == false)
                {
                    builder.Append(" NOT LIKE ");
                }
                else
                {
                    builder.Append(" LIKE ");
                }

                string test = function.Name.ToLower();
                string parameterName = context.GetNextName();
                context.Assign(parameterName, function.Arguments[1]);
                if (test.EndsWith("contains") == true)
                {
                    builder.AppendFormat("'%' + {0} + '%'", parameterName);
                }
                else if (test.EndsWith("startswith") == true)
                {
                    builder.AppendFormat("{0} + '%'", parameterName);
                }
                else if (test.EndsWith("endswith") == true)
                {
                    builder.AppendFormat("'%' + {0}", parameterName);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize a scalar function.
        /// </summary>
        /// <param name="name">The function name to serialize.</param>
        /// <param name="args">The function arguments.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <returns>The serialized function.</returns>
        private string SerializeScalarFunction(
            string name, 
            List<object> args,
            ParameterContext context)
        {
            switch(name.ToLower())
            {
                case "toupper":
                    return string.Format("UPPER({0})", this.SerializePredicatable(args[0], context));

                case "tolower":
                    return string.Format("LOWER({0})", this.SerializePredicatable(args[0], context));

                case "substring":
                    if (args.Count == 2)
                    {
                        return string.Format(
                            "SUBSTRING({0}, {1}, LEN({0}))",
                            this.SerializePredicatable(args[0], context),
                            this.SerializePredicatable(args[1], context));
                    }
                    else
                    {
                        return string.Format(
                            "SUBSTRING({0}, {1}, {2})",
                            this.SerializePredicatable(args[0], context),
                            this.SerializePredicatable(args[1], context),
                            this.SerializePredicatable(args[2], context));
                    }

                case "trim":
                    return string.Format("LTRIM(RTRIM({0}))", this.SerializePredicatable(args[0], context));

                case "concat":
                    string separator = string.Empty;
                    StringBuilder builder = new StringBuilder();
                    foreach (object arg in args)
                    {
                        builder.Append(separator);
                        builder.Append(this.SerializePredicatable(arg, context));
                        separator = " + ";
                    }

                    return builder.ToString();

                case "length":
                    return string.Format("LEN({0})", this.SerializePredicatable(args[0], context));

                case "indexof":
                    return string.Format("CHARINDEX({1}, {0})",
                        this.SerializePredicatable(args[0], context),
                        this.SerializePredicatable(args[1], context));

                case "year":
                    return string.Format("YEAR({0})", this.SerializePredicatable(args[0], context));

                case "month":
                    return string.Format("MONTH({0})", this.SerializePredicatable(args[0], context));

                case "day":
                    return string.Format("DAY({0})", this.SerializePredicatable(args[0], context));

                case "hour":
                    return string.Format("DATEPART(hh, {0})", this.SerializePredicatable(args[0], context));

                case "minute":
                    return string.Format("DATEPART(mm, {0})", this.SerializePredicatable(args[0], context));

                case "second":
                    return string.Format("DATEPART(ss, {0})", this.SerializePredicatable(args[0], context));

                case "fractionalseconds":
                    return string.Format("DATEPART(ms, {0})", this.SerializePredicatable(args[0], context));

                case "totaloffsetminutes":
                    return string.Format("DATEPART(tz, {0})", this.SerializePredicatable(args[0], context));

                case "totalseconds":
                    return string.Format("DATEDIFF(ss, '00:00:00', {0})", this.SerializePredicatable(args[0], context));

                case "round":
                    return string.Format("ROUND({0}, 0, 0)", this.SerializePredicatable(args[0], context));

                case "floor":
                    return string.Format("FLOOR({0})", this.SerializePredicatable(args[0], context));

                case "ceiling":
                    return string.Format("CEILING({0})", this.SerializePredicatable(args[0], context));

                case "date":
                    return string.Format("CAST({0} AS date)", this.SerializePredicatable(args[0], context));

                case "time":
                    return string.Format("CAST({0} AS time)", this.SerializePredicatable(args[0], context));

                case "now":
                    return "SYSDATETIMEOFFSET()";

                case "mindatetime":
                    return "CAST('001-01-01' AS datetimeoffset)";

                case "maxdatetime":
                    return "CAST('9999-12-31 23:59:59' AS datetimeoffset)";

                case "cast":
                    return string.Format("CAST({0} AS {1})", this.SerializePredicatable(args[0], context), this.SerializeEdmType(args[1]));

                default:
                    Dictionary<string, UserDefinedFunction> functions = QueryBuilderSettings.UserDefinedFunctions();
                    if (functions.ContainsKey(name) == true && functions[name] != null)
                    {
                        return functions[name].SerializeToSql(args, context, this.SerializePredicatable);
                    }

                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Serialize the edm type into a sql type name.
        /// </summary>
        /// <param name="edm">The edm type.</param>
        /// <returns>The serialized sql type.</returns>
        private string SerializeEdmType(object edm)
        {
            if (edm != null)
            {
                switch (edm.ToString())
                {
                    case "Edm.String":
                        return "nvarchar(max)";
                    case "Edm.Int16":
                        return "smallint";
                    case "Edm.Int32":
                        return "int";
                    case "Edm.Int64":
                        return "bigint";
                    case "Edm.Double":
                        return "float";
                    case "Edm.Boolean":
                        return "bit";
                    case "Edm.Byte":
                        return "tinyint";
                    case "Edm.DateTime":
                        return "datetime";
                    case "Edm.DateTimeOffset":
                        return "datetimeoffset";
                    case "Edm.Decimal":
                        return "decimal";
                    case "Edm.Single":
                        return "real";
                    case "Edm.Guid":
                        return "uniqueidentifier";
                    case "Edm.Binary":
                        return "varbinary(max)";
                    case "Edm.SByte":
                        return "smallint";
                    case "Edm.Time":
                        return "time";
                }
            }

            throw new NotSupportedException("Non null edm types are required on this api.");
        }

        /// <summary>
        /// Convert a custom datetime parameter clause, if applicable.
        /// </summary>
        /// <param name="subject">The parameter subject.</param>
        /// <param name="op">The operator type.</param>
        /// <param name="predicate">The serialized predicate.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <returns>The builder with the serialization string.</returns>
        private StringBuilder CustomDateConvert(
            ParameterType subject, 
            Type op, 
            object predicate, 
            ParameterContext context)
        {
            StringBuilder builder = new StringBuilder();
            if (subject == null)
            {
                return builder;
            }

            if (subject.Value == "@Now" ||
                subject.Value == "@UtcNow" ||
                subject.Value == "@Today" ||
                subject.Value == "@UtcToday")
            {
                if (op == typeof(AddType) || op == typeof(SubType))
                {
                    string units = "dd";
                    string parm = this.SerializePredicatable(subject, context);
                    string dayoffset = null;
                    if (predicate is TimeSpan)
                    {
                        units = "hh";
                        TimeSpan ts = (TimeSpan)predicate;
                        dayoffset = this.SerializePredicatable((int)ts.TotalHours, context);
                    }
                    else
                    {
                        dayoffset = this.SerializePredicatable(predicate, context);
                    }

                    builder.Clear();
                    builder.AppendFormat(
                        "DATEADD({0}, {1}{2}, {3})",
                        units,
                        op == typeof(SubType) ? "-" : string.Empty,
                        dayoffset,
                        parm);
                }
            }

            return builder;
        }
    }
}
