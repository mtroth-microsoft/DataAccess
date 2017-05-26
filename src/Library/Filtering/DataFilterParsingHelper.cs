// -----------------------------------------------------------------------
// <copyright file="DataFilterParsingHelper.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// List of query options that can be parsed.
    /// </summary>
    internal enum ParsableQueryOptions
    {
        Where,
        Filter,
    }

    /// <summary>
    /// Class for providing DataFilter parsing functionality.
    /// </summary>
    public static class DataFilterParsingHelper
    {
        /// <summary>
        /// The value for the root token.
        /// </summary>
        private const string FilterTokenValue = "where";

        /// <summary>
        /// Extract a list of values from the provided expression.
        /// </summary>
        /// <param name="expressionType">The expression to inspect.</param>
        /// <param name="propertyName">The name of the property containing the id values.</param>
        /// <returns>The list of values.</returns>
        public static List<T> ExtractList<T>(ExpressionType expressionType, string propertyName)
        {
            List<T> list = new List<T>();
            Queue<ExpressionType> queue = new Queue<ExpressionType>();

            if (expressionType != null)
            {
                queue.Enqueue(expressionType);
                while (queue.Count > 0)
                {
                    ExpressionType item = queue.Dequeue();
                    ConditionType condition = item as ConditionType;
                    PredicateType predicate = item as PredicateType;
                    if (condition != null)
                    {
                        foreach (ExpressionType child in condition.Items)
                        {
                            queue.Enqueue(child);
                        }
                    }
                    else if (predicate != null)
                    {
                        string subject;
                        object value;
                        if (TryShredPredicate(predicate, out subject, out value) == true &&
                            subject.Equals(propertyName, StringComparison.OrdinalIgnoreCase) == true &&
                            value != null)
                        {
                            List<object> set = value as List<object>;
                            if (set != null)
                            {
                                list.AddRange(set.OfType<T>().Cast<T>());
                            }
                            else
                            {
                                list.Add((T)value);
                            }
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Extract a list of ids from the provided expression.
        /// </summary>
        /// <param name="expressionType">The expression to inspect.</param>
        /// <param name="propertyName">The name of the property containing the id values.</param>
        /// <returns>The list of ids.</returns>
        public static List<int> ExtractListOfIds(ExpressionType expressionType, string propertyName)
        {
            return ExtractList<int>(expressionType, propertyName);
        }

        /// <summary>
        /// Extracts the name value pairs from the and type.
        /// </summary>
        /// <param name="and">The container of the data.</param>
        /// <returns>The resulting map.</returns>
        public static Dictionary<string, object> ExtractNameValuePairs(ExpressionType expression)
        {
            // This assumes that filtering syntax is understood by the stored procedure.
            Dictionary<string, object> map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Queue<ExpressionType> queue = new Queue<ExpressionType>();
            if (expression != null)
            {
                queue.Enqueue(expression);
            }

            while (queue.Count > 0)
            {
                ExpressionType item = queue.Dequeue();
                ConditionType condition = item as ConditionType;
                PredicateType predicate = item as PredicateType;
                if (condition != null)
                {
                    foreach (ExpressionType child in condition.Items)
                    {
                        queue.Enqueue(child);
                    }
                }
                else if (predicate != null)
                {
                    string subject;
                    object value;
                    if (TryShredPredicate(predicate, out subject, out value) == true)
                    {
                        map[subject] = value;
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Parses the uri into a query builder settings instance.
        /// </summary>
        /// <param name="uri">The uri to inspect.</param>
        /// <param name="entitySetType">The type of the first entity set in the url.</param>
        /// <returns>The parsed result.</returns>
        public static QueryBuilderSettings ExtractToSettings(Uri uri, Type entitySetType)
        {
            QueryBuilderSettings settings = new QueryBuilderSettings();
            settings.Filter = ParseFilter(uri);
            settings.Where = ParseWhere(uri);
            settings.Groupings.AddRange(ParseGroupBy(uri));
            settings.Selects.AddRange(ParseSelect(uri));
            settings.Orderings.AddRange(ParseOrderBy(uri));
            settings.Top = ParseTop(uri);
            settings.Skip = ParseSkip(uri);
            settings.Aggregates.AddRange(ParseColumns(uri));
            settings.QueryContext = new QueryContext(uri);
            settings.ArgumentFilter = ParseArguments(uri, entitySetType);
            settings.Url = uri;
            settings.EntitySetType = entitySetType;
            settings.Executor = ExtractStoreExecutor(uri);
            FixEnums(entitySetType, settings);
            AlignAliasedPredicates(settings);

            return settings;
        }

        /// <summary>
        /// Parse the entire query options string, creating a map.
        /// </summary>
        /// <param name="odata">The url to parse.</param>
        /// <returns>The map of query options.</returns>
        public static Dictionary<string, string> ParseQueryOptions(Uri odata)
        {
            Dictionary<string, string> tokens = BuildTokenMap(odata);

            return tokens;
        }

        /// <summary>
        /// Parses the data filter.
        /// </summary>
        /// <param name="odata">The uri to inspect.</param>
        /// <returns>The parsed result.</returns>
        public static FilterType ParseWhere(Uri odata)
        {
            return ParseQueryOption(odata, ParsableQueryOptions.Where);
        }

        /// <summary>
        /// Parses the odata filter.
        /// </summary>
        /// <param name="odata">The uri to inspect.</param>
        /// <returns>The parsed result.</returns>
        public static FilterType ParseFilter(Uri odata)
        {
            return ParseQueryOption(odata, ParsableQueryOptions.Filter);
        }

        /// <summary>
        /// Parses the groupby.
        /// </summary>
        /// <param name="odata">The uri to inspect.</param>
        /// <returns>The parsed result.</returns>
        public static List<GroupByReferenceType> ParseGroupBy(Uri odata)
        {
            List<GroupByReferenceType> groupings = new List<GroupByReferenceType>();
            Dictionary<string, string> tokenMap = BuildTokenMap(odata);
            if (tokenMap.ContainsKey("groupby") == true)
            {
                string groupby = RemoveRootParens(tokenMap["groupby"]);
                string[] tokens = Split(groupby, ',', new List<char>() { '$' });
                for (int i = 0; i < tokens.Length; i++)
                {
                    string cleaned = RemoveRootParens(tokens[i]);
                    GroupByReferenceType gbrt = new GroupByReferenceType();
                    if (cleaned.StartsWith("rollup(") == true)
                    {
                        gbrt.GroupingType = GroupingType.Rollup;
                        FunctionHelper helper = new FunctionHelper(cleaned.Trim());
                        foreach (object arg in helper.Arguments)
                        {
                            PropertyNameType pnt = arg as PropertyNameType;
                            PropertyType pt = new PropertyType() { Name = pnt.Serialize() };
                            gbrt.Properties.Add(pt);
                        }
                    }
                    else
                    {
                        gbrt.Properties.Add(new PropertyType() { Name = cleaned });
                    }

                    groupings.Add(gbrt);
                }
            }

            return groupings;
        }

        /// <summary>
        /// Parses the odata select.
        /// </summary>
        /// <param name="odata">The uri to inspect.</param>
        /// <returns>The parsed result.</returns>
        public static List<string> ParseSelect(Uri odata)
        {
            List<string> selects = new List<string>();
            Dictionary<string, string> tokenMap = BuildTokenMap(odata);
            if (tokenMap.ContainsKey("select") == true)
            {
                string[] tokens = tokenMap["select"].Split(',');
                foreach (string token in tokens)
                {
                    selects.Add(token.Trim());
                }
            }

            return selects;
        }

        /// <summary>
        /// Parse the order by for the given query.
        /// </summary>
        /// <param name="odata">The query's uri.</param>
        /// <returns>The list of ordered properties.</returns>
        public static List<OrderedPropertyType> ParseOrderBy(Uri odata)
        {
            List<OrderedPropertyType> orderbys = new List<OrderedPropertyType>();
            Dictionary<string, string> tokenMap = BuildTokenMap(odata);
            if (tokenMap.ContainsKey("orderby") == true)
            {
                string[] tokens = Split(tokenMap["orderby"], ',', new List<char>());
                foreach (string token in tokens)
                {
                    OrderedPropertyType order = new OrderedPropertyType();
                    orderbys.Add(order);
                    string[] split = Split(token.Trim(), ' ', new List<char>());
                    order.Name = split[0];
                    order.Prefix = string.Empty;
                    int pos = split[0].LastIndexOf('/');
                    if (pos > 0)
                    {
                        order.Prefix = split[0].Substring(0, pos);
                        order.Name = split[0].Substring(pos + 1);
                    }

                    order.Ascending = true;
                    if (split.Length > 1 && split[1].ToLower().Equals("desc") == true)
                    {
                        order.Ascending = false;
                    }
                }
            }

            return orderbys;
        }

        /// <summary>
        /// Parse the top for the given query.
        /// </summary>
        /// <param name="odata">The query's uri.</param>
        /// <returns>The top value or null.</returns>
        public static long? ParseTop(Uri odata)
        {
            Dictionary<string, string> tokenMap = BuildTokenMap(odata);
            if (tokenMap.ContainsKey("top") == true)
            {
                return long.Parse(tokenMap["top"]);
            }

            return null;
        }

        /// <summary>
        /// Parse the skip for the given query.
        /// </summary>
        /// <param name="odata">The query's uri.</param>
        /// <returns>The skip value or null.</returns>
        public static long? ParseSkip(Uri odata)
        {
            Dictionary<string, string> tokenMap = BuildTokenMap(odata);
            if (tokenMap.ContainsKey("skip") == true)
            {
                return long.Parse(tokenMap["skip"]);
            }

            return null;
        }

        /// <summary>
        /// Parse the arguments in the url.
        /// Applies to keys on entity sets and args to functions.
        /// </summary>
        /// <param name="odata">The uri to inspect.</param>
        /// <param name="entitySetType">The type of the first entity set in the url.</param>
        /// <returns>The corresponding filter for the args.</returns>
        public static FilterType ParseArguments(Uri odata, Type entitySetType)
        {
            if (entitySetType == null)
            {
                return null;
            }

            List<TypeCache.PathStep> types = TypeCache.ExtractTypeSequence(odata, entitySetType);
            Dictionary<string, string> map = ParseQueryOptions(odata);

            bool keyAdded = false;
            StringBuilder fullPath = new StringBuilder();
            AndType and = new AndType();
            string separator = string.Empty, nextStep = string.Empty;
            for (int i = types.Count - 1; i >= 0; i--)
            {
                string step = types[i].Name;
                string args = types[i].Args;
                Type type = types[i].Type;

                string[] parts = Split(args, ',', new List<char>());
                if (parts.Length == 0)
                {
                    parts = args.Split(',');
                }

                int current = 0;
                foreach (string part in parts)
                {
                    string value = null, name = null;
                    string[] sides = Split(part, '=', new List<char>());
                    if (sides.Length == 2)
                    {
                        name = sides[0].Trim();
                        value = sides[1].Trim();
                        if (value[0] == '@')
                        {
                            value = map[sides[1]];
                        }
                    }
                    else if (type != null)
                    {
                        if (sides.Length == 1)
                        {
                            value = sides[0];
                        }
                        else
                        {
                            value = "null";
                        }

                        int index = 0;
                        PropertyInfo[] properties = type.GetProperties();
                        foreach (PropertyInfo property in properties)
                        {
                            KeyAttribute key = TypeCache.GetAttribute<KeyAttribute>(property);
                            if (key != null && current == index++)
                            {
                                name = property.Name;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(name) == false && value != "null" && string.IsNullOrEmpty(args) == false && keyAdded == false)
                    {
                        keyAdded = true;
                        FilterType result = Parse(string.Concat(name, " eq ", value));
                        and.Items.Add(result.Item);
                        ((PropertyNameType)((EqualType)result.Item).Subject).ElementType = type;
                        ((PropertyNameType)((EqualType)result.Item).Subject).Prefix = fullPath.ToString();
                    }

                    current++;
                }

                if (types[i].IsTypeConstraint == true && fullPath.Length == 0)
                {
                    FilterType result = Parse(string.Concat("isof($it, '", type.FullName, "')"));
                    and.Items.Add(result.Item);
                }

                nextStep = step;
                if (types[i].IsTypeConstraint == false)
                {
                    fullPath.Append(separator);
                    fullPath.Append(nextStep);
                }

                if (fullPath.Length > 0)
                {
                    separator = "/";
                }
            }

            FilterType filter = null;
            if (and.Items.Count > 0)
            {
                filter = new FilterType() { Item = and };
            }

            return filter;
        }

        /// <summary>
        /// Parse the column references for use in aggregate queries.
        /// </summary>
        /// <param name="odata">The uri.</param>
        /// <returns>The list of aggregate column references.</returns>
        public static List<AggregateColumnReference> ParseColumns(Uri odata)
        {
            const string WithName = " with ";
            const string AsName = " as ";

            List<AggregateColumnReference> list = new List<AggregateColumnReference>();
            Dictionary<string, string> tokenMap = BuildTokenMap(odata);
            if (tokenMap.ContainsKey("aggregate") == true || tokenMap.ContainsKey("compute") == true)
            {
                string aggregate = ExtractColumnString(tokenMap);
                string[] items = Split(aggregate, ',', new List<char>() { '$' });
                for (int i = 0; i < items.Length; i++)
                {
                    string cleaned = RemoveRootParens(items[i]);
                    int withpos = cleaned.IndexOf(WithName, StringComparison.OrdinalIgnoreCase);
                    int aspos = cleaned.IndexOf(AsName, StringComparison.OrdinalIgnoreCase);

                    string inner = "*";
                    if (withpos > 0)
                    {
                        inner = cleaned.Substring(0, withpos).Trim();
                    }

                    string alias = null;
                    string type = cleaned.Substring(withpos + WithName.Length).Trim();
                    if (aspos > 0)
                    {
                        alias = cleaned.Substring(aspos + AsName.Length).Trim();
                        if (withpos > 0)
                        {
                            type = type.Substring(0, aspos - withpos - WithName.Length).Trim();
                        }
                        else
                        {
                            type = cleaned.Substring(0, aspos);
                        }
                    }

                    AggregateType agg;
                    if (Enum.TryParse<AggregateType>(type, true, out agg))
                    {
                        IPredicatable predicatable = ParseArithmetic(inner);
                        if (string.IsNullOrEmpty(alias) == true)
                        {
                            int slashPos = inner.IndexOf('/');
                            alias = inner.Substring(slashPos + 1);
                        }

                        AggregateColumnReference acr = new AggregateColumnReference(predicatable, agg, alias);
                        list.Add(acr);
                    }
                    else if (withpos < 0 && string.IsNullOrEmpty(type) == false && string.IsNullOrEmpty(alias) == false)
                    {
                        IPredicatable predicatable = ParseArithmetic(type);
                        AggregateColumnReference acr = new AggregateColumnReference(predicatable, AggregateType.None, alias);
                        list.Add(acr);
                    }
                    else if (string.IsNullOrEmpty(alias) == false && withpos > 0)
                    {
                        IPredicatable predicatable = ParseArithmetic(cleaned.Substring(0, aspos).Trim());
                        AggregateColumnReference acr = new AggregateColumnReference(predicatable, AggregateType.Multiple, alias);
                        list.Add(acr);
                    }
                    else
                    {
                        throw new InvalidDataFilterException("An Invalid aggregate type has been specified: " + type);
                    }
                }                
            }

            return list;
        }

        /// <summary>
        /// Extract all the predicates in a given filter and return them as a list.
        /// </summary>
        /// <param name="filter">The filter to inspect.</param>
        /// <returns>The list of predicates.</returns>
        internal static List<ParsedPredicate> Flatten(FilterType filter)
        {
            List<ParsedPredicate> list = new List<ParsedPredicate>();
            Queue<ExpressionType> queue = new Queue<ExpressionType>();
            if (filter != null && filter.Item != null)
            {
                queue.Enqueue(filter.Item);
            }

            while (queue.Count > 0)
            {
                ExpressionType item = queue.Dequeue();
                ConditionType condition = item as ConditionType;
                PredicateType predicate = item as PredicateType;
                if (condition != null)
                {
                    foreach (ExpressionType child in condition.Items)
                    {
                        queue.Enqueue(child);
                    }
                }
                else if (predicate != null)
                {
                    ParsedPredicate pp = new ParsedPredicate();
                    pp.Item = predicate;
                    pp.Value = predicate.FindValue();
                    IPredicatable predicatable = predicate.FindSubject();
                    if (predicate != null)
                    {
                        pp.PropertyNames = predicatable.LocatePropertyNames();
                    }

                    list.Add(pp);
                }
            }

            return list;
        }

        /// <summary>
        /// Parse arithmetic into a predicatable.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <returns>The resulting predicatable.</returns>
        internal static IPredicatable ParseArithmetic(string input)
        {
            XElement element = Compile(input);
            IPredicatable predicatable = PredicateType.CreatePredicatable(element.Name.LocalName);
            predicatable.Deserialize(element.CreateReader());

            return predicatable;
        }

        /// <summary>
        /// Test the string to see if it is a datetimeoffset.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if it is a datetimeoffset, otherwise false.</returns>
        internal static bool IsDateTimeOffset(string value)
        {
            Regex regex = new Regex(@"^(')?(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(.(\d{3,7}))?(Z| )?(\+|\-)?((\d{2}):(\d{2}))?(')?$");

            return regex.IsMatch(value);
        }

        /// <summary>
        /// Test the string to see if it is a long.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if it is a long, otherwise false.</returns>
        internal static bool IsLong(string value)
        {
            Regex regex = new Regex(@"^(-)?(\d{1,19})L$");

            return regex.IsMatch(value);
        }

        /// <summary>
        /// Split the arguments in a comma delimitted list. 
        /// Omits commas inside of ticks.
        /// Omits commas inside of parentheses.
        /// Removes any spaces between members in the list.
        /// </summary>
        /// <param name="input">The input string to split.</param>
        /// <returns>The set of tokens.</returns>
        internal static IList<string> SplitArgs(string input)
        {
            return Split(input, ',', new List<char>() { ' ' });
        }

        /// <summary>
        /// Locate the store an discover its executor, if applicable.
        /// </summary>
        /// <param name="uri">The uri to test.</param>
        /// <returns>The matching executor.</returns>
        internal static IExecutor ExtractStoreExecutor(Uri uri)
        {
            DatabaseType store = new DefaultStoreType();
            Dictionary<string, string> tokenMap = BuildTokenMap(uri);
            if (tokenMap.ContainsKey("store") == true)
            {
                Type type = TypeCache.LocateType(tokenMap["store"]);
                if (type != null)
                {
                    store = Activator.CreateInstance(type) as DatabaseType;
                }
            }

            return store.Executor;
        }

        /// <summary>
        /// Aligns the aliased predicates in the filter clauses.
        /// </summary>
        /// <param name="settings">The settings object to inspect.</param>
        private static void AlignAliasedPredicates(QueryBuilderSettings settings)
        {
            if (settings.IsAggregateQuery == true)
            {
                return;
            }

            foreach (AggregateColumnReference acr in settings.Aggregates)
            {
                if (acr.AggregateType == AggregateType.None)
                {
                    List<ParsedPredicate> predicates = new List<ParsedPredicate>();
                    predicates.AddRange(Flatten(settings.Filter));
                    predicates.AddRange(Flatten(settings.Where));
                    foreach (ParsedPredicate pp in predicates)
                    {
                        foreach (PropertyNameType property in pp.PropertyNames)
                        {
                            if (property.Value == acr.Alias)
                            {
                                property.AggregateColumnReference = acr;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper to extract the column string from the token map.
        /// </summary>
        /// <param name="tokenMap">The token map to inspect.</param>
        /// <returns>The column string to use.</returns>
        private static string ExtractColumnString(Dictionary<string, string> tokenMap)
        {
            if ((tokenMap.ContainsKey("aggregate") && tokenMap.ContainsKey("compute")) ||
                (tokenMap.ContainsKey("compute") && tokenMap.ContainsKey("groupby")))
            {
                throw new InvalidDataFilterException("Aggregate and compute options cannot both be provided in same query.");
            }

            if (tokenMap.ContainsKey("aggregate"))
            {
                return RemoveRootParens(tokenMap["aggregate"]);
            }
            else if (tokenMap.ContainsKey("compute"))
            {
                return RemoveRootParens(tokenMap["compute"]);
            }

            throw new InvalidDataFilterException("Column string can only be extracted if either aggregate or compute is provided.");
        }

        /// <summary>
        /// Build the token map from the Uri.
        /// </summary>
        /// <param name="odata">The uri to parse.</param>
        /// <returns>The resulting token map.</returns>
        private static Dictionary<string, string> BuildTokenMap(Uri odata)
        {
            Dictionary<string, string> tokenMap =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (odata.Query.Length > 0)
            {
                string[] tokens = Split(odata.Query.Substring(1), '&', new List<char>() { '$' });
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    string[] pairs = Split(token, '=', new List<char>());
                    if (pairs.Length == 2)
                    {
                        tokenMap[pairs[0]] = Uri.UnescapeDataString(pairs[1]);
                    }
                    else if (pairs[0] == "format" && pairs.Length == 3)
                    {
                        tokenMap[pairs[0]] = string.Concat(pairs[1], '=', pairs[2]);
                    }
                    else
                    {
                        throw new InvalidDataFilterException(
                            "Invalid QueryOption provided:" + Uri.UnescapeDataString(token));
                    }
                }
            }

            return tokenMap;
        }

        /// <summary>
        /// Parse the data filter.
        /// </summary>
        /// <param name="odata">The uri to parse.</param>
        /// <returns>The resulting filter.</returns>
        private static FilterType ParseQueryOption(Uri odata, ParsableQueryOptions option)
        {
            Dictionary<string, string> tokenMap = BuildTokenMap(odata);
            FilterType expression = null;
            if (tokenMap.ContainsKey(option.ToString()) == true)
            {
                expression = Parse(tokenMap[option.ToString()]);
            }

            return expression;
        }

        /// <summary>
        /// Parses an odata filter statement into an expression type.
        /// </summary>
        /// <param name="filter">The string to parse into an expression.</param>
        /// <returns>The expression type instance.</returns>
        private static FilterType Parse(string filter)
        {
            XElement element = Compile(filter);
            FilterType instance = new FilterType();
            instance.Deserialize(element.CreateReader());

            return instance;
        }

        /// <summary>
        /// Compile an input into its xml.
        /// </summary>
        /// <param name="input">The input to compile.</param>
        /// <returns>The compiled tree.</returns>
        private static XElement Compile(string input)
        {
            List<Token> tokens = CreateTokenList(input);
            CompileAggregate(tokens);
            CompileArithmetic(tokens);
            CompileOperators(tokens);
            CompileConjunctions(tokens);
            Token root = CompileFilterTree(tokens);
            XElement element = CreateXml(root);

            return element;
        }

        /// <summary>
        /// Compile the final list into a token tree.
        /// </summary>
        /// <param name="tokens">The list of tokens.</param>
        /// <returns>The root token.</returns>
        private static Token CompileFilterTree(List<Token> tokens)
        {
            Token root = new Token(FilterTokenValue);
            Stack<Token> stack = new Stack<Token>();
            stack.Push(root);

            Token[] array = new Token[tokens.Count];
            tokens.CopyTo(array);
            for (int i = 0; i < array.Length; i++)
            {
                SemanticToken st = array[i] as SemanticToken;
                if (st == null)
                {
                    if (array[i].Value == "(")
                    {
                        stack.Push(array[i]);
                    }
                    else if (array[i].Value == ")")
                    {
                        Token item = stack.Pop();
                        Token next = stack.Pop();
                        next.Children.Add(item);
                        stack.Push(next);
                    }
                }
                else
                {
                    Token item = stack.Pop();
                    item.Children.Add(st);
                    stack.Push(item);
                }
            }

            return root;
        }

        /// <summary>
        /// Compile the conjunction tokens from their operators.
        /// </summary>
        /// <param name="tokens">List of tokens.</param>
        private static void CompileConjunctions(List<Token> tokens)
        {
            Token[] array = new Token[tokens.Count];
            tokens.CopyTo(array);
            for (int i = 0; i < array.Length; i++)
            {
                SemanticToken st = array[i] as SemanticToken;
                if (st != null)
                {
                    if (st.IsConjunction == true)
                    {
                        SemanticToken left = array[i - 1] as SemanticToken;
                        SemanticToken right = array[i + 1] as SemanticToken;
                        if (left != null && tokens.Contains(left) == true)
                        {
                            st.Children.Add(left);
                            tokens.Remove(left);
                        }

                        if (right != null && tokens.Contains(right) == true)
                        {
                            st.Children.Add(right);
                            tokens.Remove(right);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compile the operator tokens from their statements.
        /// </summary>
        /// <param name="tokens">List of tokens.</param>
        private static void CompileOperators(List<Token> tokens)
        {
            Token[] array = new Token[tokens.Count];
            tokens.CopyTo(array);
            for (int i = 0; i < array.Length; i++)
            {
                SemanticToken st = array[i] as SemanticToken;
                if (st != null)
                {
                    if (st.IsOperator == true)
                    {
                        SemanticToken left = array[i - 1] as SemanticToken;
                        SemanticToken right = array[i + 1] as SemanticToken;
                        if (left != null && tokens.Contains(left) == true)
                        {
                            st.Children.Add(left);
                            tokens.Remove(left);
                        }

                        if (right != null && tokens.Contains(right) == true)
                        {
                            st.Children.Add(right);
                            tokens.Remove(right);
                        }
                    }
                    else if (st.IsFunction == true &&
                        IsBooleanFunction(st.Value) == true)
                    {
                        if (array.Length > i + 1)
                        {
                            SemanticToken next = array[i + 1] as SemanticToken;
                            if (next != null && next.IsOperator == true)
                            {
                                continue;
                            }
                        }

                        SemanticToken left = st;
                        SemanticToken right = new SemanticToken("true");
                        st = new SemanticToken("eq");
                        st.Children.Add(left);
                        st.Children.Add(right);
                        int pos = tokens.IndexOf(left);
                        tokens.Remove(left);
                        tokens.Insert(pos, st);
                    }
                    else if (st.IsAnyOrAll == true && array.Length > i + 2)
                    {
                        SemanticToken next = array[i + 1] as SemanticToken;
                        if (next != null && next.IsOperator == true)
                        {
                            ConfigureAnyAllValue(st, array[i + 2].Value);
                            tokens.Remove(next);
                            tokens.Remove(array[i + 2]);
                            i += 2;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compile the final math structure into a tree.
        /// </summary>
        /// <param name="tokens">The list of tokens.</param>
        /// <returns>The root token.</returns>
        private static Token CompileArithmeticTree(List<Token> tokens)
        {
            Token root = new Token("Eq");
            Stack<Token> stack = new Stack<Token>();
            stack.Push(root);

            Token[] array = new Token[tokens.Count];
            tokens.CopyTo(array);
            for (int i = 0; i < array.Length; i++)
            {
                SemanticToken st = array[i] as SemanticToken;
                if (st == null)
                {
                    if (array[i].Value == "(")
                    {
                        stack.Push(array[i]);
                    }
                    else if (array[i].Value == ")")
                    {
                        Token item = stack.Pop();
                        Token next = stack.Pop();
                        next.Children.Add(item);
                        stack.Push(next);
                    }
                }
                else
                {
                    Token item = stack.Pop();
                    item.Children.Add(st);
                    stack.Push(item);
                }
            }

            Token compiled = ArithmeticHelper.SetPrecedence(root);
            SemanticToken test = compiled as SemanticToken;
            while (test == null && compiled != null)
            {
                test = compiled.Children.SingleOrDefault() as SemanticToken;
                compiled = compiled.Children.FirstOrDefault();
            }

            return test;
        }

        /// <summary>
        /// Compile the operator tokens from their statements.
        /// </summary>
        /// <param name="tokens">List of tokens.</param>
        private static void CompileArithmetic(List<Token> tokens)
        {
            Token[] array = new Token[tokens.Count];
            tokens.CopyTo(array);
            List<Token> buffer = new List<Token>();
            for (int i = 0; i < array.Length; i++)
            {
                SemanticToken st = array[i] as SemanticToken;
                if (st != null)
                {
                    if (st.IsArithmetic == true)
                    {
                        if (buffer.Contains(array[i - 1]) == false)
                        {
                            buffer.Add(array[i - 1]);
                        }

                        buffer.Add(st);
                        if (buffer.Contains(array[i + 1]) == false)
                        {
                            buffer.Add(array[i + 1]);
                        }
                    }
                    else if (st.IsOperator == true ||
                        st.IsConjunction == true)
                    {
                        if (buffer.OfType<SemanticToken>().Count() > 0)
                        {
                            List<Token> cleaned = CleanBuffer(buffer);
                            Token root = CompileArithmeticTree(cleaned);
                            foreach (Token token in cleaned)
                            {
                                if (token != root && root != null)
                                {
                                    tokens.Remove(token);
                                }
                            }
                        }

                        buffer.Clear();
                    }
                }
                else if (buffer.Contains(array[i]) == false &&
                    (array[i].Value == "(" || array[i].Value == ")"))
                {
                    if (array[i].Value == ")" && 
                        buffer.Contains(array[i - 1]) == false && 
                        buffer.Any(p => p.Value == "(") == true)
                    {
                        buffer.Add(array[i - 1]);
                    }

                    buffer.Add(array[i]);
                }
            }

            if (buffer.OfType<SemanticToken>().Count() > 0)
            {
                List<Token> cleaned = CleanBuffer(buffer);
                Token last = CompileArithmeticTree(cleaned);
                foreach (Token token in cleaned)
                {
                    if (token != last && last != null)
                    {
                        tokens.Remove(token);
                    }
                }
            }
        }

        /// <summary>
        /// Clean a list of tokens of any extra parens at start or end of list.
        /// </summary>
        /// <param name="buffer">The list to clean.</param>
        /// <returns>The cleaned list.</returns>
        private static List<Token> CleanBuffer(List<Token> buffer)
        {
            int level = 0;
            foreach (Token token in buffer)
            {
                if (token.Value == "(")
                {
                    level++;
                }
                else if (token.Value == ")")
                {
                    level--;
                }
            }

            List<Token> result = null;
            Token[] array;
            if (level > 0)
            {
                array = new Token[buffer.Count - level];
                buffer.CopyTo(level, array, 0, buffer.Count - level);
                result = array.ToList();
            }
            else if (level < 0)
            {
                array = new Token[buffer.Count - (level * -1)];
                buffer.CopyTo(0, array, 0, buffer.Count + level);
                result = array.ToList();
            }
            else
            {
                result = buffer;
            }

            return result;
        }

        /// <summary>
        /// Compile the aggregates in the list of tokens.
        /// </summary>
        /// <param name="tokens">The list of tokens to compile.</param>
        private static void CompileAggregate(List<Token> tokens)
        {
            Token[] array = new Token[tokens.Count];
            tokens.CopyTo(array);
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Value == "with")
                {
                    List<Token> buffer = FillAggregateBuffer(array, i - 1);
                    foreach (Token item in buffer)
                    {
                        tokens.Remove(item);
                    }

                    CompileArithmetic(buffer);
                    if (buffer.Count != 1)
                    {
                        throw new InvalidDataFilterException("Illegal Aggregate Column specified.");
                    }

                    array[i].Children.Add(buffer.Single());
                    array[i].Children.Add(array[i + 1]);
                    tokens.Remove(array[i + 1]);
                    i++;
                }
            }
        }

        /// <summary>
        /// Fill the buffer for an aggregate column expression.
        /// </summary>
        /// <param name="array">The array to walk backwards.</param>
        /// <param name="start">The position to begin.</param>
        /// <returns>The populated buffer.</returns>
        private static List<Token> FillAggregateBuffer(Token[] array, int start)
        {
            List<Token> buffer = new List<Token>();
            int level = 0;
            for (int i = start; i >= 0; i--)
            {
                if (array[i].Value == ")")
                {
                    level++;
                }
                else if (array[i].Value == "(")
                {
                    level--;
                }

                if (level < 0)
                {
                    break;
                }
                else if (array[i].Value == "with" && level == 0)
                {
                    buffer.Remove(array[i + 1]);
                    buffer.Remove(array[i + 2]);
                    break;
                }

                buffer.Insert(0, array[i]);
            }

            return buffer;
        }

        /// <summary>
        /// Creates a list of tokens from the given filter statement.
        /// </summary>
        /// <param name="filter">The filter to inspect.</param>
        /// <returns>The resulting list of tokens.</returns>
        private static List<Token> CreateTokenList(string filter)
        {
            bool inlist = false;
            bool quoted = false;
            bool function = false;
            int depth = 0;

            List<Token> tokens = new List<Token>();
            StringBuilder token = new StringBuilder();
            foreach (char c in filter)
            {
                if (c == '\'' && quoted == false)
                {
                    token.Append(c);
                    quoted = true;
                }
                else if (c == '\'' && quoted == true)
                {
                    token.Append(c);
                    quoted = false;
                }
                else if (c == '(' && quoted == false && function == true)
                {
                    depth++;
                    token.Append(c);
                }
                else if (c == '(' && quoted == false)
                {
                    function = IsFunction(token.ToString());
                    if (function == false)
                    {
                        if (token.Length > 0)
                        {
                            tokens.Add(new SemanticToken(token.ToString()));
                            token.Clear();
                        }

                        tokens.Add(new Token(c.ToString()));
                        continue;
                    }
                    else
                    {
                        token.Append(c);
                    }
                }
                else if (c == ')' && quoted == false && depth > 0)
                {
                    depth--;
                    token.Append(c);
                }
                else if (c == ')' && quoted == false)
                {
                    if (function == false)
                    {
                        if (token.Length > 0)
                        {
                            tokens.Add(new SemanticToken(token.ToString(), inlist));
                            token.Clear();
                        }

                        inlist = false;
                        tokens.Add(new Token(c.ToString()));
                        continue;
                    }
                    else
                    {
                        function = false;
                        token.Append(c);
                        tokens.Add(new SemanticToken(token.ToString()));
                        token.Clear();
                    }
                }
                else if (c == ' ' && quoted == false && function == false && IsUnary(token.ToString()) == false && inlist == false)
                {
                    if (token.Length > 0)
                    {
                        tokens.Add(new SemanticToken(token.ToString()));
                        inlist = IsListOperator(token.ToString());
                        token.Clear();
                    }
                }
                else
                {
                    token.Append(c);
                }
            }

            if (token.Length > 0)
            {
                tokens.Add(new SemanticToken(token.ToString()));
            }

            return tokens;
        }

        /// <summary>
        /// Assign the updated value for any/all functions.
        /// </summary>
        /// <param name="st">The semantic token of the any/all.</param>
        /// <param name="value">The value to assign.</param>
        private static void ConfigureAnyAllValue(SemanticToken st, string value)
        {
            bool defaultValue = bool.Parse(st.Xml.Attribute("Value").Value);
            bool bvalue = bool.Parse(value);
            if (defaultValue == false)
            {
                bvalue = !bvalue;
                st.Xml.Attribute("Value").Value = bvalue.ToString().ToLower();
            }
            else
            {
                st.Xml.Attribute("Value").Value = value;
            }
        }

        /// <summary>
        /// Tests string against list of known unary functions.
        /// Unary functions are preceded and followed by a space.
        /// They do not occur within parentheses or wihin quotes.
        /// This function assumes the caller has determined that
        /// these conditions have been met.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if unary function, otherwise false.</returns>
        private static bool IsUnary(string value)
        {
            return value.Equals("Not", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests string against list of supported functions.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if function, otherwise false.</returns>
        private static bool IsFunction(string value)
        {
            return value.Equals("Contains", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Not Contains", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("ToLower", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("ToUpper", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("StartsWith", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Not StartsWith", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("EndsWith", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Not EndsWith", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Length", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Substring", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Trim", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Concat", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Year", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Month", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Day", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Hour", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Minute", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Second", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("FractionalSeconds", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Date", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Time", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("TotalOffsetMinutes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("TotalSeconds", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Now", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("MinDateTime", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("MaxDateTime", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Round", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Floor", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Ceiling", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Cast", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("IsOf", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Not IsOf", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Geo.Distance", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Geo.Length", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Geo.Intersects", StringComparison.OrdinalIgnoreCase) ||
                value.EndsWith("/any", StringComparison.OrdinalIgnoreCase) ||
                value.EndsWith("/all", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("rollup", StringComparison.OrdinalIgnoreCase) ||
                QueryBuilderSettings.UserDefinedFunctions().ContainsKey(value) == true;
        }

        /// <summary>
        /// Tests string against list of known boolean functions:
        /// functions that return a boolean value and so may be written
        /// in shorthand form that omits the comparison value of true.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if function, otherwise false.</returns>
        private static bool IsBooleanFunction(string value)
        {
            return value.StartsWith("Contains(", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("StartsWith(", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("EndsWith(", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("IsOf(", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Not Contains(", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Not StartsWith(", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Not EndsWith(", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Not IsOf(", StringComparison.OrdinalIgnoreCase) ||
                value.ToLower().Contains("/any(") ||
                value.ToLower().Contains("/all(");
        }

        /// <summary>
        /// Tests a string to determine if it is followed by an list of values.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if the token precedes a list, otherwise false.</returns>
        private static bool IsListOperator(string value)
        {
            return value.Equals("in", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Split the given string using the provided char. 
        /// Omitting characters inside single quotes or inside parens.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="split">The split character.</param>
        /// <param name="ignores">The list of chars to ignore at the root level of the string.</param>
        /// <returns>The list of tokens.</returns>
        private static string[] Split(string input, char split, List<char> ignores)
        {
            List<string> tokens = new List<string>();
            bool quoted = false;
            int level = 0;
            StringBuilder token = new StringBuilder();

            foreach (char c in input)
            {
                // Causes problems if there is an apostrophe inside the quoted string.
                if (c == '\'')
                {
                    quoted = quoted == false ? true : false;
                }
                else if (c == '(')
                {
                    level++;
                }
                else if (c == ')')
                {
                    level--;
                }

                if (ignores.Contains(c) == true && quoted == false && level == 0)
                {
                    continue;
                }
                else if (c == split && quoted == false && level == 0)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }
                else
                {
                    token.Append(c);
                }
            }

            if (token.Length > 0)
            {
                tokens.Add(token.ToString());
            }

            return tokens.ToArray();
        }

        /// <summary>
        /// Creates an xml reader for the filter.
        /// </summary>
        /// <param name="root">The filter root.</param>
        /// <returns>The xml.</returns>
        private static XElement CreateXml(Token root)
        {
            XElement element = root.Merge();

            return element;
        }

        /// <summary>
        /// Extract the subject and predicate from the statement.
        /// </summary>
        /// <param name="statement">The statement to shred.</param>
        /// <param name="name">The resulting name.</param>
        /// <param name="value">The resulting value.</param>
        /// <returns>True if the predicate is shredded, otherwise false.</returns>
        private static bool TryShredPredicate(
            PredicateType statement,
            out string name,
            out object value)
        {
            name = null;
            value = null;

            IPredicatable predicatable = statement.FindSubject();
            if (predicatable != null)
            {
                name = predicatable.LookupPropertyName();
                value = predicatable.LookupPropertyValue(statement);
                if (value is NullType)
                {
                    value = null;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove root parens from an input.
        /// </summary>
        /// <param name="input">The input to test.</param>
        /// <returns>The cleaned string.</returns>
        private static string RemoveRootParens(string input)
        {
            Stack<string> stack = new Stack<string>();
            stack.Push(input);
            string output = null;
            while (stack.Count > 0)
            {
                string test = stack.Pop();
                output = test;
                bool wrapped = false;
                List<Token> tokens = CreateTokenList(test);
                if (tokens.First().Value == "(" && tokens.Last().Value == ")")
                {
                    wrapped = true;
                    int level = 0;
                    foreach (Token token in tokens)
                    {
                        if (token.Value == "(")
                        {
                            level++;
                        }
                        else if (token.Value == ")")
                        {
                            level--;
                        }

                        if (level == 0 && token != tokens.Last())
                        {
                            wrapped = false;
                            break;
                        }
                    }
                }

                if (wrapped == true)
                {
                    output = test.Substring(1, test.Length - 2);
                }

                if (output != test)
                {
                    stack.Push(output);
                }
            }

            return output;
        }

        /// <summary>
        /// Fix the enums in the settings' filters.
        /// </summary>
        /// <param name="entitySetType">The root type.</param>
        /// <param name="settings">The constructed settings.</param>
        private static void FixEnums(Type entitySetType, QueryBuilderSettings settings)
        {
            if (entitySetType == null)
            {
                return;
            }

            List<ParsedPredicate> predicates = new List<ParsedPredicate>();
            predicates.AddRange(Flatten(settings.Filter));
            predicates.AddRange(Flatten(settings.Where));
            predicates.AddRange(Flatten(settings.ArgumentFilter));
            foreach (ParsedPredicate pp in predicates)
            {
                foreach (PropertyNameType pnt in pp.PropertyNames)
                {
                    Type type = TypeCache.LocatePropertyType(entitySetType, pnt.Prefix ?? string.Empty) ?? entitySetType;
                    if (type == null)
                    {
                        continue;
                    }

                    IEnumerable<PropertyInfo> properties = type.GetProperties().Where(p => p.PropertyType.IsEnum == true);
                    if (properties.Any(p => p.Name == pnt.Value) &&
                        (pp.Item.Predicate is EnumValueType) == false &&
                        (pp.Item.Subject is EnumValueType) == false)
                    {
                        PropertyInfo pi = properties.Single(p => p.Name == pnt.Value);
                        EnumValueType evt = new EnumValueType();
                        evt.Type = pi.PropertyType.FullName;
                        if (pp.Item.Subject == pnt)
                        {
                            IPredicatable predicatable = pp.Item.Predicate as IPredicatable;
                            evt.Value = predicatable == null ? pp.Item.Predicate.ToString() : predicatable.Serialize();
                            pp.Item.Predicate = evt;
                        }
                        else if (pp.Item.Predicate == pnt)
                        {
                            IPredicatable predicatable = pp.Item.Subject as IPredicatable;
                            evt.Value = predicatable == null ? pp.Item.Subject.ToString() : predicatable.Serialize();
                            pp.Item.Subject = evt;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper class for storing token value and level for semantic elements.
        /// </summary>
        private class SemanticToken : Token
        {
            /// <summary>
            /// Initializes an instance of the SemanticToken class.
            /// </summary>
            /// <param name="value">The value of the token.</param>
            /// <param name="inlist">Indicates whether value is a list of values.</param>
            public SemanticToken(string value, bool inlist = false)
                : base(value)
            {
                Dictionary<string, Tuple<string, bool, bool, bool>> elementMap = CreateElementMap();

                if (elementMap.ContainsKey(value) == true)
                {
                    this.Xml = new XElement(elementMap[value].Item1);
                    this.IsOperator = elementMap[value].Item2;
                    this.IsConjunction = elementMap[value].Item3;
                    this.IsArithmetic = elementMap[value].Item4;
                }
                else
                {
                    this.Xml = ConfigureXml(value, inlist);
                    this.IsFunction = this.Xml.Name == "Function";
                    this.IsAnyOrAll = this.Xml.Name == "Any" || this.Xml.Name == "All";
                }
            }

            /// <summary>
            /// Gets a value indicating whether the token is an any or all type.
            /// </summary>
            public bool IsAnyOrAll
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets a value indicating whether the token is a function call.
            /// </summary>
            public bool IsFunction
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets a value indicating whether the token is an operator.
            /// </summary>
            public bool IsOperator
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets a value indicating whether the token is a conjunction.
            /// </summary>
            public bool IsConjunction
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets a value indicating whether the token is an arithemtic operator.
            /// </summary>
            public bool IsArithmetic
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the expression.
            /// </summary>
            public XElement Xml
            {
                get;
                private set;
            }

            /// <summary>
            /// Return the current expression.
            /// </summary>
            /// <returns>The xml output for the current node.</returns>
            public override XElement Merge()
            {
                foreach (Token child in this.Children)
                {
                    XElement element = child.Merge();
                    this.Xml.Add(element);
                }

                return this.Xml;
            }

            /// <summary>
            /// Creates the element map for use with semantic tokens.
            /// </summary>
            /// <returns>The map of uri semantic items to xsd elements.</returns>
            private static Dictionary<string, Tuple<string, bool, bool, bool>> CreateElementMap()
            {
                Dictionary<string, Tuple<string, bool, bool, bool>> elementMap =
                    new Dictionary<string, Tuple<string, bool, bool, bool>>();

                elementMap["in"] = new Tuple<string, bool, bool, bool>("In", true, false, false);
                elementMap["has"] = new Tuple<string, bool, bool, bool>("Has", true, false, false);
                elementMap["ge"] = new Tuple<string, bool, bool, bool>("GreaterThanOrEqual", true, false, false);
                elementMap["gt"] = new Tuple<string, bool, bool, bool>("GreaterThan", true, false, false);
                elementMap["le"] = new Tuple<string, bool, bool, bool>("LessThanOrEqual", true, false, false);
                elementMap["lt"] = new Tuple<string, bool, bool, bool>("LessThan", true, false, false);
                elementMap["ne"] = new Tuple<string, bool, bool, bool>("NotEqual", true, false, false);
                elementMap["eq"] = new Tuple<string, bool, bool, bool>("Equal", true, false, false);
                elementMap["and"] = new Tuple<string, bool, bool, bool>("And", false, true, false);
                elementMap["or"] = new Tuple<string, bool, bool, bool>("Or", false, true, false);
                elementMap["null"] = new Tuple<string, bool, bool, bool>("Null", false, false, false);
                elementMap["add"] = new Tuple<string, bool, bool, bool>("Add", false, false, true);
                elementMap["sub"] = new Tuple<string, bool, bool, bool>("Sub", false, false, true);
                elementMap["mul"] = new Tuple<string, bool, bool, bool>("Mul", false, false, true);
                elementMap["div"] = new Tuple<string, bool, bool, bool>("Div", false, false, true);
                elementMap["divby"] = new Tuple<string, bool, bool, bool>("DivBy", false, false, true);
                elementMap["mod"] = new Tuple<string, bool, bool, bool>("Mod", false, false, true);
                elementMap["with"] = new Tuple<string, bool, bool, bool>("With", false, false, false);
                elementMap["sum"] = new Tuple<string, bool, bool, bool>("Sum", false, false, false);
                elementMap["min"] = new Tuple<string, bool, bool, bool>("Min", false, false, false);
                elementMap["max"] = new Tuple<string, bool, bool, bool>("Max", false, false, false);
                elementMap["avg"] = new Tuple<string, bool, bool, bool>("Average", false, false, false);
                elementMap["count"] = new Tuple<string, bool, bool, bool>("Count", false, false, false);
                elementMap["countdistinct"] = new Tuple<string, bool, bool, bool>("CountDistinct", false, false, false);
                elementMap["none"] = new Tuple<string, bool, bool, bool>("None", false, false, false);
                elementMap["merge"] = new Tuple<string, bool, bool, bool>("Merge", false, false, false);
                elementMap[FilterTokenValue] = new Tuple<string, bool, bool, bool>("Where", false, false, false);

                return elementMap;
            }

            /// <summary>
            /// Configure xml node for given token value.
            /// </summary>
            /// <param name="value">The token value.</param>
            /// <param name="inlist">Indicates whether the value is a list of items.</param>
            /// <returns>The xml node.</returns>
            private static XElement ConfigureXml(string value, bool inlist)
            {
                XElement xml = ConfigureXmlForValue(value, inlist);
                if (xml == null)
                {
                    xml = ConfigureXmlForPredicate(value);
                }

                return xml;
            }

            /// <summary>
            /// Configure xml for a predicate token.
            /// </summary>
            /// <param name="value">The token value to test.</param>
            /// <returns>The configured xml.</returns>
            private static XElement ConfigureXmlForPredicate(string value)
            {
                XElement xml = null;
                if (value.ToLower().Contains("/any(") || value.ToLower().Contains("/all("))
                {
                    xml = ConfigureXmlForAnyOrAll(value);
                }
                else if (value[value.Length - 1] == ')')
                {
                    xml = new XElement("Function", value);
                }
                else if (value[0] == '@')
                {
                    xml = new XElement("Parameter", value);
                }
                else
                {
                    xml = new XElement("PropertyName", value);
                }

                return xml;
            }

            /// <summary>
            /// Configure xml for any or all tokens.
            /// </summary>
            /// <param name="value">The token value to test.</param>
            /// <returns>The confiugred xml.</returns>
            private static XElement ConfigureXmlForAnyOrAll(string value)
            {
                XElement xml = null;
                string name = value.Substring(0, value.IndexOf('('));
                string args = value.Substring(value.IndexOf('('));
                bool isany = name.ToLower().EndsWith("/any");
                if (isany == true)
                {
                    xml = new XElement("Any");
                }
                else
                {
                    xml = new XElement("All");
                }

                bool defaultValue = true;
                if (name.StartsWith("Not ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    name = name.Split(' ')[1];
                    defaultValue = false;
                }

                int pos = name.LastIndexOf('/');
                xml.Add(new XAttribute("Name", name.Substring(0, pos)));
                xml.Add(new XAttribute("Value", defaultValue));

                if (args.Length > 2)
                {
                    string data = args.Substring(1, args.Length - 2);
                    int colon = data.IndexOf(':');
                    string p = data.Substring(0, colon) + '/';
                    data = data.Substring(colon + 1).Trim();
                    data = data.Replace(string.Concat(" ", p), " ").Replace(string.Concat("(", p), "(").Replace(string.Concat(",", p), ",");
                    if (data.StartsWith(p) == true)
                    {
                        data = data.Substring(p.Length);
                    }

                    XElement element = DataFilterParsingHelper.Compile(data);
                    xml.Add(element);
                }

                return xml;
            }

            /// <summary>
            /// Configure xml for a value token.
            /// </summary>
            /// <param name="value">The token value to test.</param>
            /// <param name="inlist">Indicates whether the value is a list of items.</param>
            /// <returns>The configured xml.</returns>
            private static XElement ConfigureXmlForValue(string value, bool inlist)
            {
                int result;
                bool bresult;
                DateTimeOffset dresult;
                DateTime dtresult;
                double dbresult;
                Guid gresult;
                string name, type;
                XElement xml = null;
                if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    xml = new XElement("Null");
                }
                else if (DataFilterParsingHelper.IsDateTimeOffset(value) == true)
                {
                    DateTimeOffset.TryParse(value.Replace("'", string.Empty), out dresult);
                    xml = new XElement("DateTimeOffset", dresult.ToString("o"));
                }
                else if (inlist == true)
                {
                    xml = BuildList(value);
                }
                else if (value[0] == '\'')
                {
                    string data = value.Substring(1, value.Length - 2);
                    if (data.Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        xml = new XElement("Null");
                    }
                    else
                    {
                        xml = new XElement("String", data);
                    }
                }
                else if (int.TryParse(value, out result) == true)
                {
                    xml = new XElement("Int", value);
                }
                else if (bool.TryParse(value, out bresult) == true)
                {
                    xml = new XElement("Boolean", value);
                }
                else if (DataFilterParsingHelper.IsLong(value) == true)
                {
                    xml = new XElement("Long", value.Substring(0, value.Length - 1));
                }
                else if (DateTimeOffset.TryParseExact(value, "o", null, System.Globalization.DateTimeStyles.None, out dresult) == true)
                {
                    xml = new XElement("DateTimeOffset", value);
                }
                else if (DateTime.TryParseExact(value, "o", null, System.Globalization.DateTimeStyles.None, out dtresult) == true)
                {
                    xml = new XElement("DateTime", value);
                }
                else if (value.StartsWith("duration'") == true)
                {
                    xml = new XElement("TimeSpan", ConvertToTimeSpan(value));
                }
                else if (TryParseEnumValueType(value, out type, out name) == true)
                {
                    xml = new XElement("EnumValue", name);
                    xml.Add(new XAttribute("Type", type));
                }
                else if (double.TryParse(value, out dbresult) == true)
                {
                    xml = new XElement("Double", value);
                }
                else if (Guid.TryParse(value, out gresult) == true)
                {
                    xml = new XElement("Guid", value);
                }
                else
                {
                    string[] bits = value.Split(' ');
                    if (bits.Length == 2 &&
                        DataFilterParsingHelper.IsUnary(bits[0]) == true &&
                        bool.TryParse(bits[1], out bresult) == true)
                    {
                        xml = new XElement("Boolean", !bool.Parse(bits[1]));
                    }
                }

                return xml;
            }

            /// <summary>
            /// Build a list element.
            /// </summary>
            /// <param name="value">The value to build into a list.</param>
            /// <returns>The list element.</returns>
            private static XElement BuildList(string value)
            {
                XElement list = new XElement("List");
                string[] members = Split(value, ',', new List<char>());
                foreach (string member in members)
                {
                    XElement e = ConfigureXmlForValue(member.Trim(), false);
                    list.Add(e);
                }

                return list;
            }

            /// <summary>
            /// Convert the provided value to a timespan.
            /// </summary>
            /// <param name="value">The supplied value.</param>
            /// <returns>The corresponding timespan.</returns>
            private static TimeSpan ConvertToTimeSpan(string value)
            {
                string trimmed = value.Replace("duration'", string.Empty);
                if (trimmed[trimmed.Length - 1] == '\'')
                {
                    trimmed = trimmed.Substring(0, trimmed.Length - 1);
                }

                TimeSpan ts = XmlConvert.ToTimeSpan(trimmed);

                return ts;
            }

            /// <summary>
            /// Test input to determine if it is an enum value type.
            /// </summary>
            /// <param name="value">The value to test.</param>
            /// <param name="type">The type string, if applicable.</param>
            /// <param name="name">The enum value string, if applicable.</param>
            /// <returns>True if the input is an enumtypevalue, otherwise false.</returns>
            private static bool TryParseEnumValueType(string value, out string type, out string name)
            {
                type = null;
                name = null;
                if (string.IsNullOrEmpty(value) == true)
                {
                    return false;
                }

                string[] parts = value.Split(new char[] { '\'' });
                if (parts.Length == 3 && parts[2] == string.Empty && string.IsNullOrEmpty(parts[0]) == false)
                {
                    name = parts[1];
                    type = parts[0];
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Helper class for storing token value and level.
        /// </summary>
        private class Token
        {
            /// <summary>
            /// Initializes an instance of the Token class.
            /// </summary>
            /// <param name="value">The value of the token.</param>
            public Token(string value)
            {
                this.Children = new List<Token>();
                this.Value = value;
            }

            /// <summary>
            /// Gets the token value.
            /// </summary>
            public string Value
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets teh list of children nodes.
            /// </summary>
            public List<Token> Children
            {
                get;
                private set;
            }

            /// <summary>
            /// Override of the tostring method.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Value;
            }

            /// <summary>
            /// Merge the xml from each of the current node's children.
            /// </summary>
            /// <returns>The merged xml.</returns>
            public virtual XElement Merge()
            {
                XElement result = null;
                if (this.Children.Count == 1)
                {
                    result = this.Children[0].Merge();
                }
                else
                {
                    Queue<XElement> queue = new Queue<XElement>();
                    for (int i = 0; i < this.Children.Count; i++)
                    {
                        SemanticToken st = this.Children[i] as SemanticToken;
                        if (st != null)
                        {
                            XElement xml = st.Merge();
                            if (result == null)
                            {
                                result = xml;
                            }
                            else
                            {
                                // Or takes precedence over other tokens at same level.
                                // where no parentheses have been provided.
                                if (st.Xml.Name.LocalName.Equals(result.Name.LocalName) == true)
                                {
                                    foreach (XElement c in xml.Elements())
                                    {
                                        result.Add(c);
                                    }
                                }
                                else
                                {
                                    if (result.Name.LocalName.Equals("Or") == true)
                                    {
                                        result.Add(xml);
                                    }
                                    else
                                    {
                                        xml.Add(result);
                                        result = xml;
                                    }
                                }
                            }
                        }
                        else
                        {
                            queue.Enqueue(this.Children[i].Merge());
                        }
                    }

                    // Handle the non-semantic tokens last.
                    while (queue.Count > 0)
                    {
                        XElement xml = queue.Dequeue();
                        if (result != null)
                        {
                            result.Add(xml);
                        }
                        else
                        {
                            result = xml;
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Helper class to compile the arithmetic.
        /// </summary>
        private static class ArithmeticHelper
        {
            /// <summary>
            /// Construct the arithmetic nodes according to arithmetic precedence.
            /// </summary>
            /// <param name="root">The root node to process.</param>
            /// <returns>The compiled and properly ordered token.</returns>
            public static Token SetPrecedence(Token root)
            {
                Stack<Token> stack = new Stack<Token>();
                stack.Push(root);
                while (stack.Count > 0)
                {
                    Token token = stack.Pop();
                    foreach (Token child in token.Children)
                    {
                        stack.Push(child);
                    }

                    if (token.Children.Count % 2 == 0)
                    {
                        continue;
                    }

                    for (int i = token.Children.Count - 2; i >= 0; i = i - 2)
                    {
                        StackRank(token, i);
                    }
                }

                return root.Children.SingleOrDefault();
            }

            /// <summary>
            /// Stack rank the arithmetic nodes at the specified location.
            /// </summary>
            /// <param name="token">The container token.</param>
            /// <param name="index">The specified location.</param>
            private static void StackRank(Token token, int index)
            {
                SemanticToken st = token.Children[index] as SemanticToken;
                if (st != null && st.IsArithmetic == true)
                {
                    Token pre = token.Children[index - 1];
                    Token post = token.Children[index + 1];
                    SemanticToken stpre = pre as SemanticToken;
                    SemanticToken stpost = post as SemanticToken;
                    if (stpre == null || stpre.IsArithmetic == false)
                    {
                        SimpleSwap(token, st, pre);
                    }
                    else
                    {
                        bool hp = HasPrecedence(st, stpre, true);
                        if (hp == true)
                        {
                            RotateSwap(token, st, pre, false);
                        }
                        else
                        {
                            SimpleSwap(token, st, pre);
                        }
                    }

                    if (stpost == null || stpost.IsArithmetic == false)
                    {
                        SimpleSwap(token, st, post);
                    }
                    else
                    {
                        bool hp = HasPrecedence(st, stpost, false);
                        if (hp == true)
                        {
                            RotateSwap(token, st, post, true);
                        }
                        else
                        {
                            SimpleSwap(token, st, post);
                        }
                    }
                }
            }

            /// <summary>
            /// Rotate the child / node as indicated.
            /// </summary>
            /// <param name="parent">The parent token.</param>
            /// <param name="node">The node to test.</param>
            /// <param name="swap">The swap node.</param>
            /// <param name="right">True to rotate to the right, otherwise false.</param>
            private static void RotateSwap(Token parent, Token node, Token swap, bool right)
            {
                if (right == true)
                {
                    node.Children.Add(swap.Children[0]);
                }
                else
                {
                    node.Children.Insert(0, swap.Children[0]);
                }

                swap.Children.Remove(swap.Children[0]);
                if (right == true)
                {
                    swap.Children.Insert(0, node);
                }
                else
                {
                    swap.Children.Add(node);
                }

                parent.Children.Remove(node);
            }

            /// <summary>
            /// Simply swap the indicated nodes.
            /// </summary>
            /// <param name="parent">The parent token.</param>
            /// <param name="node">The node to test.</param>
            /// <param name="swap">The swap node.</param>
            private static void SimpleSwap(Token parent, Token node, Token swap)
            {
                node.Children.Add(swap);
                parent.Children.Remove(swap);
            }

            /// <summary>
            /// Helper to determine whether one arithmetic function has precendence over another.
            /// </summary>
            /// <param name="token">The token being tested.</param>
            /// <param name="other">The other token.</param>
            /// <param name="otherIsFirst">True if other token comes first in order.</param>
            /// <returns>True if the first token has precedence, otherwise false.</returns>
            private static bool HasPrecedence(SemanticToken token, SemanticToken other, bool otherIsFirst)
            {
                switch (token.Value)
                {
                    case "divby":
                    case "div":
                        if (otherIsFirst == true)
                        {
                            return other.Value != "div" && other.Value != "divby" && other.Value != "mul";
                        }
                        else
                        {
                            return true;
                        }

                    case "mul":
                        if (otherIsFirst == true)
                        {
                            return other.Value != "div" && other.Value != "divby" && other.Value != "mul";
                        }
                        else
                        {
                            return true;
                        }

                    case "add":
                        if (otherIsFirst == true)
                        {
                            return true;
                        }
                        else
                        {
                            return other.Value != "div" && other.Value != "divby" && other.Value != "mul";
                        }
                        
                    case "sub":
                        if (otherIsFirst == true)
                        {
                            return true;
                        }
                        else
                        {
                            return other.Value != "div" && other.Value != "divby" && other.Value != "mul";
                        }

                    default:
                        return false;
                }
            }
        }
    }
}