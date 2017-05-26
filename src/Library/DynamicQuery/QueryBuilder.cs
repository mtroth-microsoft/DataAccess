// -----------------------------------------------------------------------
// <copyright file="QueryBuilder.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// QueryBuilder helper class.
    /// </summary>
    public class QueryBuilder<T> : IQueryBuilder
    {
        /// <summary>
        /// Internal alias template.
        /// </summary>
        private string template = "Alias";

        /// <summary>
        /// Map of created tables.
        /// </summary>
        private Dictionary<string, QuerySource> tables = new Dictionary<string, QuerySource>();

        /// <summary>
        /// Internal alias counter.
        /// </summary>
        private int counter;

        /// <summary>
        /// Initializes a new instance of the QueryBuilder class.
        /// This constructor allows the caller to omit $select parsing.
        /// </summary>
        /// <param name="settings">The query builder settings to use.</param>
        public QueryBuilder(QueryBuilderSettings settings)
        {
            this.template = settings.Template ?? this.template;

            QueryBuilderSettings local = new QueryBuilderSettings();
            local.Filter = settings.Filter == null ? null : settings.Filter.DeepCopy();
            local.Where = settings.Where == null ? null : settings.Where.DeepCopy();
            local.Groupings.AddRange(settings.Groupings);
            local.Template = settings.Template;
            local.Aggregates.AddRange(settings.Aggregates);
            local.Selects.AddRange(settings.Selects);
            local.Orderings.AddRange(settings.Orderings);
            local.QueryContext = settings.QueryContext;
            local.Skip = settings.Skip;
            local.Top = settings.Top;
            local.QueryProcessor = settings.QueryProcessor;
            local.ArgumentFilter = settings.ArgumentFilter == null ? null : settings.ArgumentFilter.DeepCopy();
            local.Url = settings.Url;
            local.EntitySetType = settings.EntitySetType;
            local.DefaultJoinType = settings.DefaultJoinType;
            local.Executor = settings.Executor;
            ConfigureAbstractGroupBy(local);

            this.Settings = local;
            this.Query = this.CreateQuery(local);
        }

        /// <summary>
        /// Gets the built query.
        /// </summary>
        public SelectQuery Query
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the query builder settings.
        /// </summary>
        public QueryBuilderSettings Settings
        {
            get;
            private set;
        }

        /// <summary>
        /// Run the built query.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIds">The shard ids.</param>
        /// <param name="timeout">The query timeout or null to use the default.</param>
        /// <returns>The results of the query.</returns>
        internal IQueryable<T> Run(
            DatabaseType databaseType, 
            IEnumerable<ShardIdentifier> shardIds,
            TimeSpan? timeout = null)
        {
            if (this.Query.PathQuery != null)
            {
                StoredProcedureMultiple spm = this.Settings.Executor.RunMultiple(this.Query, databaseType, shardIds, timeout);
                return this.Settings.Executor.ProjectionHelper.LoadDataFromProcedure<T>(this.Query, spm);
            }
            else if (this.Settings.IsAggregateQuery == true &&
                this.Query.Columns.Any(p => p.Source != null && string.IsNullOrEmpty(p.Source.Path) == false) == true)
            {
                StoredProcedureMultiple spm = this.Settings.Executor.RunMultiple(this.Query, databaseType, shardIds, timeout);
                return AggregateHelper.Load<T>(this.Query, spm);
            }
            else
            {
                return this.Settings.Executor.Run<T>(this.Query, databaseType, shardIds, timeout);
            }
        }

        /// <summary>
        /// Align a filter with the built query.
        /// </summary>
        /// <param name="filter">The filter to align.</param>
        internal void Align(FilterType filter)
        {
            AlignColumns(filter, this.Query.AllColumns, this.Query.RootNode, this.CreateTable);
        }

        /// <summary>
        /// Align a predicatable with the built query.
        /// </summary>
        /// <param name="predicatable">The predicatable to align.</param>
        internal void Align(IPredicatable predicatable)
        {
            AlignColumnsToPredicatable(predicatable, this.Query.AllColumns, this.Query.RootNode, this.CreateTable);
        }

        /// <summary>
        /// Align the columns in the orderby with those in the query.
        /// </summary>
        /// <param name="settings">The settings to inspect.</param>
        /// <param name="columns">The list of columns.</param>
        /// <param name="node">The composite node to work with.</param>
        /// <param name="tableCreator">The opertion to create discovered tables.</param>
        private static void AlignColumnsInOrderBy(
            QueryBuilderSettings settings,
            List<QueryColumn> columns,
            CompositeNode node,
            Func<Type, string, QuerySource> tableCreator)
        {
            foreach (OrderedPropertyType order in settings.Orderings)
            {
                AggregateColumnReference acr = settings.Aggregates
                    .SingleOrDefault(p => p.Alias == order.Name && string.IsNullOrEmpty(order.Prefix) == true);
                if (acr != null)
                {
                    continue;
                }

                if (order.Name.Contains('(') == false)
                {
                    PropertyNameType pnt = new PropertyNameType();
                    pnt.Value = order.Name;
                    pnt.Prefix = order.Prefix;
                    AlignColumnsToPredicatable(pnt, columns, node, tableCreator);
                    order.Alias = pnt.Alias;
                    order.DbName = pnt.Value;
                }
                else
                {
                    FunctionType ft = new FunctionType() { Value = order.Name };
                    List<PropertyNameType> properties = ((IPredicatable)ft).LocatePropertyNames();
                    foreach (PropertyNameType pnt in properties)
                    {
                        AlignColumnsToPredicatable(pnt, columns, node, tableCreator);
                    }
                }
            }
        }

        /// <summary>
        /// Discover composite node declared in the groupby columns.
        /// </summary>
        /// <param name="groupby">The list of groupby columns.</param>
        /// <param name="node">The root node of the query.</param>
        /// <param name="tableCreator">The table creator function.</param>
        private static void AlignColumnsInGroupBy(
            List<GroupByReferenceType> groupby,
            CompositeNode node,
            Func<Type, string, QuerySource> tableCreator)
        {
            List<string> tests = new List<string>();
            foreach (GroupByReferenceType item in groupby)
            {
                tests.AddRange(item.Properties.Select(p => p.Name));
            }

            for (int i = 0; i < tests.Count; i++)
            {
                int paren = tests[i].IndexOf('(');
                if (paren > 0)
                {
                    FunctionType ft = new FunctionType() { Value = tests[i] };
                    tests.RemoveAt(i--);
                    List<PropertyNameType> properties = ((IPredicatable)ft).LocatePropertyNames();
                    foreach (PropertyNameType pnt in properties)
                    {
                        tests.Add(pnt.Serialize());
                    }
                }
            }

            foreach (string group in tests)
            {
                string prefix = string.Empty;
                string name = group;
                int pos = group.LastIndexOf('/');
                if (pos > 0)
                {
                    name = group.Substring(pos + 1);
                    prefix = group.Substring(0, pos);
                }

                CompositeNode child = node.Align(prefix);
                TypeCache.CheckIsLegalColumn(name, child.ElementType);
                tableCreator(child.ElementType, child.GetFullPath());
            }
        }

        /// <summary>
        /// Discover composite node declared in the Aggregate columns.
        /// </summary>
        /// <param name="aggregates">The list of aggregate columns.</param>
        /// <param name="node">The root node of the query.</param>
        /// <param name="tableCreator">The table creator function.</param>
        private static void AlignColumnsInAggregate(
            List<AggregateColumnReference> aggregates,
            CompositeNode node,
            Func<Type, string, QuerySource> tableCreator)
        {
            foreach (AggregateColumnReference acr in aggregates)
            {
                List<PropertyNameType> properties = acr.Predicatable.LocatePropertyNames();
                foreach (PropertyNameType pnt in properties)
                {
                    CompositeNode child = node.Align(pnt.Prefix ?? string.Empty);
                    TypeCache.CheckIsLegalColumn(pnt.Value, child.ElementType);
                    tableCreator(child.ElementType, child.GetFullPath());
                }
            }
        }

        /// <summary>
        /// Discover composite node declared in the select columns.
        /// </summary>
        /// <param name="selects">The list of select columns.</param>
        /// <param name="aggregates">The aggregates in the query.</param>
        /// <param name="node">The root node of the query.</param>
        /// <param name="tableCreator">The table creator function.</param>
        private static void AlignColumnsInSelect(
            List<string> selects,
            List<AggregateColumnReference> aggregates,
            CompositeNode node,
            Func<Type, string, QuerySource> tableCreator)
        {
            foreach (string select in selects)
            {
                AggregateColumnReference acr = aggregates.SingleOrDefault(p => p.Alias == select);
                if (acr != null)
                {
                    continue;
                }

                string name = select;
                string prefix = string.Empty;
                int pos = select.LastIndexOf('/');
                if (pos > 0)
                {
                    name = select.Substring(pos + 1);
                    prefix = select.Substring(0, pos);
                }

                CompositeNode child = node.Align(prefix);
                TypeCache.CheckIsLegalColumn(name, child.ElementType);
                tableCreator(child.ElementType, child.GetFullPath());
            }
        }

        /// <summary>
        /// Align the columns in the filter with those in the query.
        /// </summary>
        /// <param name="filter">The filter to inspect.</param>
        /// <param name="columns">The list of columns.</param>
        /// <param name="node">The composite node to work with.</param>
        /// <param name="tableCreator">The opertion to create discovered tables.</param>
        private static void AlignColumns(
            FilterType filter, 
            List<QueryColumn> columns, 
            CompositeNode node,
            Func<Type, string, QuerySource> tableCreator)
        {
            if (filter == null)
            {
                return;
            }

            Stack<ExpressionType> stack = new Stack<ExpressionType>();
            stack.Push(filter.Item);
            while (stack.Count > 0)
            {
                ExpressionType item = stack.Pop();
                ConditionType condition = item as ConditionType;
                PredicateType predicate = item as PredicateType;
                AnyOrAllType anyorall = item as AnyOrAllType;
                if (condition != null)
                {
                    foreach (ExpressionType et in condition.Items)
                    {
                        stack.Push(et);
                    }
                }
                else if (predicate != null)
                {
                    IPredicatable s = predicate.Subject as IPredicatable;
                    IPredicatable p = predicate.Predicate as IPredicatable;
                    AlignColumnsToPredicatable(s, columns, node, tableCreator);
                    AlignColumnsToPredicatable(p, columns, node, tableCreator);
                }
                else if (anyorall != null)
                {
                    AlignAnyOrAll(anyorall, node);
                    PropertyNameType pn = new PropertyNameType() { Value = anyorall.Name, Prefix = anyorall.Prefix };
                    int pos = anyorall.Name.LastIndexOf('/');
                    if (pos > 0)
                    {
                        pn.Value = anyorall.Name.Substring(pos + 1);
                        pn.Prefix = anyorall.Name.Substring(0, pos);
                    }

                    AlignColumnsToPredicatable(pn, columns, node, tableCreator);
                    anyorall.Prefix = pn.Prefix;
                    anyorall.Alias = pn.Alias;
                    anyorall.ElementType = pn.ElementType;
                }
            }
        }

        /// <summary>
        /// Align the any or all nodes.
        /// </summary>
        /// <param name="anyorall">The any or all instance.</param>
        /// <param name="rootNode">The root node to align against.</param>
        private static void AlignAnyOrAll(AnyOrAllType anyorall, CompositeNode rootNode)
        {
            StringBuilder builder = new StringBuilder();
            string separator = string.Empty;
            List<string> steps = new List<string>();
            if (string.IsNullOrEmpty(anyorall.Prefix) == false)
            {
                steps = anyorall.Prefix.Split('/').ToList();
            }

            steps.Add(anyorall.Name);
            foreach (string step in steps)
            {
                builder.Append(separator);
                builder.Append(step);
                bool subselect = step == steps.Last();
                rootNode.Align(builder.ToString(), subselect);
                separator = "/";
            }
        }

        /// <summary>
        /// Align the relevant columns to the given predicatable.
        /// </summary>
        /// <param name="predicatable">The predicatable to test.</param>
        /// <param name="columns">The columns to use.</param>
        /// <param name="node">The composite node to work with.</param>
        /// <param name="tableCreator">The opertion to create discovered tables.</param>
        private static void AlignColumnsToPredicatable(
            IPredicatable predicatable, 
            List<QueryColumn> columns,
            CompositeNode node,
            Func<Type, string, QuerySource> tableCreator)
        {
            if (predicatable == null)
            {
                return;
            }

            FunctionType function = predicatable as FunctionType;
            List<PropertyNameType> properties = new List<PropertyNameType>();
            properties.AddRange(predicatable.LocatePropertyNames());

            foreach (PropertyNameType propertyName in properties)
            {
                if (string.IsNullOrEmpty(propertyName.Alias) == false)
                {
                    continue;
                }

                CompositeNode target = node.Align(propertyName.Prefix ?? string.Empty);
                QueryColumn column = columns.SingleOrDefault(
                    p => p.Alias.Equals(propertyName.Value, StringComparison.OrdinalIgnoreCase) && 
                         p.Source.Path.Equals(propertyName.Prefix ?? string.Empty));

                CheckIsLegalColumn(propertyName, target.ElementType);
                if (column != null && function != null && function.Name == "isof")
                {
                    CompositeNode expand = node.Align(propertyName.Value);
                    QuerySource table = tableCreator(expand.ElementType, expand.GetFullPath());
                    propertyName.Alias = table.Alias;
                }
                else if (column != null && string.IsNullOrEmpty(propertyName.Alias) == true)
                {
                    propertyName.ElementType = target.ElementType;
                    propertyName.Alias = column.Source.Alias;
                    propertyName.Value = column.Name ?? column.Alias;
                }
                else if (target.Parent != null)
                {
                    QuerySource table = tableCreator(target.ElementType, target.GetFullPath());
                    List<QueryColumn> nested = TypeCache.CreateColumns(table, target.ElementType);
                    AlignColumnsToPredicatable(propertyName, nested, node, tableCreator);
                }
                else if (propertyName.Value == "$it")
                {
                    QuerySource table = tableCreator(node.ElementType, string.Empty);
                    propertyName.Alias = table.Alias;
                }
            }
        }

        /// <summary>
        /// Check if the property name contains a legal column reference.
        /// </summary>
        /// <param name="propertyName">The property name to check.</param>
        /// <param name="elementType">The type against which to check the property.</param>
        private static void CheckIsLegalColumn(PropertyNameType propertyName, Type elementType)
        {
            // We will check the aggregate column reference in the AlignColumnsInAggregate method.
            // We can skip the check here since we don't yet have enough context and will fail
            // since the declared alias is not a property directly on the element type.
            if (propertyName.AggregateColumnReference == null)
            {
                TypeCache.CheckIsLegalColumn(propertyName.Value, elementType);
            }
        }

        /// <summary>
        /// Fix up the argument filter.
        /// </summary>
        /// <param name="argumentFilter">The argument filter to fix.</param>
        /// <param name="rootNode">The root node.</param>
        /// <returns>Fixed filter.</returns>
        private static FilterType FixUpArgumentFilter(FilterType argumentFilter, CompositeNode rootNode)
        {
            FilterType filter = null;
            if (argumentFilter != null)
            {
                filter = argumentFilter.DeepCopy();
                List<PropertyNameType> all = new List<PropertyNameType>();
                List<EqualType> removals = new List<EqualType>();

                AndType and = filter.Item as AndType;
                if (and == null)
                {
                    return argumentFilter;
                }

                List<string> prefixes = and.Items
                    .Cast<EqualType>()
                    .Where(p => p.Subject is PropertyNameType)
                    .Select(p => ((PropertyNameType)p.Subject).Prefix)
                    .Distinct()
                    .ToList();

                if (prefixes.Count > 1)
                {
                    return filter;
                }

                foreach (string prefix in prefixes)
                {
                    bool remove = false;
                    List<PropertyNameType> properties = new List<PropertyNameType>();
                    List<EqualType> items = and.Items
                        .Cast<EqualType>()
                        .Where(p => p.Subject is PropertyNameType)
                        .Where(p => ((PropertyNameType)p.Subject).Prefix.Equals(prefix) == true)
                        .ToList();

                    CompositeNode test = AlignArgumentFilter(rootNode, prefix, items, ref removals, ref properties, ref all);
                    if (prefix == prefixes.Last() && string.IsNullOrEmpty(prefix) == false)
                    {
                        remove = FlagUnnecessaryArgumentFilters(test, properties, all, items, ref removals);
                    }

                    if (remove == true)
                    {
                        test.Parent.Nodes.Remove(test);
                    }
                }

                foreach (EqualType et in removals)
                {
                    and.Items.Remove(et);
                }

                if (and.Items.Count == 1)
                {
                    filter.Item = and.Items[0];
                }
            }

            return filter;
        }

        /// <summary>
        /// Align the properties in the argument filter.
        /// </summary>
        /// <param name="rootNode">The root node of the query.</param>
        /// <param name="prefix">The prefix to align.</param>
        /// <param name="items">The list of EqualType predicates in scoped to the same prefix.</param>
        /// <param name="removals">The list of EqualType predicates to be removed.</param>
        /// <param name="properties">The list of properties associated with the current prefix.</param>
        /// <param name="all">The complete lis of all properties in the argument filter.</param>
        /// <returns>The node against which the prefixed properties have been aligned.</returns>
        private static CompositeNode AlignArgumentFilter(
            CompositeNode rootNode, 
            string prefix, 
            List<EqualType> items,
            ref List<EqualType> removals,
            ref List<PropertyNameType> properties,
            ref List<PropertyNameType> all)
        {
            Type nodeType = ((PropertyNameType)items.First().Subject).ElementType;
            CompositeNode test = rootNode.Align(nodeType, prefix, false);
            foreach (EqualType et in items)
            {
                PropertyNameType pnt = et.Subject as PropertyNameType;
                pnt.Prefix = test.GetFullPath();
                if (et.Predicate is NullType)
                {
                    removals.Add(et);
                }
                else
                {
                    properties.Add(pnt);
                    all.Add(pnt);
                }
            }

            return test;
        }

        /// <summary>
        /// Evaluate the current list of properties to determine if a new join
        /// is actually required to use those properties in the query. If a new
        /// join is not required, return "true". The method will either fix the
        /// properties so that they make use of the existing join that contains 
        /// the columns or it will indicate that the property is not required
        /// at all in the query because some other filter has been included
        /// which already contains the necessary condition.
        /// </summary>
        /// <param name="test">The composite node (join) to evaluate.</param>
        /// <param name="properties">The list of properties defining the necessary key.</param>
        /// <param name="all">All properties that have been added thus far.</param>
        /// <param name="items">The list of all of equal predicates in the Argument Filter.</param>
        /// <param name="removals">The collection of predicates flagged for removal.</param>
        /// <returns>True if the test node can be removed, otherwise false</returns>
        private static bool FlagUnnecessaryArgumentFilters(
            CompositeNode test, 
            List<PropertyNameType> properties, 
            List<PropertyNameType> all,
            List<EqualType> items,
            ref List<EqualType> removals)
        {
            bool remove = false;
            QueryTable im;
            Dictionary<string, string> stmts = TypeCache.LocateJoin(test.Parent, test, out im);
            if (stmts.Count == properties.Count)
            {
                foreach (string st in stmts.Keys)
                {
                    PropertyNameType pnt = properties.SingleOrDefault(p => p.Value.Equals(stmts[st]) == true);
                    if (pnt != null)
                    {
                        remove = true;
                        if (all.Any(p => p.Prefix.Equals(test.Parent.Path) == true &&
                                         p.ElementType == test.Parent.ElementType &&
                                         p.Value.Equals(st) == true))
                        {
                            removals.Add(items.Single(p => p.Subject == pnt));
                        }
                        else
                        {
                            pnt.Value = st;
                            pnt.Prefix = test.Parent.Path;
                            pnt.ElementType = test.Parent.ElementType;
                        }
                    }
                }
            }

            return remove;
        }

        /// <summary>
        /// Find a column based on an odata full name.
        /// </summary>
        /// <param name="fullName">The full name to use.</param>
        /// <param name="columns">The initial list of columns to check.</param>
        /// <param name="query">The query in scope.</param>
        /// <returns>The discovered column.</returns>
        private static QueryColumn FindColumnInQuery(
            string fullName,
            List<QueryColumn> columns,
            SelectQuery query)
        {
            IEnumerable<QueryColumn> columnsToCheck = columns;
            Type type = typeof(T);
            string name = fullName;
            int pos = fullName.LastIndexOf('/');
            if (pos > 0)
            {
                name = fullName.Substring(pos + 1);
                string prefix = fullName.Substring(0, pos);
                CompositeNode node = query.RootNode.Align(prefix);
                type = node.ElementType;
                QueryJoin groupJoin = query.Joins.First(p => p.TargetNode == node);
                columnsToCheck = TypeCache.CreateColumns(groupJoin.Target, type);
            }

            QueryColumn column = columnsToCheck.SingleOrDefault(p => p.Alias == name);
            return column;
        }

        /// <summary>
        /// Find the query column that matches a provided aggregate.
        /// </summary>
        /// <param name="acr">The aggregate column reference.</param>
        /// <param name="query">The containing query.</param>
        /// <param name="columns">The original list of columns to check.</param>
        /// <param name="fullName">The fullname of the property.</param>
        /// <returns>A deep copy of the correct column to use.</returns>
        private static QueryColumn FindColumnForAggregate(
            AggregateColumnReference acr,
            SelectQuery query,
            List<QueryColumn> columns,
            out string fullName)
        {
            List<QueryColumn> list = new List<QueryColumn>();
            List<PropertyNameType> properties = acr.Predicatable.LocatePropertyNames();
            foreach (PropertyNameType pnt in properties)
            {
                Type type = typeof(T);
                QueryColumn column = columns
                    .SingleOrDefault(p => p.Alias.Equals(pnt.Value, StringComparison.OrdinalIgnoreCase) == true &&
                                          p.Source.Path.Equals(pnt.Prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true);

                // look for column in joined tables.
                if (column == null && pnt.Value != "*")
                {
                    CompositeNode node = query.RootNode.Align(pnt.Prefix ?? string.Empty);
                    type = node.ElementType;
                    QuerySource target = query.Joins.First(p => p.TargetNode == node).Target;
                    List<QueryColumn> joined = TypeCache.CreateColumns(target, node.ElementType);
                    column = joined
                        .SingleOrDefault(p => p.Alias.Equals(pnt.Value, StringComparison.OrdinalIgnoreCase) == true);
                }

                if (column != null)
                {
                    list.Add(column);
                }
            }

            QueryColumn copy = new QueryColumn();
            copy.Alias = acr.Alias;
            copy.NestedColumns.AddRange(list);
            copy.AggregateColumnReference = acr;
            copy.AggregateColumnReference.Column = copy;
            fullName = acr.Alias;
            if (properties.Count == 1)
            {
                fullName = properties[0].Serialize();
                if (string.IsNullOrEmpty(acr.Alias) == true)
                {
                    copy.Alias = properties[0].Value;
                }
            }

            return copy;
        }

        /// <summary>
        /// Add columns to the query.
        /// </summary>
        /// <param name="query">The query to use.</param>
        /// <param name="columns">The list of columns.</param>
        /// <param name="selects">The selects collection.</param>
        /// <param name="isAggregateQuery">True if add columns to aggregate query, otherwise false.</param>
        private static void AddColumns(
            SelectQuery query, 
            List<QueryColumn> columns,
            List<string> selects,
            bool isAggregateQuery)
        {
            foreach (QueryColumn column in columns)
            {
                if (TypeCache.IsNavigationalType(column.ElementType) == true)
                {
                    continue;
                }

                if (query.AllColumns.Any(p => p.Alias == column.Alias) == false)
                {
                    query.AllColumns.Add(column);
                    if (column.Alias == "$TypeName" && isAggregateQuery == false)
                    {
                        query.Columns.Add(column);
                    }
                }

                if (query.Columns.Any(p => p.Alias.Equals(column.Alias, StringComparison.OrdinalIgnoreCase) == true && (column.Source == null || p.Source == null || column.Source.Path == p.Source.Path)) == false &&
                    (selects.Count == 0 || selects.Contains(column.Alias) == true))
                {
                    if (column.IsKeyColumn && isAggregateQuery == true && column.AggregateColumnReference == null && query.Joins.Count > 0)
                    {
                        column.AggregateColumnReference = new AggregateColumnReference(column.Alias, AggregateType.Max, column.Alias);
                        column.NestedColumns.Add(column);
                        query.Columns.Add(column);
                    }
                    else if (isAggregateQuery == false)
                    {
                        query.Columns.Add(column);
                    }
                }
            }
        }

        /// <summary>
        /// Add aggregates to the query, if applicable.
        /// </summary>
        /// <param name="query">The query to use.</param>
        /// <param name="columns">The list of columns.</param>
        /// <param name="settings">The builder settings.</param>
        private static void AddAggregates(
            SelectQuery query, 
            List<QueryColumn> columns, 
            QueryBuilderSettings settings)
        {
            if (settings.Aggregates.Count > 0)
            {
                foreach (AggregateColumnReference acr in settings.Aggregates)
                {
                    string fullName;
                    QueryColumn copy = FindColumnForAggregate(acr, query, columns, out fullName);
                    if (query.AllColumns.Any(p => p.Alias == copy.Alias) == false)
                    {
                        query.AllColumns.Add(copy);
                    }

                    if (settings.Selects.Count == 0 || settings.Selects.Contains(fullName) || settings.Selects.Contains(acr.Alias))
                    {
                        query.Columns.Add(copy);
                    }
                }
            }
        }

        /// <summary>
        /// Add the groupby statements to the query.
        /// </summary>
        /// <param name="query">The query to supplement.</param>
        /// <param name="columns">The list of columns for the root type.</param>
        /// <param name="settings">The settings to use.</param>
        private static void AddGroupBy(SelectQuery query, List<QueryColumn> columns, QueryBuilderSettings settings)
        {
            List<string> aliasedGroupings = new List<string>();
            IEnumerable<IPredicatable> set = settings.Aggregates.Where(p => p.AggregateType == AggregateType.None).Select(p => p.Predicatable);
            foreach (IPredicatable item in set)
            {
                aliasedGroupings.Add(item.Serialize());
            }

            foreach (GroupByReferenceType item in settings.Groupings)
            {
                List<QueryColumn> groupedColumns = new List<QueryColumn>();
                foreach (PropertyType pt in item.Properties)
                {
                    QueryColumn column = FindColumnInQuery(pt.Name, columns, query);
                    if (column == null)
                    {
                        string fullName;
                        AggregateColumnReference acr = settings.Aggregates.Where(p => p.Predicatable.Serialize() == pt.Name).SingleOrDefault();
                        if (acr == null)
                        {
                            FunctionType ft = new FunctionType() { Value = pt.Name };
                            acr = new AggregateColumnReference(ft, AggregateType.None);
                        }

                        column = FindColumnForAggregate(acr, query, columns, out fullName);
                    }

                    groupedColumns.Add(column);
                    if (aliasedGroupings.Contains(pt.Name) == false)
                    {
                        if (query.AllColumns.Contains(column) == false)
                        {
                            query.AllColumns.Add(column);
                        }

                        if ((settings.Selects.Count == 0 || 
                             settings.Selects.Contains(pt.Name) == true || 
                             settings.Selects.Contains(column.Source.Path)) &&
                            query.Columns.Contains(column) == false)
                        {
                            query.Columns.Add(column);
                        }
                    }
                }

                QueryGroupBy gb = new QueryGroupBy(item.GroupingType, groupedColumns.ToArray());
                query.GroupBy.Add(gb);
            }
        }

        /// <summary>
        /// Add orderby to the query, if applicable.
        /// </summary>
        /// <param name="query">The query to use.</param>
        /// <param name="columns">The list of columns.</param>
        /// <param name="orderby">The list of orderbys.</param>
        private static void AddOrderBy(
            SelectQuery query, 
            List<QueryColumn> columns,
            List<OrderedPropertyType> orderby)
        {
            foreach (OrderedPropertyType order in orderby)
            {
                string name = order.DbName ?? order.Name;
                QueryOrder qo = new QueryOrder();
                qo.IsAscending = order.Ascending;
                qo.Column = columns.SingleOrDefault(p => p.Name == name && order.Prefix == p.Source.Path);
                if (qo.Column == null)
                {
                    qo.Column = new QueryColumn();
                    qo.Column.Name = name;
                    QueryJoin join = query.Joins.FirstOrDefault(p => p.Source.Alias == order.Alias || p.Target.Alias == order.Alias);
                    if (join == null)
                    {
                        // column is not attached to any source.
                        qo.Column.Source = new QueryTable();
                    }
                    else if (join.Source.Alias == order.Alias)
                    {
                        qo.Column.Source = join.Source;
                    }
                    else
                    {
                        qo.Column.Source = join.Target;
                    }
                }

                if (order.Name.Contains('(') == true)
                {
                    string fullName;
                    FunctionType ft = new FunctionType() { Value = order.Name };
                    AggregateColumnReference acr = new AggregateColumnReference(ft, AggregateType.None);
                    qo.Column = FindColumnForAggregate(acr, query, columns, out fullName);
                }
                else if (query.GroupBy.Count > 0 && query.GroupBy.SelectMany(p => p.NestedColumns).Any(
                    p => p.Name == qo.Column.Name && p.Source.Alias == qo.Column.Source.Alias) == false &&
                    query.Columns.Any(p => p.Alias == (qo.Column.Alias ?? qo.Column.Name)) == true)
                {
                    QueryColumn copy = new QueryColumn();
                    copy.AggregateColumnReference = qo.Column.AggregateColumnReference;
                    copy.Alias = qo.Column.Alias;
                    copy.DefaultValue = qo.Column.DefaultValue;
                    copy.ElementType = qo.Column.ElementType;
                    copy.Expression = qo.Column.Expression;
                    copy.IsKeyColumn = qo.Column.IsKeyColumn;
                    copy.Name = qo.Column.Name;
                    copy.NestedColumns.AddRange(qo.Column.NestedColumns);
                    copy.Source = new QueryTable();
                    qo.Column = copy;
                }

                query.OrderBy.Add(qo);
            }
        }

        /// <summary>
        /// Create an embedded join/filter for expressing top and skip.
        /// Skip is basically a left self join where null using the skip value as a count. 
        /// This will cause the left query in the join to skip those many rows.
        /// Top, in the projection case, is an inner self join with the top count set on the joined table. 
        /// No null check or condition is required.
        /// </summary>
        /// <typeparam name="RootType">The type of the root node.</typeparam>
        /// <param name="settings">The original settings data.</param>
        /// <param name="query">The query being constructed.</param>
        /// <param name="top">True if this is a top query, otherwise false.</param>
        /// <param name="executor">The executor to use.</param>
        /// <returns>The created query.</returns>
        private static SelectQuery CreateSkipOrTop<RootType>(
            QueryBuilderSettings settings, 
            SelectQuery query, 
            bool top,
            IExecutor executor)
        {
            QueryBuilderSettings skipsettings = new QueryBuilderSettings();
            if (top == true && settings.Top != null)
            {
                skipsettings.Top = settings.Top.Value;
            }
            else if (top == false)
            {
                skipsettings.Top = settings.Skip.Value;
            }

            skipsettings.Skip = top == true && settings.Skip != null ? settings.Skip.Value : (long?)null;
            skipsettings.Template = top == true ? "Top" : "Skip";
            skipsettings.Where = settings.Where;
            skipsettings.Filter = settings.Filter;
            skipsettings.ArgumentFilter = settings.ArgumentFilter;
            skipsettings.Orderings.AddRange(settings.Orderings);
            skipsettings.Groupings.AddRange(settings.Groupings);
            skipsettings.Aggregates.AddRange(settings.Aggregates);
            skipsettings.Url = settings.Url;
            skipsettings.EntitySetType = settings.EntitySetType;
            skipsettings.DefaultJoinType = settings.DefaultJoinType;
            skipsettings.Executor = settings.Executor;
            QueryBuilder<RootType> skipbuilder = new QueryBuilder<RootType>(skipsettings);
            foreach (QueryJoin join in skipbuilder.Query.Joins)
            {
                join.JoinType = QueryJoinType.Left;
            }

            QueryJoin skipjoin = new QueryJoin();
            query.Joins.Add(skipjoin);
            skipjoin.Source = query.Source;
            skipjoin.Target = skipbuilder.Query;
            skipjoin.Target.Alias = skipsettings.Template;
            if (query.Distinct == true)
            {
                skipbuilder.Query.Distinct = true;
            }

            ConfigureSkipJoin(skipjoin, skipbuilder.Query, query, executor);

            if (top == false)
            {
                skipjoin.JoinType = QueryJoinType.Left;

                FilterType skipfilter = new FilterType();
                EqualType eq = new EqualType();
                eq.Predicate = new NullType();
                eq.Subject = new PropertyNameType() { Alias = skipsettings.Template, Value = skipjoin.Statements.First().Item2.Name };
                skipfilter.Item = eq;

                query.Filter = FilterType.Merge(query.Filter, skipfilter);
            }

            return skipbuilder.Query;
        }

        /// <summary>
        /// Configure the skip join based on the query type.
        /// </summary>
        /// <param name="skipjoin">The join to configure.</param>
        /// <param name="skipQuery">The inner skip query.</param>
        /// <param name="query">The outer top query.</param>
        /// <param name="executor">The executor to use.</param>
        private static void ConfigureSkipJoin(
            QueryJoin skipjoin,
            SelectQuery skipQuery,
            SelectQuery query,
            IExecutor executor)
        {
            if (skipQuery.GroupBy.Count == 0 &&
                skipQuery.Columns.Any(p => p.AggregateColumnReference != null && p.AggregateColumnReference.AggregateType != AggregateType.None) == false)
            {
                List<QueryColumn> keys = skipQuery.Columns.Where(p => p.IsKeyColumn == true).ToList();
                skipjoin.SetStatements(query.RootNode, query.RootNode);
                skipQuery.Columns.Clear();
                skipQuery.Columns.AddRange(keys);
            }
            else
            {
                foreach (QueryColumn tqc in skipQuery.Columns)
                {
                    if (tqc.AggregateColumnReference != null &&
                        tqc.AggregateColumnReference.AggregateType != AggregateType.None)
                    {
                        continue;
                    }
                    else if (tqc.IsKeyColumn == true && tqc.DefaultValue != null && tqc.DefaultValue.ToString() == tqc.Expression)
                    {
                        continue;
                    }

                    QueryColumn sqc = query.AllColumns.Single(p => p.Alias.Equals(tqc.Alias) == true);
                    QueryColumn copy = new QueryColumn();
                    copy.Source = skipQuery;
                    copy.Alias = tqc.Alias;
                    copy.Name = tqc.Alias;
                    skipjoin.Statements.Add(new Tuple<QueryColumn, QueryColumn>(sqc, copy));
                }
            }

            executor.ProjectionHelper.FixUpOrderBy(skipQuery);
        }

        /// <summary>
        /// Make the joins for the given query, based on the provided structure.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <param name="query">The query to add the joins to.</param>
        /// <param name="tables">The set of tables.</param>
        /// <param name="tableCreator">The opertion to create discovered tables.</param>
        private static void MakeJoins(
            QueryBuilderSettings settings,
            SelectQuery query,
            Dictionary<string, QuerySource> tables,
            Func<Type, string, QuerySource> tableCreator)
        {
            Queue<CompositeNode> queue = new Queue<CompositeNode>();
            queue.Enqueue(query.RootNode);
            while (queue.Count > 0)
            {
                CompositeNode node = queue.Dequeue();
                foreach (CompositeNode child in node.Nodes)
                {
                    if (child.IsSubSelect == false)
                    {
                        queue.Enqueue(child);
                    }
                }

                if (node.Parent != null)
                {
                    QuerySource source, target;
                    string sourcePath = node.Parent.GetFullPath();
                    string targetPath = node.GetFullPath();
                    if (tables.TryGetValue(sourcePath, out source) == false)
                    {
                        source = tableCreator(node.Parent.ElementType, node.Parent.Path);
                    }

                    if (tables.TryGetValue(targetPath, out target) == false)
                    {
                        target = tableCreator(node.ElementType, node.Path);
                    }

                    QueryJoin join = new QueryJoin() { Source = source, Target = target };
                    join.SetStatements(node.Parent, node);
                    join.JoinType = settings.DefaultJoinType;
                    query.Joins.Add(join);
                }
            }
        }

        /// <summary>
        /// Project the query over any expands it may have.
        /// </summary>
        /// <typeparam name="RootType">The type of the root entity.</typeparam>
        /// <param name="query">The root query.</param>
        /// <param name="settings">The original query settings.</param>
        /// <param name="executor">The executor to use.</param>
        private static void Project<RootType>(SelectQuery query, QueryBuilderSettings settings, IExecutor executor)
        {
            if (settings.QueryContext != null && settings.QueryContext.IsEmpty == false && settings.IsAggregateQuery == false)
            {
                List<QueryColumn> projectionColumns = new List<QueryColumn>();
                IEnumerable<QueryColumn> projection = executor.ProjectionHelper.LocateColumns(query, query.RootNode);
                projectionColumns.AddRange(projection);
                QueryColumn[] seedKeys = new QueryColumn[projection.Count() - 1];
                Array.Copy(projection.ToArray(), 1, seedKeys, 0, projection.Count() - 1);
                Visit(query.RootNode, settings.QueryContext, query, projectionColumns, seedKeys, executor);
                query.PathQuery = executor.ProjectionHelper.CreatePathQuery(projectionColumns);
                query.PathSubSelect = executor.ProjectionHelper.AlignColumnsToPath(projection, seedKeys);

                SelectQuery insertQuery = new SelectQuery();
                insertQuery.RootNode = query.RootNode;
                insertQuery.Filter = query.Filter;
                insertQuery.Source = query.Source;
                insertQuery.Columns.AddRange(projectionColumns);
                insertQuery.OrderBy.AddRange(query.OrderBy);
                foreach (QueryJoin join in query.Joins)
                {
                    QueryJoin copy = new QueryJoin(join);
                    copy.JoinType = QueryJoinType.Left;
                    insertQuery.Joins.Add(copy);
                }

                if (query.Top != null || (query.Skip != null && executor.UseJoinForSkip == false))
                {
                    CreateSkipOrTop<RootType>(settings, insertQuery, true, executor);
                }
                else if (query.Skip != null && executor.UseJoinForSkip == true && query.Top == null)
                {
                    CreateSkipOrTop<RootType>(settings, insertQuery, false, executor);
                }

                query.Joins.Clear();
                query.OrderBy.Clear();
                query.Filter = null;
                query.Top = null;
                query.Skip = null;
                executor.ProjectionHelper.ReConfigureJoins(query);
                executor.ProjectionHelper.FixSubSelects(query);
                query.InsertQuery = insertQuery;
                AppendExpandedOrderBys(query);
            }
            else if (query.Skip != null && executor.UseJoinForSkip == true)
            {
                CreateSkipOrTop<RootType>(settings, query, false, executor);
            }
        }

        /// <summary>
        /// Append Projected orderings onto insert query.
        /// </summary>
        /// <param name="query">The insert query from the projection.</param>
        private static void AppendExpandedOrderBys(SelectQuery query)
        {
            List<OrderedPropertyType> orderings = new List<OrderedPropertyType>();
            Queue<CompositeNode> queue = new Queue<CompositeNode>();
            queue.Enqueue(query.RootNode);
            while (queue.Count > 0)
            {
                CompositeNode node = queue.Dequeue();
                foreach (string orderby in node.OrderBys ?? new List<string>())
                {
                    bool descending = false;
                    string[] parts = orderby.Split(' ');
                    if (parts.Length == 2 && parts[1].ToLower() == "desc")
                    {
                        descending = true;
                    }

                    string fullPath = node.GetFullPath();
                    OrderedPropertyType opt = new OrderedPropertyType();
                    opt.Name = parts[0];
                    opt.Prefix = fullPath;
                    opt.Ascending = !descending;
                    QuerySource table = query.InsertQuery.Joins.Where(p => p.TargetNode == node).Select(p => p.Target).FirstOrDefault();
                    if (table == null)
                    {
                        table = query.InsertQuery.Joins.Where(p => p.SourceNode == node).Select(p => p.Source).FirstOrDefault();
                    }

                    opt.Alias = table.Alias;
                    orderings.Add(opt);
                }

                foreach (CompositeNode child in node.Nodes)
                {
                    queue.Enqueue(child);
                }
            }

            AddOrderBy(query.InsertQuery, query.InsertQuery.AllColumns, orderings);
        }

        /// <summary>
        /// Visit a particular node in the composite structure.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="context">The current context.</param>
        /// <param name="query">The root query.</param>
        /// <param name="projectionColumns">The list of projection columns to populate.</param>
        /// <param name="seedKeys">The key columns from the seed type.</param>
        /// <param name="executor">The executor to use.</param>
        private static void Visit(
            CompositeNode node, 
            QueryContext context,
            SelectQuery query,
            List<QueryColumn> projectionColumns,
            IEnumerable<QueryColumn> seedKeys,
            IExecutor executor)
        {
            if (node.Parent != null)
            {
                // Create a dummy query to use for producing path sub select.
                QuerySource source = executor.ProjectionHelper.LocateSource(query, node);
                SelectQuery secondary = new SelectQuery();
                secondary.RootNode = node;
                secondary.Source = source;
                List<QueryColumn> columns = TypeCache.CreateColumns(source, node.ElementType);
                AddColumns(secondary, columns, new List<string>(), false);

                // Get columns from dummy query and add them to the projected columns.
                IEnumerable<QueryColumn> projection = executor.ProjectionHelper.LocateColumns(secondary, node);
                projectionColumns.AddRange(projection);

                // Generate the real query, creating the correct subselect afterward.
                QueryBuilderSettings settings = CreateSecondarySettings(context, executor);
                IQueryBuilder builder = TypeCache.ReflectCorrectBuilder(node.ElementType, settings);
                query.Secondaries.Add(builder.Query);
                builder.Query.RootNode = node;
                builder.Query.PathSubSelect = executor.ProjectionHelper.AlignColumnsToPath(projection, seedKeys);
            }

            foreach (CompositeNode child in node.Nodes)
            {
                if (context.Expand(child.Path) == true)
                {
                    context.Navigate(child.Path);
                    Visit(child, context, query, projectionColumns, seedKeys, executor);
                    context.ResetToParent();
                }
            }
        }

        /// <summary>
        /// Create the query builder settings for the secondary query in a projection.
        /// </summary>
        /// <param name="context">The context positioned for the relevant secondary query.</param>
        /// <param name="executor">The executor to use.</param>
        /// <returns>The query builder settings.</returns>
        private static QueryBuilderSettings CreateSecondarySettings(QueryContext context, IExecutor executor)
        {
            string select = context.ReadSelects();
            string filter = context.ReadFilter();
            string pseudoUrl = string.Format(
                "http://server/collection?{0}&{1}",
                string.IsNullOrEmpty(filter) ? "@p1=0" : "$filter=" + filter,
                string.IsNullOrEmpty(select) ? "@p2=0" : "$select=" + select);
            QueryBuilderSettings secondarySettings = DataFilterParsingHelper.ExtractToSettings(new Uri(pseudoUrl), null);
            secondarySettings.Executor = executor;

            return secondarySettings;
        }

        /// <summary>
        /// Configure any abstract group bys in the current query.
        /// </summary>
        /// <param name="local">The local deep copy of parsed settings.</param>
        private static void ConfigureAbstractGroupBy(QueryBuilderSettings local)
        {
            if (local.IsAggregateQuery == true)
            {
                List<GroupByReferenceType> added = new List<GroupByReferenceType>();
                IEnumerable<string> pts = local.Groupings.SelectMany(p => p.Properties).Select(p => p.Name).Distinct();
                foreach (string pt in pts)
                {
                    int pos = pt.LastIndexOf('/');
                    if (pos > 0)
                    {
                        string pn = pt.Substring(0, pos);
                        Type propertyType = TypeCache.LocatePropertyType(typeof(T), pn);
                        propertyType = TypeCache.NormalizeType(propertyType);
                        if (TypeCache.ReadDerivedTypes(propertyType).Any() == true)
                        {
                            GroupByReferenceType gbrt = new GroupByReferenceType();
                            PropertyType gb = new PropertyType() { Name = pn + "/$TypeName" };
                            gbrt.Properties.Add(gb);
                            added.Add(gbrt);
                        }
                    }
                }

                local.Groupings.AddRange(added);
            }
        }

        /// <summary>
        /// Build the source for a given type as a subselect query.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>The created sub select.</returns>
        private static SelectQuery BuildSubSelect(Type type)
        {
            string schema, name;
            int innerIndex = 0;
            SelectQuery innerQuery = new SelectQuery();
            innerQuery.Type = type;
            Type baseType = type;
            while (baseType != null)
            {
                TypeCache.ExtractTableName(baseType, out name, out schema);
                if (string.IsNullOrEmpty(name) == false)
                {
                    QueryTable inner = new QueryTable();
                    inner.Name = name;
                    inner.Schema = schema;
                    inner.Alias = string.Concat("Inner", innerIndex++);
                    inner.Type = baseType;
                    List<QueryColumn> columns = TypeCache.CreateColumns(inner, baseType)
                        .Where(p => p.DeclaringType == baseType || p.Alias == "$TypeName").ToList();
                    if (columns.Count > 0)
                    {
                        AddColumns(innerQuery, columns, new List<string>(), false);
                    }

                    if (innerQuery.Source == null)
                    {
                        innerQuery.Source = inner;
                    }
                    else
                    {
                        QueryJoin join = new QueryJoin();
                        join.Source = innerQuery.Source;
                        join.Target = inner;
                        List<string> keyNames = TypeCache.GetKeys(type);
                        foreach (string keyName in keyNames)
                        {
                            QueryColumn scol = new QueryColumn() { Name = keyName, Source = join.Source };
                            QueryColumn tcol = new QueryColumn() { Name = keyName, Source = join.Target };
                            join.Statements.Add(new Tuple<QueryColumn, QueryColumn>(scol, tcol));
                        }

                        innerQuery.Joins.Add(join);
                    }
                }

                baseType = baseType.BaseType;
            }

            return innerQuery;
        }

        /// <summary>
        /// Build the source for a given abstract type as a union query.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>The created union.</returns>
        private static UnionQuery BuildAbstractSubSelect(Type type)
        {
            IEnumerable<Type> types = TypeCache.ReadDerivedTypes(type);
            UnionQuery union = new UnionQuery();
            union.Type = type;
            foreach (Type t in types)
            {
                if (t.IsAbstract == false)
                {
                    SelectQuery typeQuery = BuildSubSelect(t);
                    union.AddQuery(typeQuery, true);
                }
            }

            union.Align();

            return union;
        }

        /// <summary>
        /// Test the ordering construction and add key based order if top/skip is present without orderby.
        /// </summary>
        /// <param name="settings">The settings to modify.</param>
        private static void TestOrderRules(QueryBuilderSettings settings)
        {
            if (settings.Orderings.Count > 0)
            {
                return;
            }

            if (settings.Top.HasValue == true || settings.Skip.HasValue == true)
            {
                List<string> keys = TypeCache.GetKeys(typeof(T));
                foreach (string key in keys)
                {
                    OrderedPropertyType order = new OrderedPropertyType();
                    order.Name = key;
                    order.Prefix = string.Empty;
                    order.Ascending = true;
                    settings.Orderings.Add(order);
                }
            }
        }

        /// <summary>
        /// Creates an adoption metric based query.
        /// </summary>
        /// <param name="settings">The query configuration.</param>
        /// <returns>The select query to use.</returns>
        private SelectQuery CreateQuery(QueryBuilderSettings settings)
        {
            SelectQuery query = new SelectQuery();
            query.RootNode = new CompositeNode(null, string.Empty, typeof(T), false);
            QuerySource table = this.CreateTable(typeof(T), query.RootNode.GetFullPath());
            query.Source = table;
            List<QueryColumn> columns = TypeCache.CreateColumns(table, typeof(T));
            if (settings.QueryContext != null && settings.QueryContext.IsEmpty == false)
            {
                settings.QueryContext.PopulateCompositeNodes(query.RootNode, this.CreateTable);
            }

            TestOrderRules(settings);
            FilterType fixedUp = FixUpArgumentFilter(settings.ArgumentFilter, query.RootNode);
            FilterType original = FilterType.Merge(settings.Filter, settings.Where, fixedUp);
            if (original != null)
            {
                query.Filter = original.DeepCopy();
            }

            if (settings.Top.HasValue == true)
            {
                query.Top = new QueryTop() { Value = settings.Top.Value };
            }

            if (settings.Skip.HasValue == true)
            {
                query.Skip = new QueryTop() { Value = settings.Skip.Value };
            }

            AlignColumns(query.Filter, columns, query.RootNode, this.CreateTable);
            AlignColumnsInOrderBy(settings, columns, query.RootNode, this.CreateTable);
            AlignColumnsInGroupBy(settings.Groupings, query.RootNode, this.CreateTable);
            AlignColumnsInAggregate(settings.Aggregates, query.RootNode, this.CreateTable);
            AlignColumnsInSelect(settings.Selects, settings.Aggregates, query.RootNode, this.CreateTable);
            MakeJoins(settings, query, this.tables, this.CreateTable);

            AddGroupBy(query, columns, settings);
            AddAggregates(query, columns, settings);
            AddColumns(query, columns, settings.Selects, settings.IsAggregateQuery);
            AddOrderBy(query, columns, settings.Orderings);
            Project<T>(query, settings, this.Settings.Executor);

            if (settings.QueryProcessor != null)
            {
                query = settings.QueryProcessor(query);
            }

            return query;
        }

        /// <summary>
        /// Create the table for the given type.
        /// </summary>
        /// <param name="type">The underlying type.</param>
        /// <param name="fullPath">The full path to the table.</param>
        /// <returns>The resulting table.</returns>
        private QuerySource CreateTable(Type type, string fullPath)
        {
            if (this.tables.ContainsKey(fullPath) == true)
            {
                return this.tables[fullPath];
            }

            if (TypeCache.ReadDerivedTypes(type).Any() == true)
            {
                UnionQuery abs = BuildAbstractSubSelect(type);
                abs.Alias = this.GetAlias();
                abs.Path = fullPath;
                this.tables[fullPath] = abs;
                return abs;
            }
            else
            {
                SelectQuery sub = BuildSubSelect(type);
                if (sub.Joins.Count > 0)
                {
                    sub.Alias = this.GetAlias();
                    sub.Path = fullPath;
                    this.tables[fullPath] = sub;
                    return sub;
                }
            }

            string schema, name;
            TypeCache.ExtractTableName(type, out name, out schema);

            QueryTable table = new QueryTable();
            table.Name = name;
            table.Schema = schema;
            table.Alias = this.GetAlias();
            table.Path = fullPath;
            table.Type = type;
            this.tables[fullPath] = table;

            return table;
        }

        /// <summary>
        /// Generate an alias to use.
        /// </summary>
        /// <returns>The alias to use.</returns>
        private string GetAlias()
        {
            return string.Concat(this.template, this.counter++);
        }
    }
}
