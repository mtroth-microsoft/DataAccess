// -----------------------------------------------------------------------
// <copyright file="AggregateHelper.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using OdataExpressionModel;

    /// <summary>
    /// Helper class to populate generic resultset from select query.
    /// </summary>
    internal static class AggregateHelper
    {
        /// <summary>
        /// Load data for an aggregate query that includes expanded properties.
        /// </summary>
        /// <typeparam name="T">The type of the return set.</typeparam>
        /// <param name="query">The query that has been executed.</param>
        /// <param name="spm">The procedure containing the result set.</param>
        /// <returns></returns>
        internal static IQueryable<T> Load<T>(
            SelectQuery query,
            StoredProcedureMultiple spm)
        {
            DataTable table = spm.ReadRawData(0);
            Dictionary<string, Result> results = new Dictionary<string, Result>();
            Dictionary<string, CompositeNode> nodeMap = new Dictionary<string, CompositeNode>();
            foreach (DataRow row in table.Rows)
            {
                Dictionary<string, Dictionary<string, object>> map = new Dictionary<string, Dictionary<string, object>>();
                int indexer = 0;
                foreach (QueryColumn column in query.Columns)
                {
                    string mapValue = GetPathValue(column);
                    Dictionary<string, object> value;
                    if (map.TryGetValue(mapValue, out value) == false)
                    {
                        value = new Dictionary<string, object>();
                        map[mapValue] = value;
                    }

                    DataColumn dc = table.Columns[indexer++];
                    value[column.Alias] = row[dc];
                }

                List<Result> joined = new List<Result>();
                foreach (string key in map.Keys)
                {
                    string separator = "|";
                    string itemKey = key;
                    foreach (string property in map[key].Keys)
                    {
                        itemKey += separator + map[key][property];
                    }

                    Result result;
                    if (results.TryGetValue(itemKey, out result) == false)
                    {
                        CompositeNode node;
                        if (nodeMap.TryGetValue(key, out node) == false)
                        {
                            node = query.RootNode.Align(key);
                            nodeMap[node.ComponentId] = node;
                        }

                        Type propertyType = null;
                        if (string.IsNullOrEmpty(key) == true)
                        {
                            propertyType = typeof(T);
                        }
                        else
                        {
                            propertyType = TypeCache.LocatePropertyType(typeof(T), key);
                        }

                        object typeName;
                        propertyType = TypeCache.NormalizeType(propertyType);
                        if (propertyType.IsAbstract == true && map[key].TryGetValue("$TypeName", out typeName) == true)
                        {
                            propertyType = TypeCache.LocateType(typeName.ToString());
                        }

                        Dictionary<string, object> dynamicProperties = new Dictionary<string, object>();
                        result = new Result() { Path = key, ComponentId = node.ComponentId, Member = Activator.CreateInstance(propertyType) };
                        results.Add(itemKey, result);
                        foreach (string property in map[key].Keys)
                        {
                            PropertyInfo pi = TypeCache.LocateProperty(propertyType, property);
                            if (pi != null)
                            {
                                TypeCache.SetValue(propertyType, property, result.Member, map[key][property]);
                            }
                            else if (property[0] != '$')
                            {
                                dynamicProperties[property] = map[key][property];
                            }
                        }

                        PropertyInfo dynamic = propertyType.GetProperty("DynamicProperties");
                        if (dynamic != null)
                        {
                            TypeCache.SetValue(propertyType, dynamic.Name, result.Member, dynamicProperties);
                        }
                    }

                    joined.Add(result);
                }

                foreach (Result result in joined)
                {
                    if (string.IsNullOrEmpty(result.Path) == false)
                    {
                        int pos = result.ComponentId.LastIndexOf('.');
                        string parentId = result.ComponentId.Substring(0, pos);
                        Result parent = joined.Single(p => p.ComponentId == parentId);
                        parent.Nodes.Add(result);
                    }
                }
            }

            Stack<Result> stack = new Stack<Result>(results.Values);
            while (stack.Count > 0)
            {
                Result result = stack.Pop();
                IEnumerable<string> properties = result.Nodes.Select(p => p.Path).Distinct();
                CompositeNode context = nodeMap[result.ComponentId];

                foreach (string propertyName in properties)
                {
                    TypeCache.PopulateData(propertyName, context, result);
                }

                foreach (Result child in result.Nodes)
                {
                    stack.Push(child);
                }
            }

            return results.Values.Where(p => string.IsNullOrEmpty(p.Path) == true).Select(p => p.Member).Cast<T>().AsQueryable();
        }

        /// <summary>
        /// Run an aggregate query.
        /// </summary>
        /// <param name="settings">The querybuilder settings to use.</param>
        /// <param name="rootType">The root type of the query.</param>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIds">The list of shard ids.</param>
        /// <param name="timeout">The query timeout or null to use the default.</param>
        /// <returns>The list of results.</returns>
        internal static IEnumerable<AggregateResult> Aggregate(
            QueryBuilderSettings settings,
            Type rootType,
            DatabaseType databaseType,
            IEnumerable<ShardIdentifier> shardIds,
            TimeSpan? timeout = null)
        {
            return GenerateAndRunQueryBuilder(settings, rootType, databaseType, shardIds, timeout);
        }

        /// <summary>
        /// Generate the query builder and run the query.
        /// </summary>
        /// <param name="settings">The querybuilder settings to use.</param>
        /// <param name="rootType">The root type of the query.</param>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIds">The list of shard Ids.</param>
        /// <param name="timeout">The query timeout or null to use the default.</param>
        /// <returns>The list of results.</returns>
        private static IEnumerable<AggregateResult> GenerateAndRunQueryBuilder(
            QueryBuilderSettings settings,
            Type rootType,
            DatabaseType databaseType,
            IEnumerable<ShardIdentifier> shardIds,
            TimeSpan? timeout)
        {
            IQueryBuilder instance = TypeCache.ReflectCorrectBuilder(rootType, settings);

            return PopulateGenericAggregate(instance.Query, databaseType, shardIds, timeout, settings.Executor);
        }

        /// <summary>
        /// Populate the aggregate results.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIds">The list of shard ids.</param>
        /// <param name="timeout">The query timeout or null to use the default.</param>
        /// <param name="executor">The executor to use.</param>
        /// <returns>The list of results.</returns>
        private static IEnumerable<AggregateResult> PopulateGenericAggregate(
            SelectQuery query,
            DatabaseType databaseType,
            IEnumerable<ShardIdentifier> shardIds,
            TimeSpan? timeout,
            IExecutor executor)
        {
            if (query.PathQuery != null)
            {
                throw new NotSupportedException("Projection queries are not supported.");
            }

            List<AggregateResult> results = new List<AggregateResult>();
            StoredProcedureMultiple spm = executor.RunMultiple(query, databaseType, shardIds, timeout);

            int indexer = 0;
            DataTable table = spm.ReadRawData(0);
            foreach (DataRow row in table.Rows)
            {
                AggregateResult result = new AggregateResult();
                foreach (DataColumn column in table.Columns)
                {
                    QueryColumn qc = query.Columns.FirstOrDefault(p => p.Alias == column.ColumnName);
                    if (qc != null && qc.Expression != null && qc.IsKeyColumn == true && qc.DefaultValue != null)
                    {
                        // Skip default keys since they are not needed in this context.
                    }
                    else if (column.ColumnName[0] != '$')
                    {
                        result.DynamicProperties[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                    }
                }

                result.AggregateKey = indexer++;
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Gets the corresponding path value for a given column.
        /// </summary>
        /// <param name="column">The column to inspect.</param>
        /// <returns>The path value.</returns>
        private static string GetPathValue(QueryColumn column)
        {
            if (column.Source != null)
            {
                return column.Source.Path;
            }
            else if (column.AggregateColumnReference != null)
            {
                List<PropertyNameType> properties = column.AggregateColumnReference.Predicatable.LocatePropertyNames();
                IEnumerable<string> prefixes = properties.Select(p => p.Prefix ?? string.Empty).Distinct();
                if (prefixes.Count() == 1)
                {
                    return prefixes.Single();
                }
            }

            throw new InvalidDataFilterException("Invalid Column data discovered.");
        }
    }
}
