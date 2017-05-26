// -----------------------------------------------------------------------
// <copyright file="MergeQueryBuilder.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Web.OData;
    using OdataExpressionModel;

    /// <summary>
    /// Merge query builder class.
    /// </summary>
    internal sealed class MergeQueryBuilder
    {
        /// <summary>
        /// Initializes a new instance of the MergeQueryBuilder class.
        /// </summary>
        /// <param name="reader">Write set to inspect.</param>
        /// <param name="settings">Bulkd writer settings.</param>
        public MergeQueryBuilder(WriteSet reader, BulkWriterSettings settings)
        {
            this.Query = GenerateQuery(
                reader.QueryTable, 
                reader.TablePerType,
                reader.Columns.ToList(),
                settings, 
                reader.Data);
            this.Query.BulkWriterSettings = settings;
        }

        /// <summary>
        /// Initializes a new instance of the MergeQueryBuilder class.
        /// </summary>
        /// <param name="type">The entity type.</param>
        /// <param name="request">The write request.</param>
        /// <param name="settings">The bulk writer settings to use.</param>
        public MergeQueryBuilder(
            Type type,
            WriteRequest request, 
            BulkWriterSettings settings = null)
        {
            this.Query = GenerateQueryFromRequest(settings, request, type);
            this.Query.BulkWriterSettings = settings;
        }

        /// <summary>
        /// Initializes a new instance of the MergeQueryBuilder class.
        /// </summary>
        /// <param name="source">The soruce of the merge.</param>
        /// <param name="target">The target of the merge.</param>
        /// <param name="columns">The column list.</param>
        /// <param name="settings">The bulk writer settings to use.</param>
        public MergeQueryBuilder(
            QueryTable source, 
            QueryTable target, 
            List<QueryColumn> columns,
            BulkWriterSettings settings = null)
        {
            this.Query = GenerateSimpleQuery(settings, source, target, columns);
            this.Query.BulkWriterSettings = settings;
        }

        /// <summary>
        /// Gets the merge query.
        /// </summary>
        public MergeQuery Query
        {
            get;
            private set;
        }

        /// <summary>
        /// Generate the simplest possible merge operation.
        /// </summary>
        /// <param name="settings">The bulk writer settings to use.</param>
        /// <param name="source">The source table.</param>
        /// <param name="target">The target table.</param>
        /// <param name="columns">The list of columns.</param>
        /// <returns>The generated merge query.</returns>
        private static MergeQuery GenerateSimpleQuery(
            BulkWriterSettings settings,
            QueryTable source,
            QueryTable target,
            List<QueryColumn> columns)
        {
            source = new QueryTable(source);
            source.Hint = HintType.None;
            source.Alias = "Source";
            target = new QueryTable(target);
            target.Hint = HintType.None;
            target.Alias = "Target";

            List<QueryColumn> unmatchedColumns = new List<QueryColumn>();
            List<Tuple<QueryColumn, QueryColumn>> keys = new List<Tuple<QueryColumn, QueryColumn>>();
            foreach (QueryColumn column in columns)
            {
                QueryColumn targetColumn = new QueryColumn(column);
                targetColumn.Source = target;
                QueryColumn sourceColumn = new QueryColumn(column);
                sourceColumn.Source = source;

                unmatchedColumns.Add(sourceColumn);
                if (column.IsKeyColumn)
                {
                    keys.Add(new Tuple<QueryColumn, QueryColumn>(sourceColumn, targetColumn));
                }
            }

            MergeQuery query = new MergeQuery();
            query.Target = target;
            query.MatchedColumns.AddRange(columns.Where(p => ForUpdate(p) == true));
            query.TargetUnmatchedColumns.AddRange(unmatchedColumns.Where(p => ForInsert(p) == true));
            query.SourceJoin = new QueryJoin() { Source = source, Target = target };
            foreach (Tuple<QueryColumn, QueryColumn> key in keys)
            {
                query.SourceJoin.Statements.Add(key);
            }

            if (settings != null && settings.OnlyUpdateChanged == true)
            {
                query.WhenMatched = GenerateWhenMatchedFilter(query);
            }

            query.ConcurrencyCheck = settings.DoConcurrencyCheck ? ValidateTimestamp(target, source, query) : null;

            return query;
        }

        /// <summary>
        /// Generate a merge query from the given request.
        /// </summary>
        /// <param name="settings">The bulk writer settings to use.</param>
        /// <param name="request">The write request.</param>
        /// <param name="type">The entity type.</param>
        /// <returns>The generated merge query.</returns>
        private static MergeQuery GenerateQueryFromRequest(
            BulkWriterSettings settings,
            WriteRequest request,
            Type type)
        {
            QueryTable target = CreateTable(type, "Target");
            List<QueryColumn> columns = TypeCache.CreateColumns(target, target.Type);
            Dictionary<string, object> data = ExtractData(request);

            return GenerateQuery(target, false, columns, settings, data);
        }

        /// <summary>
        /// Generate a merge query.
        /// </summary>
        /// <param name="target">The target of the query.</param>
        /// <param name="tablePerType">True if current query is for a table per type member.</param>
        /// <param name="columns">The list of columns.</param>
        /// <param name="settings">The writer settings.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>The generated merge query.</returns>
        private static MergeQuery GenerateQuery(
            QueryTable target,
            bool tablePerType,
            List<QueryColumn> columns, 
            BulkWriterSettings settings, 
            IDictionary<string, object> data)
        {
            SelectQuery source = new SelectQuery() { Alias = "Source", Type = target.Type };
            List<Tuple<QueryColumn, QueryColumn>> keys = AddColumns(source, columns);
            MergeQuery query = CreateQuery(target, source, tablePerType, keys, data, settings);

            return query;
        }

        /// <summary>
        /// Create a merge query.
        /// </summary>
        /// <param name="target">The target of the merge.</param>
        /// <param name="source">The source of the merge.</param>
        /// <param name="tablePerType">True if current query is for a table per type member.</param>
        /// <param name="keys">The join keys.</param>
        /// <param name="data">The data to write.</param>
        /// <param name="settings">The writer settings.</param>
        /// <returns>The merge query.</returns>
        private static MergeQuery CreateQuery(
            QueryTable target,
            SelectQuery source,
            bool tablePerType,
            List<Tuple<QueryColumn, QueryColumn>> keys,
            IDictionary<string, object> data,
            BulkWriterSettings settings)
        {
            List<QueryColumn> matched = source.AllColumns
                .Where(p => ForUpdate(p) == true)
                .Select(p => new QueryColumn(p))
                .ToList();
            List<QueryColumn> unmatched = source.AllColumns
                .Where(p => ForInsert(p) == true || (p.IsKeyColumn == true && tablePerType == true))
                .Select(p => new QueryColumn(p))
                .ToList();

            foreach (QueryColumn keycol in unmatched.Where(p => p.IsKeyColumn && tablePerType == true))
            {
                keycol.Computed = DatabaseGeneratedOption.None;
            }

            foreach (string property in data.Keys)
            {
                QueryColumn column = source.AllColumns.Where(p => p.Alias == property).SingleOrDefault();
                if (column != null)
                {
                    object value = data[property];
                    column.Expression = PredicateType.SerializeValue(value);
                    column.DefaultValue = value;
                    source.Columns.Add(column);
                }
            }

            MergeQuery query = new MergeQuery();
            query.Target = target;
            query.MatchedColumns.AddRange(matched.Where(p => source.Columns.Any(q => q.Alias == p.Alias) || 
                                                                                     p.IsUpdatedTime == true || 
                                                                                     p.IsChangedBy == true));
            query.TargetUnmatchedColumns.AddRange(unmatched.Where(p => source.Columns.Any(q => q.Alias == p.Alias ||
                                                                                               p.IsInsertedTime == true ||
                                                                                               p.IsUpdatedTime == true ||
                                                                                               p.IsChangedBy == true)));
            query.SourceJoin = new QueryJoin() { Source = source, Target = target };
            foreach (Tuple<QueryColumn, QueryColumn> key in keys)
            {
                query.SourceJoin.Statements.Add(key);
            }

            if (settings != null && settings.OnlyUpdateChanged == true)
            {
                query.WhenMatched = GenerateWhenMatchedFilter(query);
            }

            QueryTable inserted = CreateTable(target.Type, "inserted");
            IEnumerable<QueryColumn> outputCols = TypeCache.CreateColumns(inserted, target.Type)
                .Where(p => TypeCache.IsNavigationalType(p.ElementType) == false && 
                           (p.DeclaringType == target.Type || p.IsKeyColumn == true));
            query.OutputColumns.AddRange(outputCols);

            query.ConcurrencyCheck = settings.DoConcurrencyCheck ? ValidateTimestamp(target, source, data) : null;
            foreach (QueryColumn column in source.Columns)
            {
                if (column.Alias != column.Name)
                {
                    string temp = column.Alias;
                    column.Alias = column.Name;
                    column.Name = temp;
                }
            }

            return query;
        }

        /// <summary>
        /// Verify timestamp columns, if applicable.
        /// </summary>
        /// <param name="target">The target table.</param>
        /// <param name="source">The source of the query.</param>
        /// <param name="merge">The merge query being built.</param>
        /// <returns>The concurrency query to use.</returns>
        private static SelectQuery ValidateTimestamp(
            QueryTable target, 
            QuerySource source, 
            MergeQuery merge)
        {
            SelectQuery query = null;
            List<QueryColumn> columns = TypeCache.CreateColumns(target, target.Type);
            QueryColumn concurrency = columns.SingleOrDefault(p => p.ConcurrencyCheck == true);
            if (concurrency != null)
            {
                query = new SelectQuery();
                query.Source = merge.SourceJoin.Source;
                query.Joins.Add(merge.SourceJoin);
                query.Columns.AddRange(columns.Where(p => p.IsKeyColumn == true));
                query.Filter = new FilterType();
                NotEqualType ne = new NotEqualType()
                {
                    Subject = new PropertyNameType() { Value = concurrency.Name, Alias = target.Alias },
                    Predicate = new PropertyNameType() { Value = concurrency.Name, Alias = source.Alias },
                };
                query.Filter.Item = ne;
            }

            return query;
        }

        /// <summary>
        /// Contruct a query prolog to validate the timestamp.
        /// </summary>
        /// <param name="target">The target table.</param>
        /// <param name="source">The source query.</param>
        /// <param name="data">The source data.</param>
        /// <returns>The concurrency query to use.</returns>
        private static SelectQuery ValidateTimestamp(
            QueryTable target, 
            SelectQuery source, 
            IDictionary<string, object> data)
        {
            SelectQuery query = null;
            QueryColumn concurrency = source.AllColumns.Where(p => p.ConcurrencyCheck == true).SingleOrDefault();
            if (concurrency != null)
            {
                object value;
                if (data.TryGetValue(concurrency.Alias, out value) == false)
                {
                    throw new InvalidDataFilterException("Concurrency check is turned on but no concurrency value has been provided.");
                }

                query = new SelectQuery();
                query.Source = target;
                query.Columns.Add(new QueryColumn()
                {
                    Expression = PredicateType.SerializeValue(value),
                    DefaultValue = value,
                    Alias = "ReceivedValue",
                    ElementType = concurrency.ElementType
                });
                query.Columns.Add(new QueryColumn(concurrency) { Alias = "ExistingValue", Source = target, Expression = null });

                query.Filter = new FilterType();
                AndType and = new AndType();
                query.Filter.Item = and;
                and.Items.Add(new NotEqualType()
                {
                    Subject = new PropertyNameType() { Value = concurrency.Name, Alias = target.Alias },
                    Predicate = value
                });
                IEnumerable<QueryColumn> keys = source.Columns.Where(p => p.IsKeyColumn == true);
                foreach (QueryColumn key in keys)
                {
                    and.Items.Add(new EqualType()
                    {
                        Subject = new PropertyNameType() { Value = key.Name, Alias = target.Alias },
                        Predicate = data[key.Alias]
                    });
                }
            }

            return query;
        }

        /// <summary>
        /// Populate the all columns collection of the given SelectQuery.
        /// </summary>
        /// <param name="source">The select query to populate.</param>
        /// <param name="columns">The list of columns.</param>
        /// <returns>The list of key columns.</returns>
        private static List<Tuple<QueryColumn, QueryColumn>> AddColumns(
            SelectQuery source,
            List<QueryColumn> columns)
        {
            List<Tuple<QueryColumn, QueryColumn>> keys = new List<Tuple<QueryColumn, QueryColumn>>();
            foreach (QueryColumn column in columns)
            {
                if (TypeCache.IsNavigationalType(column.ElementType) == false)
                {
                    QueryColumn copy = new QueryColumn(column);
                    copy.Source = source;
                    source.AllColumns.Add(copy);
                    if (column.IsKeyColumn)
                    {
                        keys.Add(new Tuple<QueryColumn, QueryColumn>(new QueryColumn(copy), column));
                    }
                }
            }

            return keys;
        }

        /// <summary>
        /// Populate the all columns collection of the given SelectQuery.
        /// </summary>
        /// <param name="source">The select query to populate.</param>
        /// <param name="target">The target table.</param>
        /// <param name="type">The type of the target table.</param>
        /// <returns>The list of key columns.</returns>
        private static List<Tuple<QueryColumn, QueryColumn>> AddColumns(
            SelectQuery source, 
            QueryTable target, 
            Type type)
        {
            List<QueryColumn> columns = TypeCache.CreateColumns(target, type);

            return AddColumns(source, columns);
        }

        /// <summary>
        /// Create a table for the given type.
        /// </summary>
        /// <param name="type">The type of the table.</param>
        /// <param name="alias">The alias of the table.</param>
        /// <returns>The created table.</returns>
        private static QueryTable CreateTable(Type type, string alias)
        {
            string name, schema;
            TypeCache.ExtractTableName(type, out name, out schema);
            QueryTable table = new QueryTable()
            {
                Name = name,
                Schema = schema,
                Type = type,
                Hint = HintType.None,
                Path = string.Empty,
                Alias = alias
            };

            return table;
        }

        /// <summary>
        /// Extract the data from the request.
        /// </summary>
        /// <param name="request">The request being processed.</param>
        /// <returns>The property bag of data.</returns>
        private static Dictionary<string, object> ExtractData(WriteRequest request)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            EdmEntityObject eeo = request.Entity as EdmEntityObject;
            IEnumerable<string> properties = eeo.GetChangedPropertyNames();
            foreach (string property in properties)
            {
                object value;
                request.Entity.TryGetPropertyValue(property, out value);
                data[property] = value;
            }

            if (request.Key != null)
            {
                foreach (KeyValuePair<string, object> member in request.Key.EntityKeyValues)
                {
                    if (data.ContainsKey(member.Key) == false)
                    {
                        data[member.Key] = member.Value;
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Generate a when matched filter.
        /// </summary>
        /// <param name="query">The query to inspect.</param>
        /// <returns>The resulting filter.</returns>
        private static FilterType GenerateWhenMatchedFilter(MergeQuery query)
        {
            FilterType whenMatched = new FilterType();
            OrType or = new OrType();
            whenMatched.Item = or;

            foreach (QueryColumn column in query.MatchedColumns)
            {
                if (ForUpdate(column) == true && column.IsUpdatedTime == false && column.IsChangedBy == false)
                {
                    NotEqualType ne = new NotEqualType();
                    ne.Subject = CreateProperty(column.Name, column.ElementType, query.SourceJoin.Source.Alias);
                    ne.Predicate = CreateProperty(column.Name, column.ElementType, query.SourceJoin.Target.Alias);

                    if (column.Nullable == true)
                    {
                        OrType subor = new OrType();
                        subor.Items.Add(CreateNullCheck(column, query.SourceJoin.Source.Alias, column, query.SourceJoin.Target.Alias));
                        subor.Items.Add(CreateNullCheck(column, query.SourceJoin.Target.Alias, column, query.SourceJoin.Source.Alias));
                        subor.Items.Add(ne);
                        or.Items.Add(subor);
                    }
                    else
                    {
                        or.Items.Add(ne);
                    }
                }
            }

            if (or.Items.Count > 0)
            {
                return whenMatched;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Create a null check pairing to ensure nulls are handled correctly.
        /// </summary>
        /// <param name="left">The left column.</param>
        /// <param name="source">The left alias.</param>
        /// <param name="right">The right column.</param>
        /// <param name="target">The right alias.</param>
        /// <returns>The and type wrapping the two conditions.</returns>
        private static AndType CreateNullCheck(QueryColumn left, string source, QueryColumn right, string target)
        {
            // (@Test IS NULL AND PropertyName IS NOT NULL) 
            EqualType eq = new EqualType();
            eq.Subject = CreateProperty(left.Name, left.ElementType, source);
            eq.Predicate = new NullType();
            NotEqualType ne = new NotEqualType();
            ne.Subject = CreateProperty(right.Name, right.ElementType, target);
            ne.Predicate = new NullType();
            AndType and = new AndType();
            and.Items.Add(eq);
            and.Items.Add(ne);

            return and;
        }

        /// <summary>
        /// Create a property with the given characteristics.
        /// </summary>
        /// <param name="value">The value for the property.</param>
        /// <param name="type">The type for the property.</param>
        /// <param name="alias">The table alias.</param>
        /// <returns>The created property name instance.</returns>
        private static PropertyNameType CreateProperty(string value, Type type, string alias)
        {
            return new PropertyNameType()
            {
                Value = value,
                ElementType = type,
                Alias = alias
            };
        }

        /// <summary>
        /// Determine whether the column is insertable.
        /// </summary>
        /// <param name="column">The query column to inspect.</param>
        /// <returns>True if the column is insertable, otherwise false.</returns>
        private static bool ForInsert(QueryColumn column)
        {
            return column.Computed == DatabaseGeneratedOption.None &&
                column.ConcurrencyCheck == false &&
                column.Alias != "$TypeName" &&
                TypeCache.IsNavigationalType(column.ElementType) == false;
        }

        /// <summary>
        /// Determine whether the column is updatable.
        /// </summary>
        /// <param name="column">The query column to inspect.</param>
        /// <returns>True if the column is updatable, otherwise false.</returns>
        private static bool ForUpdate(QueryColumn column)
        {
            return column.Computed == DatabaseGeneratedOption.None && 
                column.ConcurrencyCheck == false && 
                column.IsKeyColumn == false &&
                column.IsInsertedTime == false &&
                column.Alias != "$TypeName" &&
                TypeCache.IsNavigationalType(column.ElementType) == false;
        }
    }
}
