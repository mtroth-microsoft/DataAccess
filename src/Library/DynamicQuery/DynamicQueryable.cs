// -----------------------------------------------------------------------
// <copyright file="DynamicQueryable.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using oem = OdataExpressionModel;

    /// <summary>
    /// The dynamic queryable class.
    /// </summary>
    public class DynamicQueryable<T> :
        IQueryable<T>,
        IQueryable,
        IEnumerable<T>,
        IEnumerable,
        IOrderedQueryable<T>,
        IOrderedQueryable
    {
        /// <summary>
        /// The parsed settings for the url.
        /// </summary>
        private QueryBuilderSettings settings;

        /// <summary>
        /// The root type of the query, if different from T.
        /// </summary>
        private Type rootType;

        /// <summary>
        /// The shard identifiers to target.
        /// </summary>
        private IEnumerable<ShardIdentifier> shardIds;

        /// <summary>
        /// The database type to use.
        /// </summary>
        private DatabaseType databaseType;

        /// <summary>
        /// The cached results.
        /// </summary>
        private IQueryable<T> results;

        /// <summary>
        /// The count of data, omitting skip/top filters.
        /// </summary>
        private long? count;

        /// <summary>
        /// The transform function.
        /// </summary>
        private Func<IEnumerable<AggregateResult>, List<T>> transform;

        /// <summary>
        /// Initializes a new instnace of the DynamicQueryable class.
        /// </summary>
        /// <param name="settings">The setttings to use.</param>
        /// <param name="databaseType">The database type to target.</param>
        /// <param name="shardIds">The list of shard ids.</param>
        public DynamicQueryable(
            QueryBuilderSettings settings, 
            DatabaseType databaseType, 
            IEnumerable<ShardIdentifier> shardIds)
            : this (settings, null, databaseType, shardIds, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DynamicQueryable class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <param name="rootType">The root type of the query.</param>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIds">The list of shard ids.</param>
        /// <param name="transform">The transform to use, if applicable.</param>
        internal DynamicQueryable(
            QueryBuilderSettings settings,
            Type rootType,
            DatabaseType databaseType,
            IEnumerable<ShardIdentifier> shardIds,
            Func<IEnumerable<AggregateResult>, List<T>> transform)
        {
            this.settings = settings;
            this.shardIds = shardIds;
            this.databaseType = databaseType;
            this.rootType = rootType;
            this.transform = transform;
            this.Provider = new DynamicQueryProvider(this);
            this.Expression = Expression.Constant(this);
        }

        /// <summary>
        /// Gets the element type.
        /// </summary>
        public Type ElementType
        {
            get
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression Expression
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        public IQueryProvider Provider
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the query timeout.
        /// </summary>
        public TimeSpan? Timeout
        {
            get;
            set;
        }

        /// <summary>
        /// Get the enumerator for the current query.
        /// </summary>
        /// <returns>The relevant enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (this.results == null)
            {
                this.LoadData(this.settings);
            }

            return this.results.GetEnumerator();
        }

        /// <summary>
        /// Gets the untyped enumerator for the current query.
        /// </summary>
        /// <returns>The untyped enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Use reflection to generate the correct builder.
        /// </summary>
        /// <param name="rootType">The root type for the builder.</param>
        /// <param name="settings">The settings to pass to the constructor.</param>
        /// <returns>The generated query buidler.</returns>
        internal static IQueryBuilder GetBuilder(Type rootType, QueryBuilderSettings settings)
        {
            IQueryBuilder qbuilder = null;
            if (rootType != null)
            {
                qbuilder = TypeCache.ReflectCorrectBuilder(rootType, settings);
            }
            else
            {
                qbuilder = new QueryBuilder<T>(settings);
            }

            return qbuilder;
        }

        /// <summary>
        /// Create and run a count query for the current dataset.
        /// </summary>
        /// <returns>The count of rows in the query, omitting top and skip elements.</returns>
        private long CreateAndRunCountQuery()
        {
            if (this.count.HasValue == false)
            {
                QueryBuilderSettings settings = new QueryBuilderSettings();
                settings.QueryProcessor = this.settings.QueryProcessor;
                settings.Aggregates.AddRange(this.settings.Aggregates);
                settings.ArgumentFilter = this.settings.ArgumentFilter;
                settings.Filter = this.settings.Filter;
                settings.Groupings.AddRange(this.settings.Groupings);
                settings.Selects.AddRange(this.settings.Selects);
                settings.Where = this.settings.Where;
                settings.Url = this.settings.Url;
                settings.EntitySetType = this.settings.EntitySetType;
                settings.DefaultJoinType = this.settings.DefaultJoinType;
                settings.Executor = this.settings.Executor;

                IQueryBuilder qbuilder = GetBuilder(this.rootType, settings);
                SelectQuery countQuery = new SelectQuery();
                countQuery.Source = qbuilder.Query;
                countQuery.Source.Alias = "CountQuery";
                countQuery.Columns.Add(new QueryColumn()
                {
                    Alias = "TotalCount",
                    Expression = settings.Executor.CountExpression,
                    Source = countQuery.Source
                });

                StoredProcedureMultiple spm = settings.Executor.RunMultiple(countQuery, this.databaseType, this.shardIds);
                this.count = (long)spm.ReadRawData(0).Rows[0][0];
            }

            return this.count.Value;
        }

        /// <summary>
        /// Load the data.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        private void LoadData(QueryBuilderSettings settings)
        {
            if (this.rootType != null)
            {
                IEnumerable<AggregateResult> aggs = AggregateHelper.Aggregate(
                    settings,
                    this.rootType,
                    this.databaseType,
                    this.shardIds,
                    this.Timeout);

                if (this.transform == null)
                {
                    this.results = aggs.Cast<T>().AsQueryable();
                }
                else
                {
                    this.results = transform(aggs).AsQueryable();
                }
            }
            else
            {
                QueryBuilder<T> builder = new QueryBuilder<T>(settings);
                this.results = builder.Run(this.databaseType, this.shardIds, this.Timeout);
            }
        }

        /// <summary>
        /// The dynamic query provider class.
        /// </summary>
        private class DynamicQueryProvider : IQueryProvider
        {
            /// <summary>
            /// Initializes a new instance of the DynamicQueryProvider class.
            /// </summary>
            public DynamicQueryProvider(DynamicQueryable<T> queryable)
            {
                this.Queryable = queryable;
            }

            /// <summary>
            /// Gets the current queryable in scope.
            /// </summary>
            public DynamicQueryable<T> Queryable
            {
                get;
                private set;
            }

            /// <summary>
            /// Create a typed query using the given expression.
            /// </summary>
            /// <typeparam name="TElement">The type of the query.</typeparam>
            /// <param name="expression">The expression to use.</param>
            /// <returns>The resuling query.</returns>
            public IQueryable CreateQuery(Expression expression)
            {
                object result = this.ExecuteCreateQuery(expression);

                return result as IQueryable;
            }

            /// <summary>
            /// Create a query using the given expression.
            /// </summary>
            /// <param name="expression">The expression to use.</param>
            /// <returns>The resulting query.</returns>
            public IQueryable<TResult> CreateQuery<TResult>(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Call)
                {
                    MethodCallExpression mce = expression as MethodCallExpression;
                    if (mce.Method.Name == "Take")
                    {
                        this.HandleTop(mce);
                    }
                    else if (mce.Method.Name == "OrderBy")
                    {
                        this.HandleOrderBy(mce, true);
                    }
                    else if (mce.Method.Name == "ThenBy")
                    {
                        this.HandleOrderBy(mce, true);
                    }
                    else if (mce.Method.Name == "OrderByDescending")
                    {
                        this.HandleOrderBy(mce, false);
                    }
                    else if (mce.Method.Name == "ThenByDescending")
                    {
                        this.HandleOrderBy(mce, false);
                    }
                    else if (mce.Method.Name == "OfType")
                    {
                        return this.HandleOfType<TResult>();
                    }
                }

                return this.Queryable as IQueryable<TResult>;
            }

            /// <summary>
            /// Execute the given query.
            /// </summary>
            /// <typeparam name="TResult">The type of the output.</typeparam>
            /// <param name="expression">The expression to use.</param>
            /// <returns>The result.</returns>
            public object Execute(Expression expression)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Execute the given query.
            /// </summary>
            /// <param name="expression">The expression to use.</param>
            /// <returns>The result.</returns>
            public TResult Execute<TResult>(Expression expression)
            {
                // Handle for the case where we are resolving a count.
                // In this case TResult will be of type int.
                if (expression.NodeType == ExpressionType.Call)
                {
                    MethodCallExpression mce = expression as MethodCallExpression;
                    switch (mce.Method.Name)
                    {
                        case "LongCount":
                            long count = this.Queryable.CreateAndRunCountQuery();
                            return (TResult)(object)count;
                    }
                }

                throw new NotImplementedException();
            }

            /// <summary>
            /// Recurse the member expression to build the prefix.
            /// </summary>
            /// <param name="memberExpression">The leaf member expression.</param>
            /// <returns>The constructed prefix.</returns>
            private static string BuildPrefix(MemberExpression memberExpression)
            {
                if (memberExpression == null)
                {
                    return string.Empty;
                }

                StringBuilder builder = new StringBuilder();
                Stack<MemberExpression> members = new Stack<MemberExpression>();
                members.Push(memberExpression);
                string separator = string.Empty;
                while (members.Count > 0)
                {
                    MemberExpression item = members.Pop();
                    MemberExpression pre = item.Expression as MemberExpression;
                    if (pre != null)
                    {
                        members.Push(pre);
                    }

                    builder.Insert(0, separator);
                    builder.Insert(0, item.Member.Name);
                }

                return builder.ToString();
            }

            /// <summary>
            /// Execute the create query using the original type.
            /// </summary>
            /// <param name="expression">The expression to pass.</param>
            /// <returns>The unwrapped object result.</returns>
            private object ExecuteCreateQuery(Expression expression)
            {
                MethodInfo generic = this.GetType().GetMethods()
                    .Where(p => p.IsGenericMethod == true && p.Name.StartsWith("CreateQuery"))
                    .SingleOrDefault();
                MethodInfo method = generic.MakeGenericMethod(typeof(T));

                object result = method.Invoke(this, new object[] { expression });

                return result;
            }

            /// <summary>
            /// Set the top statement correct when modified by WebApi.
            /// </summary>
            /// <param name="mce">The method call in scope.</param>
            private void HandleTop(MethodCallExpression mce)
            {
                if (mce.Arguments[1].NodeType == ExpressionType.Constant)
                {
                    int top = (int)((ConstantExpression)mce.Arguments[1]).Value;
                    if (this.Queryable.settings.Top.HasValue == false ||
                        this.Queryable.settings.Top.Value > top)
                    {
                        this.Queryable.settings.Top = (int)((ConstantExpression)mce.Arguments[1]).Value;
                    }
                }
            }

            /// <summary>
            /// Append any additional order bys added by WebApi.
            /// </summary>
            /// <param name="mce">The method call in scope.</param>
            /// <param name="ascending">True if order is ascending, otherwise false.</param>
            private void HandleOrderBy(MethodCallExpression mce, bool ascending)
            {
                if (this.Queryable.settings.IsAggregateQuery == true)
                {
                    this.HandleOrderByWithGroupBy();
                }
                else if (this.Queryable.rootType == null)
                {
                    this.HandleOrderByWithoutGroupBy(mce, ascending);
                }
                else
                {
                    this.HandleOrderByWithAggregateResult();
                }
            }

            /// <summary>
            /// Handle the order by when an aggregate result is used without an aggregate query.
            /// </summary>
            private void HandleOrderByWithAggregateResult()
            {
                List<string> names = TypeCache.GetKeys(this.Queryable.rootType);
                foreach (string name in names)
                {
                    oem.OrderedPropertyType order = new oem.OrderedPropertyType();
                    order.Prefix = string.Empty;
                    order.Name = name;
                    order.Ascending = true;
                    if (this.Queryable.settings.Orderings.Any(p => p.Prefix == order.Prefix && p.Name == order.Name) == false)
                    {
                        this.Queryable.settings.Orderings.Add(order);
                    }
                }
            }

            /// <summary>
            /// Handle the order by when a group by is present.
            /// </summary>
            private void HandleOrderByWithGroupBy()
            {
                foreach (oem.GroupByReferenceType grouping in this.Queryable.settings.Groupings)
                {
                    List<string> groups = new List<string>();
                    groups.AddRange(grouping.Properties.Select(p => p.Name));

                    foreach (string group in groups)
                    {
                        oem.OrderedPropertyType order = new oem.OrderedPropertyType();
                        int pos = group.IndexOf('/');
                        order.Prefix = string.Empty;
                        order.Name = group.Substring(pos + 1);
                        order.Ascending = true;
                        if (pos > 0)
                        {
                            order.Prefix = group.Substring(0, pos);
                        }

                        if (this.Queryable.settings.Orderings.Any(p => p.Prefix == order.Prefix && p.Name == order.Name) == false)
                        {
                            this.Queryable.settings.Orderings.Add(order);
                        }
                    }
                }
            }

            /// <summary>
            /// Append any additional order bys added by WebApi on non groupby queries.
            /// </summary>
            /// <param name="mce">The method call in scope.</param>
            /// <param name="ascending">True if order is ascending, otherwise false.</param>
            private void HandleOrderByWithoutGroupBy(MethodCallExpression mce, bool ascending)
            {
                for (int i = 1; i < mce.Arguments.Count; i++)
                {
                    string name = null, prefix = null;
                    UnaryExpression ue = mce.Arguments[i] as UnaryExpression;
                    MemberExpression member = ((LambdaExpression)ue.Operand).Body as MemberExpression;
                    if (member != null)
                    {
                        name = member.Member.Name;
                        prefix = BuildPrefix(member.Expression as MemberExpression);
                    }
                    else
                    {
                        ConditionalExpression conditional = ((LambdaExpression)ue.Operand).Body as ConditionalExpression;
                        string fullName = conditional.IfFalse.ToString();
                        string[] parts = fullName.Split('.');
                        if (parts.Length == 3)
                        {
                            name = parts[2].Replace(")", string.Empty);
                            prefix = parts[1];
                        }
                    }

                    oem.OrderedPropertyType order = new oem.OrderedPropertyType();
                    order.Name = name;
                    order.Prefix = prefix;
                    order.Ascending = ascending;
                    if (string.IsNullOrEmpty(name) == false && prefix != null &&
                        this.Queryable.settings.Orderings.Any(p => p.Name == order.Name && p.Prefix == order.Prefix) == false)
                    {
                        this.Queryable.settings.Orderings.Add(order);
                    }
                }
            }

            /// <summary>
            /// Handle calls of the of type variety.
            /// </summary>
            /// <typeparam name="TResult">The desired type of the queryable.</typeparam>
            /// <returns>The correctly typed queryable.</returns>
            private DynamicQueryable<TResult> HandleOfType<TResult>()
            {
                if (typeof(TResult) != typeof(T))
                {
                    return new DynamicQueryable<TResult>(
                        this.Queryable.settings,
                        this.Queryable.rootType,
                        this.Queryable.databaseType,
                        this.Queryable.shardIds,
                        null);
                }

                return null;
            }
        }
    }
}
