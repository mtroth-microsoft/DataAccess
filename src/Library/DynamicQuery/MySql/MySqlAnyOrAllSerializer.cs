// -----------------------------------------------------------------------
// <copyright file="MySqlAnyOrAllSerializer.cs" company="Lensgrinder, Ltd.">
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
    /// Helper class for constructing any or all queries.
    /// </summary>
    internal static class MySqlAnyOrAllSerializer
    {
        /// <summary>
        /// Serialize the any or all type.
        /// </summary>
        /// <param name="anyorall">The any or all type.</param>
        /// <param name="context">The parameter context.</param>
        /// <param name="formatter">The sql formatter to use.</param>
        /// <returns>The serialized expression.</returns>
        public static string Serialize(
            AnyOrAllType anyorall,
            CompositeNode node,
            ParameterContext context,
            SqlFormatter formatter)
        {
            MySqlQuerySerializer serializer = new MySqlQuerySerializer();
            ExpressionType filter = anyorall.Item;
            string command = anyorall.Value ? "EXISTS (\n" : "NOT EXISTS (\n";
            if (anyorall is AllType)
            {
                command = anyorall.Value ? "NOT EXISTS(\n" : "EXISTS(\n";
                filter = InvertExpression(anyorall.Item);
            }

            QueryTable intermediateTable = null;
            QueryColumn targetKey = null;
            string imtarget = null;
            AndType and = ConfigurePseudoJoin(node, anyorall, out intermediateTable, out imtarget, out targetKey);
            if (filter != null)
            {
                and.Items.Add(filter);
            }

            IQueryBuilder instance = CreateQueryBuilder(anyorall, and);
            if (intermediateTable != null)
            {
                AddIntermediateJoin(instance, intermediateTable, targetKey, imtarget);
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(command);

            SqlFormatter inner = new SqlFormatter() { Indent = formatter.Indent + string.Empty.PadLeft(10) };
            string sql = serializer.SerializeSource(instance.Query, false, context, inner);

            builder.Append(sql);
            builder.Append(")");

            return builder.ToString();
        }

        /// <summary>
        /// Add the intermediate join to the subselect query.
        /// </summary>
        /// <param name="instance">The querybuilder to modify.</param>
        /// <param name="intermediateTable">The intermediate table to join to.</param>
        /// <param name="targetKey">The column containing the key in the target table.</param>
        /// <param name="imtarget">The name of the column in the intermediate table to join on.</param>
        private static void AddIntermediateJoin(
            IQueryBuilder instance,
            QueryTable intermediateTable,
            QueryColumn targetKey,
            string imtarget)
        {
            QueryJoin intermediateJoin = new QueryJoin();
            intermediateJoin.Source = instance.Query.Source;
            intermediateJoin.Target = intermediateTable;
            targetKey.Source = instance.Query.Source;
            QueryColumn qc = new QueryColumn() { Source = intermediateTable, Name = imtarget };
            intermediateJoin.Statements.Add(new Tuple<QueryColumn, QueryColumn>(targetKey, qc));
            instance.Query.Joins.Add(intermediateJoin);
        }

        /// <summary>
        /// Configure the pseudo join between the outer table and the subselect.
        /// </summary>
        /// <param name="node">The composte node of the outer query.</param>
        /// <param name="anyorall">The anyorall type being processed.</param>
        /// <param name="intermediateTable">The intermediate table, if applicable.</param>
        /// <param name="imtarget">The name of the target join column, if applicable.</param>
        /// <param name="targetKey">The target key column, if applicable.</param>
        /// <returns>The set of equal statements for the join.</returns>
        private static AndType ConfigurePseudoJoin(
            CompositeNode node,
            AnyOrAllType anyorall,
            out QueryTable intermediateTable,
            out string imtarget,
            out QueryColumn targetKey)
        {
            AndType and = new AndType();
            targetKey = null;
            imtarget = null;
            intermediateTable = null;

            string path = anyorall.Name;
            if (string.IsNullOrEmpty(anyorall.Prefix) == false)
            {
                path = anyorall.Prefix + '/' + anyorall.Name;
            }

            CompositeNode child = node.Align(path);
            Dictionary<string, string> join = TypeCache.LocateJoin(child.Parent, child, out intermediateTable);
            if (intermediateTable != null)
            {
                intermediateTable.Alias = string.Concat(anyorall.Alias, "To", anyorall.Name);
                targetKey = TypeCache.CreateColumns(null, child.ElementType).Where(p => p.IsKeyColumn == true).Single();
                imtarget = join.First().Value;
                QueryColumn sourceKey = TypeCache.CreateColumns(null, node.ElementType).Where(p => p.IsKeyColumn == true).Single();
                string imsource = join.First().Key;
                join.Clear();
                join[sourceKey.Name] = imsource;
            }

            foreach (string key in join.Keys)
            {
                EqualType statement = new EqualType()
                {
                    Predicate = new PropertyNameType() { Value = key, Alias = anyorall.Alias, Prefix = anyorall.Prefix },
                    Subject = new PropertyNameType() { Value = join[key], Alias = intermediateTable == null ? null : intermediateTable.Alias }
                };
                and.Items.Add(statement);
            }

            return and;
        }

        /// <summary>
        /// Create the query builder for generating the anyorall subselect.
        /// </summary>
        /// <param name="anyorall"></param>
        /// <param name="and"></param>
        /// <returns></returns>
        private static IQueryBuilder CreateQueryBuilder(AnyOrAllType anyorall, ExpressionType and)
        {
            Type t = anyorall.ElementType.GetProperty(anyorall.Name).PropertyType.GenericTypeArguments[0];
            QueryBuilderSettings settings = new QueryBuilderSettings() { Filter = new FilterType() { Item = and }, Template = anyorall.Name };
            IQueryBuilder instance = TypeCache.ReflectCorrectBuilder(t, settings);

            return instance;
        }

        /// <summary>
        /// Invert all the aspects of the provided expression type.
        /// </summary>
        /// <param name="expressionType">The expression type to invert.</param>
        /// <returns>The inverted expression.</returns>
        private static ExpressionType InvertExpression(ExpressionType expressionType)
        {
            ConditionType condition = expressionType as ConditionType;
            PredicateType predicate = expressionType as PredicateType;
            AnyOrAllType anyorall = expressionType as AnyOrAllType;
            if (condition != null)
            {
                return InvertCondition(condition);
            }
            else if (predicate != null)
            {
                return InvertPredicate(predicate);
            }
            else if (anyorall != null)
            {
                return InvertAnyOrAll(anyorall);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Invert a condition type.
        /// </summary>
        /// <param name="predicate">The condition to invert.</param>
        /// <returns>The inverted condition.</returns>
        private static ExpressionType InvertCondition(ConditionType condition)
        {
            ConditionType inverted = null;
            if (condition is AndType)
            {
                inverted = new OrType();
            }
            else if (condition is OrType)
            {
                inverted = new AndType();
            }
            else
            {
                throw new NotSupportedException();
            }

            foreach (ExpressionType et in condition.Items)
            {
                ExpressionType result = InvertExpression(et);
                inverted.Items.Add(result);
            }

            return inverted;
        }

        /// <summary>
        /// Invert a predicate type.
        /// </summary>
        /// <param name="predicate">The predicate to invert.</param>
        /// <returns>The inverted predicate.</returns>
        private static ExpressionType InvertPredicate(PredicateType predicate)
        {
            if (predicate is EqualType)
            {
                return new NotEqualType() { Subject = predicate.Subject, Predicate = predicate.Predicate };
            }
            else if (predicate is NotEqualType)
            {
                return new EqualType() { Subject = predicate.Subject, Predicate = predicate.Predicate };
            }
            else if (predicate is GreaterThanOrEqualType)
            {
                return new LessThanType() { Subject = predicate.Subject, Predicate = predicate.Predicate };
            }
            else if (predicate is GreaterThanType)
            {
                return new LessThanOrEqualType() { Subject = predicate.Subject, Predicate = predicate.Predicate };
            }
            else if (predicate is LessThanOrEqualType)
            {
                return new GreaterThanType() { Subject = predicate.Subject, Predicate = predicate.Predicate };
            }
            else if (predicate is LessThanType)
            {
                return new GreaterThanOrEqualType() { Subject = predicate.Subject, Predicate = predicate.Predicate };
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Invert an anyorall type.
        /// </summary>
        /// <param name="anyorall">The anyorall type to invert.</param>
        /// <returns>The inverted anyorall type.</returns>
        private static ExpressionType InvertAnyOrAll(AnyOrAllType anyorall)
        {
            anyorall.Value = !anyorall.Value;
            return anyorall;
        }
    }
}
