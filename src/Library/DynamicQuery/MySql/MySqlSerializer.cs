// -----------------------------------------------------------------------
// <copyright file="MySqlSerializer.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// Helper class for serializing to t-sql.
    /// </summary>
    internal class MySqlSerializer
    {
        /// <summary>
        /// Query serializer.
        /// </summary>
        private MySqlQuerySerializer serializer = new MySqlQuerySerializer();

        /// <summary>
        /// Serialize the reference.
        /// </summary>
        /// <param name="acr">The column to serialize.</param>
        /// <param name="nestedColumns">The referred columns.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <returns>The serialized string.</returns>
        internal string SerializeAggregateColumn(AggregateColumnReference acr, List<QueryColumn> nestedColumns, ParameterContext context)
        {
            StringBuilder builder = new StringBuilder();
            List<PropertyNameType> properties = acr.Predicatable.LocatePropertyNames();
            if (acr.AggregateType == AggregateType.None && acr.Predicatable is PropertyNameType)
            {
                builder.AppendFormat("`{0}`.`{1}`", nestedColumns[0].Source.Alias, nestedColumns[0].Name);
            }
            else if (acr.Predicatable.Serialize() == "*" && acr.AggregateType == AggregateType.Count)
            {
                builder.Append("COUNT(*)");
            }
            else
            {
                foreach (PropertyNameType pnt in properties)
                {
                    QueryColumn column = nestedColumns.SingleOrDefault(p =>
                        p.Alias.Equals(pnt.Value, StringComparison.OrdinalIgnoreCase) == true &&
                        p.Source.Path.Equals(pnt.Prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true);

                    if (column == null)
                    {
                        // if we couldn't find it the first time, it is likely because we are running a second time.
                        column = nestedColumns.SingleOrDefault(p =>
                            p.Name.Equals(pnt.Value, StringComparison.OrdinalIgnoreCase) == true &&
                            p.Source.Alias.Equals(pnt.Alias, StringComparison.OrdinalIgnoreCase) == true &&
                            p.Source.Path.Equals(pnt.Prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true);
                    }

                    pnt.Alias = column.Source.Alias;
                    pnt.Value = column.Name;
                }

                MySqlFilterSerializer qc = new MySqlFilterSerializer(null);
                string serialized = qc.SerializePredicatable(acr.Predicatable, context);
                if (acr.AggregateType != AggregateType.Multiple &&
                    acr.AggregateType != AggregateType.None)
                {
                    WithType wt = new WithType();
                    wt.AggregateType = acr.AggregateType;
                    wt.Predicate = acr.Predicatable;
                    serialized = GenerateWithAggregate(wt, serialized);
                }

                builder.Append(serialized);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Generate the aggregate expression for the given aggregation type.
        /// </summary>
        /// <param name="withType">The aggregation being generated.</param>
        /// <param name="suffix">The content of the aggregate function.</param>
        /// <returns>The serialized string.</returns>
        internal string GenerateWithAggregate(WithType withType, string suffix)
        {
            string format = GenerateAggregateFunction(withType);
            return string.Format(string.IsNullOrEmpty(format) == false ? "{0}{1})" : "{0}{1}", format, suffix);
        }

        /// <summary>
        /// Serialize the join statement.
        /// </summary>
        /// <param name="join">The join to serialize.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized statement.</returns>
        internal string SerializeJoin(QueryJoin join, ParameterContext context, SqlFormatter formatter)
        {
            if (join.IntermediateTable == null)
            {
                SqlFormatter inner = new SqlFormatter() { Indent = formatter.Indent + string.Empty.PadLeft(10) };
                StringBuilder builder = new StringBuilder();
                builder.Append(this.SerializeJoinType(join));
                builder.Append(" JOIN ");
                builder.Append(this.serializer.SerializeSource(join.Target, true, context, inner));

                string separator = "\n" + formatter.Indent + "ON ";
                foreach (Tuple<QueryColumn, QueryColumn> statement in join.Statements)
                {
                    builder.Append(separator);
                    builder.Append(this.SerializeColumn(statement.Item1, false, context));
                    builder.Append(" = ");
                    builder.Append(this.SerializeColumn(statement.Item2, false, context));
                    separator = "\n" + formatter.Indent + "AND ";
                }

                if (join.Exclusions != null)
                {
                    MySqlFilterSerializer qc = new MySqlFilterSerializer(join.TargetNode);
                    qc.Filter = join.Exclusions;
                    string exclusion = qc.Serialize(context, formatter);
                    builder.Append(separator);
                    builder.Append(exclusion);
                }

                return builder.ToString();
            }
            else
            {
                return this.SerializeManyToMany(join, context, formatter);
            }
        }

        /// <summary>
        /// Serialize the column.
        /// </summary>
        /// <param name="column">The column to serialize.</param>
        /// <param name="addAlias">True to add the alias, otherwise false.</param>
        /// <param name="context">The parameter context.</param>
        /// <returns>The serialized column.</returns>
        internal string SerializeColumn(QueryColumn column, bool addAlias, ParameterContext context)
        {
            StringBuilder builder = new StringBuilder();
            if (string.IsNullOrEmpty(column.Expression) == false)
            {
                if (context.IsSingleRowMerge == false)
                {
                    builder.Append(column.Expression);
                }
                else
                {
                    string pname = context.GetNextName();
                    builder.Append(pname);
                    context.Assign(pname, column.DefaultValue);
                }
            }
            else if (column.AggregateColumnReference != null)
            {
                builder.Append(this.SerializeAggregateColumn(column.AggregateColumnReference, column.NestedColumns, context));
            }
            else
            {
                if (string.IsNullOrEmpty(column.Source.Alias) == false)
                {
                    builder.AppendFormat("`{0}`.", column.Source.Alias);
                }

                builder.AppendFormat("`{0}`", column.Name);
            }

            if (addAlias == true)
            {
                builder.Append(" AS `");
                builder.Append(column.Alias ?? column.Name);
                builder.Append("`");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the top statement.
        /// </summary>
        /// <param name="top">The top to serialize.</param>
        /// <returns>The serialized statement.</returns>
        internal string SerializeTop(QueryTop top)
        {
            StringBuilder builder = new StringBuilder("TOP ");
            if (top.Parameter != null)
            {
                builder.AppendFormat("(@{0})", top.Parameter.ParameterName);
            }
            else
            {
                builder.Append(top.Value.Value.ToString());
            }

            if (top.WithTies == true)
            {
                builder.Append(" WITH TIES");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the order statement.
        /// </summary>
        /// <param name="order">The orderby to serialize.</param>
        /// <param name="context">The parameter context.</param>
        /// <returns>The serialized statement.</returns>
        internal string SerializeQueryOrder(QueryOrder order, ParameterContext context)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.SerializeColumn(order.Column, false, context));
            if (order.IsAscending == false)
            {
                builder.Append(" DESC");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Generate the aggregate function for the given aggregation type.
        /// </summary>
        /// <param name="withType">The aggregation being generated.</param>
        /// <returns>The serialized string.</returns>
        private static string GenerateAggregateFunction(WithType withType)
        {
            AggregateType aggregateType = withType.AggregateType;
            StringBuilder value = new StringBuilder();
            value.AppendFormat("{0}(", aggregateType.ToString().ToUpper());
            if (aggregateType == AggregateType.CountDistinct)
            {
                value.Clear();
                value.Append("COUNT(DISTINCT ");
            }
            else if (aggregateType == AggregateType.Average)
            {
                value.Clear();
                value.Append("AVG(");
            }
            else if (aggregateType == AggregateType.Merge)
            {
                value.Clear();
            }

            return value.ToString();
        }

        /// <summary>
        /// Serialize the join statement as a many to many.
        /// </summary>
        /// <param name="join">The join to serialize.</param>
        /// <param name="context">The parameter context to use.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized statement.</returns>
        private string SerializeManyToMany(QueryJoin join, ParameterContext context, SqlFormatter formatter)
        {
            MySqlQuerySerializer serializer = new MySqlQuerySerializer();

            // Many to many joins require single key on both sides.
            StringBuilder builder = new StringBuilder();
            QueryColumn sourceKey = TypeCache.CreateColumns(join.Source, join.SourceNode.ElementType).Where(p => p.IsKeyColumn == true).Single();
            QueryColumn targetKey = TypeCache.CreateColumns(join.Target, join.TargetNode.ElementType).Where(p => p.IsKeyColumn == true).Single();
            QueryColumn imsource = join.Statements.First().Item1;
            QueryColumn imtarget = join.Statements.First().Item2;

            SqlFormatter inner = new SqlFormatter() { Indent = formatter.Indent + string.Empty.PadLeft(10) };
            builder.Append(formatter.Indent);
            builder.Append(this.SerializeJoinType(join));
            builder.Append(" JOIN ");
            builder.Append(serializer.SerializeSource(join.IntermediateTable, true, context, inner));
            builder.AppendFormat("\n{0}ON ", formatter.Indent);
            builder.Append(this.SerializeColumn(sourceKey, false, context));
            builder.Append(" = ");
            builder.Append(this.SerializeColumn(imsource, false, context));
            builder.Append("\n");

            builder.Append(formatter.Indent);
            builder.Append(this.SerializeJoinType(join));
            builder.Append(" JOIN ");
            builder.Append(this.serializer.SerializeSource(join.Target, true, context, inner));
            builder.AppendFormat("\n{0}ON ", formatter.Indent);
            builder.Append(this.SerializeColumn(targetKey, false, context));
            builder.Append(" = ");
            builder.Append(this.SerializeColumn(imtarget, false, context));

            if (join.Exclusions != null)
            {
                MySqlFilterSerializer qc = new MySqlFilterSerializer(join.TargetNode);
                qc.Filter = join.Exclusions;
                string exclusion = qc.Serialize(context, formatter);
                builder.Append("\n" + formatter.Indent + "AND ");
                builder.Append(exclusion);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the given join type.
        /// </summary>
        /// <param name="join">The join to serialize.</param>
        /// <returns>The serialized string.</returns>
        private string SerializeJoinType(QueryJoin join)
        {
            string text = null;
            if (join.JoinType == QueryJoinType.InnerMerge)
            {
                throw new NotSupportedException("Inner Merge join not supported.");
            }
            else
            {
                text = join.JoinType.ToString().ToUpper();
            }

            return text;
        }
    }
}
