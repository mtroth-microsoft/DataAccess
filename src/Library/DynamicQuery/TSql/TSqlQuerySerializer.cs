// -----------------------------------------------------------------------
// <copyright file="TSqlQuerySerializer.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// Helper class for serializing queries to t-sql.
    /// </summary>
    internal sealed class TSqlQuerySerializer
    {
        /// <summary>
        /// Serialize the built query.
        /// </summary>
        /// <param name="query">The query to serialize.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>The serialized query.</returns>
        internal static string Serialize(QuerySource query, ParameterContext context, SqlFormatter formatter)
        {
            SelectQuery select = query as SelectQuery;
            if (select != null && select.PathQuery != null)
            {
                return TSqlProjectionHelper.Serialize(select);
            }
            else
            {
                TSqlQuerySerializer serializer = new TSqlQuerySerializer();
                return serializer.SerializeSource(query, false, context, formatter);
            }
        }

        /// <summary>
        /// Serialize a list of deletes into a single statement.
        /// </summary>
        /// <param name="deletes">The list of deletes.</param>
        /// <returns>The serialized sql string.</returns>
        internal static string SerializeDeletes(List<DeleteQuery> deletes)
        {
            ParameterContext context = new ParameterContext();
            SqlFormatter formatter = new SqlFormatter();
            StringBuilder builder = new StringBuilder();
            foreach (DeleteQuery dq in deletes)
            {
                CompositeNode node = new CompositeNode(null, null, dq.Target.Type, false);

                TSqlQuerySerializer serializer = new TSqlQuerySerializer();
                string sql = serializer.SerializeSource(dq.Target, false, context, formatter);

                TSqlFilterSerializer fs = new TSqlFilterSerializer(node);
                fs.Filter = dq.Filter;
                string filter = fs.Serialize(context, formatter);

                builder.AppendFormat("DELETE {0}", sql);
                builder.AppendFormat("\nWHERE {0}\n;", filter);
            }

            foreach (DeleteQuery dq in deletes)
            {
                dq.Target.Parameters.AddRange(context.ReadAll());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize a list of inserts into a single string.
        /// </summary>
        /// <param name="inserts">The list of inserts.</param>
        /// <returns>The serialized sql string.</returns>
        internal static string SerializeInserts(List<InsertQuery> inserts)
        {
            ParameterContext context = new ParameterContext();
            SqlFormatter formatter = new SqlFormatter();
            StringBuilder builder = new StringBuilder();

            foreach (InsertQuery iq in inserts)
            {
                TSqlSerializer columnSerializaer = new TSqlSerializer();
                TSqlQuerySerializer serializer = new TSqlQuerySerializer();
                string sql = serializer.SerializeSource(iq.Target, false, context, formatter);
                builder.AppendFormat("INSERT INTO {0} (\n", sql);
                string separator = "     ";
                foreach (QueryColumn column in iq.Columns)
                {
                    builder.AppendFormat("{0}[{1}]", separator, column.Name);
                    separator = "\n    ,";
                }

                separator = "     ";
                builder.Append(")\nVALUES (\n");
                foreach (QueryColumn column in iq.Columns)
                {
                    string value = columnSerializaer.SerializeColumn(column, false, context);
                    builder.AppendFormat("{0}{1}", separator, value);
                    separator = "\n    ,";
                }

                builder.Append(");");
            }

            foreach (InsertQuery iq in inserts)
            {
                iq.Target.Parameters.AddRange(context.ReadAll());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the query.
        /// </summary>
        /// <param name="merge">The merge query to serialize.</param>
        /// <param name="isSingleEntity">True if merge is a single entity merge.</param>
        /// <returns>The serialized sql string.</returns>
        internal static string SerializeMerge(MergeQuery merge, bool isSingleEntity)
        {
            TSqlQuerySerializer serializer = new TSqlQuerySerializer();
            ParameterContext context = new ParameterContext() { IsSingleRowMerge = isSingleEntity };
            string sql = serializer.SerializeSource(merge, false, context, new SqlFormatter());
            merge.Target.Parameters.AddRange(context.ReadAll());

            return sql;
        }

        /// <summary>
        /// Serializes the parameter declaration for the current query.
        /// </summary>
        /// <param name="source">The source to serialize parameter declaration for.</param>
        /// <returns>The parameter declaration string.</returns>
        internal static string SerializeParameterDeclaration(QuerySource source)
        {
            StringBuilder builder = new StringBuilder();
            string separator = string.Empty;
            foreach (Parameter p in source.Parameters)
            {
                if (p.ParameterName[0] != '@')
                {
                    builder.Append('@');
                }

                builder.Append(separator);
                builder.Append(p.ParameterName);
                builder.Append(" ");
                builder.Append(SerializeSqlType(p.Value));
                separator = ",";
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the source statement.
        /// </summary>
        /// <param name="source">The source to serialize.</param>
        /// <param name="addAlias">True to add the alias, otherwise false.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The formmater to use.</param>
        /// <returns>The query command text.</returns>
        internal string SerializeSource(
            object source,
            bool addAlias,
            ParameterContext context,
            SqlFormatter formatter)
        {
            if (source is SelectQuery)
            {
                return this.SerializeSelect(source as SelectQuery, addAlias, context, formatter);
            }
            else if (source is QueryTable)
            {
                return this.SerializeTable(source as QueryTable, addAlias, context, formatter);
            }
            else if (source is ScriptQuery)
            {
                return this.SerializeScript(source as ScriptQuery, addAlias, context, formatter);
            }
            else if (source is UnionQuery)
            {
                return this.SerializeUnion(source as UnionQuery, addAlias, context, formatter);
            }
            else if (source is MergeQuery)
            {
                return this.SerializeMerge(source as MergeQuery, context, formatter);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Serialize the sql type name of the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The serialized sql type name.</returns>
        private static string SerializeSqlType(object value)
        {
            string type = "nvarchar(128)";
            if (value != null)
            {
                if (value is int)
                {
                    type = "int";
                }
                else if (value is DateTime)
                {
                    type = "datetime";
                }
                else if (value is DateTimeOffset)
                {
                    type = "datetimeoffset";
                }
                else if (value is string)
                {
                    type = "nvarchar(max)";
                }
                else if (value is Guid)
                {
                    type = "uniqueidentifier";
                }
                else if (value is long)
                {
                    type = "bigint";
                }
                else if (value is short)
                {
                    type = "smallint";
                }
                else if (value is byte)
                {
                    type = "tinyint";
                }
                else if (value is byte[])
                {
                    type = "varbinary(max)";
                }
            }

            return type;
        }

        /// <summary>
        /// Serialize the union type.
        /// </summary>
        /// <param name="unionType">The union type to serialize.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>The serialized string.</returns>
        private static string SerializeUnionType(UnionQueryType unionType, SqlFormatter formatter)
        {
            StringBuilder builder = new StringBuilder(formatter.Indent + "UNION");
            if (unionType == UnionQueryType.All)
            {
                builder.Append(" ALL");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Set the expression value for the provided column, given certain attributes.
        /// </summary>
        /// <param name="column">The column to inspect.</param>
        /// <param name="settings">The bulk writer settings if applicable.</param>
        /// <returns>The expression value.</returns>
        private static string SetAttributeBasedExpression(QueryColumn column, BulkWriterSettings settings)
        {
            if (column.IsUpdatedTime || column.IsInsertedTime)
            {
                string defaultTime = "GETUTCDATE()";
                if (settings != null)
                {
                    return settings.ChangedTime != default(DateTimeOffset) ? "'" + settings.ChangedTime.ToString("o") + "'" : defaultTime;
                }

                return defaultTime;
            }
            else if (column.IsChangedBy == true)
            {
                string defaultUser = System.Threading.Thread.CurrentPrincipal.Identity.Name;
                if (settings != null)
                {
                    return string.Format("'{0}'", settings.ChangedByUser ?? defaultUser);
                }

                return defaultUser;
            }
            else
            {
                return column.Expression;
            }
        }

        /// <summary>
        /// Serialize the query into command text.
        /// </summary>
        /// <param name="merge">The merge query to serialize.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The formmater to use.</param>
        /// <returns>The query command text.</returns>
        private string SerializeMerge(
            MergeQuery merge,
            ParameterContext context,
            SqlFormatter formatter)
        {
            CompositeNode node = new CompositeNode(null, null, merge.SourceJoin.Source.Type, false);
            CompositeNode child = new CompositeNode(node, null, merge.SourceJoin.Target.Type, false);
            TSqlFilterSerializer fs = new TSqlFilterSerializer(node);

            TSqlSerializer serializer = new TSqlSerializer();
            StringBuilder builder = new StringBuilder();
            if (merge.Prolog != null)
            {
                builder.Append(formatter.Indent);
                builder.Append(merge.Prolog.Expression ?? this.SerializeSource(merge.Prolog.Query, false, context, formatter));
                builder.AppendLine();
            }

            if (merge.ConcurrencyCheck != null)
            {
                builder.Append("\nIF NOT EXISTS (\n");
                builder.Append(this.SerializeSource(merge.ConcurrencyCheck, false, context, formatter));
                builder.Append("\n)");
                builder.Append("\nBEGIN\n");
            }

            builder.Append(formatter.Indent);
            builder.Append("MERGE ");
            if (merge.Top != null && merge.Top.TopSpecified == true)
            {
                builder.Append(merge.Top.Serialize());
                builder.Append(" ");
            }

            builder.AppendFormat("\n{0}INTO ", formatter.Indent);
            builder.Append(this.SerializeSource(merge.Target, true, context, formatter));

            builder.AppendFormat("\n{0}USING ", formatter.Indent);
            builder.Append(this.SerializeSource(merge.SourceJoin.Source, true, context, formatter));

            builder.AppendFormat("\n{0}ON ", formatter.Indent);
            string join = serializer.SerializeJoin(merge.SourceJoin, context, formatter);
            int pos = join.IndexOf("ON ") + 3;
            builder.Append(join.Substring(pos));

            if (merge.MatchedColumns.Count > 0 || merge.DeleteMatchedFromTarget == true)
            {
                builder.AppendFormat("\n{0}WHEN MATCHED ", formatter.Indent);
                if (merge.WhenMatched != null && merge.WhenMatched.Item != null)
                {
                    fs.Filter = merge.WhenMatched;
                    builder.AppendFormat("AND\n{0}", formatter.Indent);
                    builder.Append(fs.Serialize(context, formatter));
                }

                builder.AppendFormat("\n{0}THEN ", formatter.Indent);
                if (merge.DeleteMatchedFromTarget == true)
                {
                    builder.Append("DELETE");
                }
                else
                {
                    builder.Append("UPDATE SET ");
                    string separator = string.Concat("\n", formatter.Indent, "       ");
                    foreach (QueryColumn column in merge.MatchedColumns)
                    {
                        builder.Append(separator);
                        separator = string.Concat("\n", formatter.Indent, "      ,");
                        builder.Append("[");
                        builder.Append(merge.Target.Alias);
                        builder.Append("].[");
                        builder.Append(column.Name);
                        builder.Append("] = ");
                        if (string.IsNullOrEmpty(column.Expression) == false ||
                            column.IsUpdatedTime == true ||
                            column.IsInsertedTime == true ||
                            column.IsChangedBy == true)
                        {
                            builder.Append(SetAttributeBasedExpression(column, merge.BulkWriterSettings));
                        }
                        else
                        {
                            builder.Append("[");
                            builder.Append(merge.SourceJoin.Source.Alias);
                            builder.Append("].[");
                            builder.Append(column.Name);
                            builder.Append("]");
                        }
                    }
                }
            }

            if (merge.TargetUnmatchedColumns.Count > 0)
            {
                builder.AppendFormat("\n{0}WHEN NOT MATCHED BY TARGET ", formatter.Indent);
                if (merge.WhenNotMatchedByTarget != null && merge.WhenNotMatchedByTarget.Item != null)
                {
                    fs.Filter = merge.WhenNotMatchedByTarget;
                    builder.AppendFormat("AND\n{0}", formatter.Indent);
                    builder.Append(fs.Serialize(context, formatter));
                }

                builder.AppendFormat("\n{0}THEN ", formatter.Indent);
                builder.Append("INSERT (");
                string separator = string.Concat("\n", formatter.Indent, "       ");
                foreach (QueryColumn column in merge.TargetUnmatchedColumns)
                {
                    builder.Append(separator);
                    builder.Append("[" + column.Name + "]");
                    separator = string.Concat("\n", formatter.Indent, "      ,");
                }

                builder.Append(") ");
                builder.AppendFormat("\n{0}VALUES (", formatter.Indent);
                separator = string.Concat("\n", formatter.Indent, "       ");
                foreach (QueryColumn column in merge.TargetUnmatchedColumns)
                {
                    if (column.Computed != DatabaseGeneratedOption.None ||
                        column.ConcurrencyCheck == true)
                    {
                        continue;
                    }

                    builder.Append(separator);
                    separator = string.Concat("\n", formatter.Indent, "      ,");
                    if (string.IsNullOrEmpty(column.Expression) == false ||
                        column.IsUpdatedTime == true ||
                        column.IsInsertedTime == true ||
                        column.IsChangedBy == true)
                    {
                        builder.Append(SetAttributeBasedExpression(column, merge.BulkWriterSettings));
                    }
                    else if (string.IsNullOrEmpty(column.Name) == false)
                    {
                        builder.Append("[");
                        builder.Append(column.Source.Alias);
                        builder.Append("].[");
                        builder.Append(column.Name);
                        builder.Append("]");
                    }
                }

                builder.Append(")");
            }

            if (merge.SourceUnmatchedColumns.Count > 0 || merge.DeleteUnmatchedFromTarget == true)
            {
                builder.AppendFormat("\n{0}WHEN NOT MATCHED BY SOURCE ", formatter.Indent);
                if (merge.WhenNotMatchedBySource != null && merge.WhenNotMatchedBySource.Item != null)
                {
                    fs.Filter = merge.WhenNotMatchedBySource;
                    builder.AppendFormat("AND\n{0}", formatter.Indent);
                    builder.Append(fs.Serialize(context, formatter));
                }

                builder.AppendFormat("\n{0}THEN ", formatter.Indent);
                if (merge.DeleteUnmatchedFromTarget == true)
                {
                    builder.Append("DELETE");
                }
                else
                {
                    builder.Append("UPDATE SET ");
                    string separator = string.Concat("\n", formatter.Indent, "       ");
                    foreach (QueryColumn column in merge.SourceUnmatchedColumns)
                    {
                        builder.Append(separator);
                        separator = string.Concat("\n", formatter.Indent, "      ,");
                        builder.Append(column.Source.Alias);
                        builder.Append(".");
                        builder.Append(column.Name);
                        builder.Append(" = ");
                        builder.Append(SetAttributeBasedExpression(column, merge.BulkWriterSettings));
                    }
                }
            }

            if (merge.OutputColumns.Count > 0)
            {
                string separator = string.Concat("\n", formatter.Indent, "       ");
                builder.AppendFormat("\n{0}OUTPUT $action, ", formatter.Indent);
                foreach (QueryColumn column in merge.OutputColumns)
                {
                    builder.Append(separator);
                    separator = string.Concat("\n", formatter.Indent, "      ,");
                    builder.Append(serializer.SerializeColumn(column, true, context));
                }
            }

            builder.Append(";");
            if (merge.ConcurrencyCheck != null)
            {
                builder.Append("\nEND");
                builder.Append("\nELSE");
                builder.Append("\nBEGIN");
                builder.AppendFormat("\n{0}RAISERROR('{1}', 16, 1)", formatter.Indent, merge.ConcurrencyError);
                builder.Append("\nEND");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the source statement.
        /// </summary>
        /// <param name="addAlias">True to add the alias, otherwise false.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The query command text.</returns>
        private string SerializeTable(
            QueryTable table,
            bool addAlias,
            ParameterContext context,
            SqlFormatter formatter)
        {
            StringBuilder builder = new StringBuilder();
            if (table.Name[0] == '@' || table.Name[0] == '#')
            {
                builder.AppendFormat("{0}", table.Name);
                table.Hint = HintType.None;
            }
            else
            {
                if (string.IsNullOrEmpty(table.Schema) == false)
                {
                    builder.AppendFormat("[{0}].", table.Schema);
                }

                builder.AppendFormat("[{0}]", table.Name);
            }

            if (addAlias == true)
            {
                builder.AppendFormat(" AS [{0}]", table.Alias);
            }

            if (table.Hint != HintType.None)
            {
                builder.AppendFormat(" WITH ({0})", table.Hint.ToString().ToUpper());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Implementation of the serialize operation.
        /// </summary>
        /// <param name="union">The union query to serialize.</param>
        /// <param name="addAlias">True to add an alias to the union, otherwise false.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>The serialized string.</returns>
        private string SerializeUnion(
            UnionQuery union,
            bool addAlias,
            ParameterContext context,
            SqlFormatter formatter)
        {
            TSqlSerializer serializer = new TSqlSerializer();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < union.Queries.Count; i++)
            {
                List<QueryOrder> order = new List<QueryOrder>();
                order.AddRange(union.Queries.ElementAt(i).OrderBy);
                union.Queries.ElementAt(i).OrderBy.Clear();
                string query = this.SerializeSource(union.Queries.ElementAt(i), false, context, formatter);
                if (i > 0)
                {
                    builder.AppendLine(SerializeUnionType(union.UnionTypes.ElementAt(i - 1), formatter));
                }

                builder.AppendLine(query);
                union.Queries.ElementAt(i).OrderBy.AddRange(order);
            }

            // Order By
            string separator = string.Concat("\n", formatter.Indent, "ORDER BY ");
            foreach (QueryOrder order in union.OrderBy)
            {
                string ordertext = serializer.SerializeQueryOrder(order, context);
                builder.Append(separator);
                builder.Append(ordertext);
                separator = string.Concat("\n", formatter.Indent, "        ,");
            }

            union.Parameters.Clear();
            union.Parameters.AddRange(context.ReadAll());
            if (addAlias == true)
            {
                builder.Insert(0, "(\n");
                builder.AppendFormat(") AS [{0}]", union.Alias);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Implementation of the serialize operation.
        /// </summary>
        /// <param name="script">The script query to serialize.</param>
        /// <param name="addAlias">True to add an alias to the union, otherwise false.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>The serialized string.</returns>
        private string SerializeScript(
            ScriptQuery script,
            bool addAlias,
            ParameterContext context,
            SqlFormatter formatter)
        {
            StringBuilder builder = new StringBuilder(formatter.Indent);
            for (int i = 0; i < script.Sources.Count; i++)
            {
                string query = this.SerializeSource(script.Sources.ElementAt(i), false, context, formatter);
                builder.Append(query);
                builder.Append(";");
                builder.AppendLine();
                builder.AppendLine();
            }

            script.Parameters.Clear();
            script.Parameters.AddRange(context.ReadAll());
            if (addAlias == true)
            {
                builder.Insert(0, "(\n");
                builder.AppendFormat(") AS [{0}]", script.Alias);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the query into command text.
        /// </summary>
        /// <param name="select">The select query to serialize.</param>
        /// <param name="addAlias">True to add the alias, otherwise false.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The formmater to use.</param>
        /// <returns>The query command text.</returns>
        private string SerializeSelect(
            SelectQuery select,
            bool addAlias,
            ParameterContext context,
            SqlFormatter formatter)
        {
            TSqlSerializer serializer = new TSqlSerializer();
            StringBuilder builder = new StringBuilder(formatter.Indent);
            if (select.Prolog != null)
            {
                builder.Append(select.Prolog.Expression ?? this.SerializeSource(select.Prolog.Query, false, context, formatter));
                builder.AppendLine();
            }

            builder.Append("SELECT ");
            if (select.Distinct == true)
            {
                builder.Append("DISTINCT ");
            }

            if (select.Top != null && select.OrderBy.Count == 0)
            {
                builder.Append(serializer.SerializeTop(select.Top));
                builder.Append(" ");
            }

            // Column Projection
            string separator = string.Concat("\n", formatter.Indent, "       ");
            foreach (QueryColumn column in select.Columns)
            {
                string columnText = serializer.SerializeColumn(column, true, context);
                if (!string.IsNullOrEmpty(columnText))
                {
                    builder.Append(separator);
                    builder.Append(columnText);
                    separator = string.Concat("\n", formatter.Indent, "      ,");
                }
            }

            // Source Clause
            if (select.Source != null)
            {
                builder.Append(formatter.Indent);
                builder.AppendFormat("\n{0}FROM ", formatter.Indent);
                SqlFormatter inner = new SqlFormatter() { Indent = formatter.Indent + string.Empty.PadLeft(5) };
                builder.Append(this.SerializeSource(select.Source, true, context, inner));
            }

            // Join Clause
            foreach (QueryJoin join in select.Joins)
            {
                builder.Append("\n");
                builder.Append(formatter.Indent);
                builder.Append(serializer.SerializeJoin(join, context, formatter));
            }

            // Where Clause
            TSqlFilterSerializer whereConditional = new TSqlFilterSerializer(select.RootNode);
            whereConditional.Filter = select.Filter;
            string whereClause = whereConditional.Serialize(context, formatter);
            if (string.IsNullOrEmpty(whereClause) == false)
            {
                whereClause = string.Concat("WHERE ", whereClause);
                builder.Append("\n");
                builder.Append(formatter.Indent);
                builder.Append(whereClause);
            }

            // GroupBy
            separator = string.Concat("\n", formatter.Indent, "GROUP BY ");
            foreach (QueryGroupBy group in select.GroupBy)
            {
                if (group.GroupingType == GroupingType.None)
                {
                    string columnText = serializer.SerializeColumn(group.NestedColumns[0], false, context);
                    if (!string.IsNullOrEmpty(columnText))
                    {
                        builder.Append(separator);
                        builder.Append(columnText);
                        separator = string.Concat("\n", formatter.Indent, "        ,");
                    }
                }
                else
                {
                    builder.Append(separator);
                    builder.Append("ROLLUP(");
                    string subtoken = string.Empty;
                    foreach (QueryColumn column in group.NestedColumns)
                    {
                        string columnText = serializer.SerializeColumn(column, false, context);
                        builder.Append(subtoken);
                        builder.Append(columnText);
                        subtoken = ",";
                    }

                    builder.Append(")");
                    separator = string.Concat("\n", formatter.Indent, "        ,");
                }
            }

            // Having clause.
            TSqlFilterSerializer havingConditional = new TSqlFilterSerializer(select.RootNode);
            havingConditional.Filter = select.Having;
            string havingClause = havingConditional.Serialize(context, formatter);
            if (string.IsNullOrEmpty(havingClause) == false)
            {
                havingClause = string.Concat("HAVING ", havingClause);
                builder.Append("\n");
                builder.Append(formatter.Indent);
                builder.Append(havingClause);
            }

            // Order By
            separator = string.Concat("\n", formatter.Indent, "ORDER BY ");
            foreach (QueryOrder order in select.OrderBy)
            {
                string ordertext = serializer.SerializeQueryOrder(order, context);
                builder.Append(separator);
                builder.Append(ordertext);
                separator = string.Concat("\n", formatter.Indent, "        ,");
            }

            // Top/Skip when orderby present.
            if ((select.Skip != null || select.Top != null) && select.OrderBy.Count > 0)
            {
                string topSkipFormat = string.Concat("\n", formatter.Indent, "OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY");
                string topSkip = string.Format(
                    topSkipFormat,
                    select.Skip != null && select.Skip.Value.HasValue ? select.Skip.Value : 0,
                    select.Top != null && select.Top.Value.HasValue ? select.Top.Value : long.MaxValue);
                builder.Append(topSkip);
            }
            else if (select.Skip != null && select.OrderBy.Count == 0)
            {
                throw new NotSupportedException("Skip not supported when no ordering is provided.");
            }

            if (select.QueryOption != QueryOptionType.None)
            {
                string optionFormat = string.Concat("\n", formatter.Indent, "OPTION ({0})");
                string option = this.SerializeQueryOption(select);
                builder.AppendFormat(optionFormat, option);
            }

            select.Parameters.Clear();
            select.Parameters.AddRange(context.ReadAll());
            if (addAlias == true)
            {
                builder.Insert(0, "(\n");
                builder.AppendFormat(") AS [{0}]", select.Alias);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the query option.
        /// </summary>
        /// <param name="select">The select query to serialize.</param>
        /// <returns>The serialized string.</returns>
        private string SerializeQueryOption(SelectQuery select)
        {
            string option = null;
            switch (select.QueryOption)
            {
                case QueryOptionType.Recompile:
                    option = "RECOMPILE";
                    break;

                case QueryOptionType.KeepPlan:
                    option = "KEEP PLAN";
                    break;

                case QueryOptionType.ForceOrder:
                    option = "FORCE ORDER";
                    break;
            }

            return option;
        }
    }
}
