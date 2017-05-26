// -----------------------------------------------------------------------
// <copyright file="TSqlProjectionHelper.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// Projection helper class.
    /// </summary>
    internal class TSqlProjectionHelper : IProjectionHelper
    {
        /// <summary>
        /// Load data from the stored procedure.
        /// </summary>
        /// <typeparam name="T">The type of the seed nodes.</typeparam>
        /// <param name="projection">The projection to execute.</param>
        /// <param name="spm">The stored procedure with data already populated.</param>
        /// <returns>The populated seed data.</returns>
        public IQueryable<T> LoadDataFromProcedure<T>(
            SelectQuery projection,
            StoredProcedureMultiple spm)
        {
            return ProcessResults<T>(projection, spm);
        }

        /// <summary>
        /// Fix the query to work with distinct and orderby.
        /// </summary>
        /// <param name="query">The query to fix.</param>
        public void FixUpOrderBy(SelectQuery query)
        {
            if (query.Distinct == true && query.OrderBy.Count > 0)
            {
                IEnumerable<QueryColumn> columns = query.OrderBy.Select(p => p.Column);
                foreach (QueryColumn column in columns)
                {
                    if (query.Columns.Any(p => p.Alias == column.Alias) == false)
                    {
                        query.Columns.Add(column);
                    }
                }
            }
        }

        /// <summary>
        /// Locathe projection columns for a given node.
        /// </summary>
        /// <param name="query">The correspnding query.</param>
        /// <param name="node">The node to inspect.</param>
        /// <returns>The correlative columns for the node.</returns>
        public IEnumerable<QueryColumn> LocateColumns(SelectQuery query, CompositeNode node)
        {
            List<QueryColumn> columns = new List<QueryColumn>();
            QueryColumn id = new QueryColumn()
            {
                Expression = string.Concat("'", node.ComponentId, "'"),
                Alias = node.ComponentId,
                ElementType = typeof(string)
            };
            columns.Add(id);

            int index = 1;
            string template = string.Concat(node.ComponentId, "Key");
            IEnumerable<QueryColumn> pkeys = query.AllColumns.Where(p => p.IsKeyColumn == true);
            foreach (QueryColumn key in pkeys)
            {
                string alias = string.Concat(template, index.ToString());
                QueryColumn copy = new QueryColumn();
                copy.IsKeyColumn = key.IsKeyColumn;
                copy.Name = key.Name;
                copy.Alias = alias;
                copy.Source = key.Source;
                copy.ElementType = key.ElementType;
                index++;
                columns.Add(copy);
            }

            return columns;
        }

        /// <summary>
        /// Align the given columns to the path table.
        /// </summary>
        /// <param name="columns">The columns to put into the table.</param>
        /// <returns>The aligned query table.</returns>
        public SelectQuery CreatePathQuery(List<QueryColumn> columns)
        {
            SelectQuery pathQuery = new SelectQuery();
            QueryTable pathTable = new QueryTable();
            pathTable.Hint = HintType.None;
            pathTable.Name = "@Path";
            pathTable.Alias = "Path";
            pathQuery.Source = pathTable;

            string alias = null;
            List<QueryColumn> keys = new List<QueryColumn>();
            foreach (QueryColumn column in columns)
            {
                QueryColumn copy = new QueryColumn();
                copy.Source = pathTable;
                copy.Name = column.Alias;
                copy.ElementType = column.ElementType;
                copy.IsKeyColumn = column.IsKeyColumn;
                if (string.IsNullOrEmpty(column.Name) == true)
                {
                    if (keys.Count > 0)
                    {
                        pathQuery.Columns.Add(CreateCompressedColumn(alias, keys, pathTable));
                        keys = new List<QueryColumn>();
                    }

                    alias = string.Concat(column.Alias, "Key");
                }
                else
                {
                    keys.Add(column);
                }

                pathQuery.Columns.Add(copy);
            }

            pathQuery.Columns.Add(CreateCompressedColumn(alias, keys, pathTable));

            return pathQuery;
        }

        /// <summary>
        /// Copy the provided columns into a path subselect for the given projection keys.
        /// </summary>
        /// <param name="projection">The projection keys to include.</param>
        /// <param name="seedKeys">The seed keys to include.</param>
        /// <returns></returns>
        public SelectQuery AlignColumnsToPath(
            IEnumerable<QueryColumn> projection,
            IEnumerable<QueryColumn> seedKeys)
        {
            QueryColumn key, id;
            FilterType filter = SplitColumns(projection, out key, out id);
            QueryColumn seed = CreateCompressedColumn("SeedId", seedKeys, null);

            SelectQuery subselect = new SelectQuery();
            subselect.Columns.Add(seed);
            subselect.Columns.Add(key);
            subselect.Columns.Add(id);
            subselect.Filter = filter;

            return subselect;
        }

        /// <summary>
        /// Configure the query joins so that they work with the Path Query.
        /// </summary>
        /// <param name="query">The root query to configure.</param>
        public void ReConfigureJoins(SelectQuery query)
        {
            List<SelectQuery> queries = new List<SelectQuery>();
            queries.Add(query);
            queries.AddRange(query.Secondaries);

            List<QueryColumn> keys = new List<QueryColumn>();
            SelectQuery test = null;
            int counter = 0;
            foreach (QueryColumn column in query.PathQuery.Columns)
            {
                SelectQuery secondary = queries.Where(p => p.RootNode.ComponentId == column.Name).SingleOrDefault();
                if (secondary != null)
                {
                    if (keys.Count > 0)
                    {
                        test.Columns.Add(CreateCompressedColumn("$Key", keys, test.Source));
                    }

                    test = secondary;
                    counter = 0;
                    keys = new List<QueryColumn>();
                }

                if (test != null && secondary == null && string.IsNullOrEmpty(column.Name) == false)
                {
                    test.Distinct = true;
                    FixUpOrderBy(test);
                    QueryJoin join = test.Joins.SingleOrDefault(p => p.Target.Alias == query.PathQuery.Source.Alias);
                    if (join == null)
                    {
                        join = new QueryJoin();
                        join.Source = test.Source;
                        join.Target = query.PathQuery.Source;
                        test.Joins.Add(join);
                    }

                    QueryJoin skipJoin = test.Joins.SingleOrDefault(p => p.Target.Alias == "Skip");
                    if (skipJoin != null)
                    {
                        SelectQuery skipQuery = skipJoin.Target as SelectQuery;
                        if (skipQuery != null)
                        {
                            skipQuery.Distinct = true;
                            FixUpOrderBy(skipQuery);
                        }
                    }

                    QueryColumn tc = test.AllColumns.Where(p => p.IsKeyColumn == true).ToList()[counter++];
                    join.Statements.Add(new Tuple<QueryColumn, QueryColumn>(tc, column));
                    keys.Add(tc);
                }
            }

            test.Columns.Add(CreateCompressedColumn("$Key", keys, test.Source));
        }

        /// <summary>
        /// Assign the path source to each of the relevant subselects.
        /// </summary>
        /// <param name="query">The seed query to fix.</param>
        public void FixSubSelects(SelectQuery query)
        {
            List<SelectQuery> queries = new List<SelectQuery>();
            queries.Add(query.PathSubSelect);
            queries.AddRange(query.Secondaries.Select(p => p.PathSubSelect));

            foreach (SelectQuery subselect in queries)
            {
                subselect.Source = query.PathQuery.Source;
                foreach (QueryColumn qc in subselect.Columns)
                {
                    qc.Source = query.PathQuery.Source;
                }
            }
        }

        /// <summary>
        /// Locate the source, given a query and a composite node.
        /// </summary>
        /// <param name="query">The root query to inspect.</param>
        /// <param name="node">The node to match.</param>
        /// <returns>The correlative query source.</returns>
        public QuerySource LocateSource(SelectQuery query, CompositeNode node)
        {
            QuerySource source = null;
            string fullPath = node.GetFullPath();
            foreach (QueryJoin join in query.Joins)
            {
                if (join.Source.Path.Equals(fullPath) == true)
                {
                    source = join.Source;
                }
                else if (join.Target.Path.Equals(fullPath) == true)
                {
                    source = join.Target;
                }

                if (source != null)
                {
                    break;
                }
            }

            return source;
        }

        /// <summary>
        /// Serialize the projection version of the query, if applicable.
        /// </summary>
        /// <param name="query">The projection query.</param>
        /// <returns>The serialized string.</returns>
        internal static string Serialize(SelectQuery query)
        {
            if (query.PathQuery == null)
            {
                throw new InvalidOperationException("Query is not a valid projection.");
            }

            TSqlQuerySerializer serializer = new TSqlQuerySerializer();
            SqlFormatter formatter = new SqlFormatter();
            ParameterContext context = new ParameterContext();

            StringBuilder builder = new StringBuilder("DECLARE @Path TABLE (\nId bigint IDENTITY(1,1)");
            string separator = ",";
            foreach (QueryColumn qc in query.InsertQuery.Columns)
            {
                builder.Append(separator);
                builder.AppendFormat("\n[{0}] {1}", qc.Alias, SwitchToSqlTypeName(qc.ElementType.Name));
                separator = ",";
            }

            builder.AppendLine(");");
            builder.AppendLine("INSERT @Path");
            builder.Append(serializer.SerializeSource(query.InsertQuery, false, context, formatter));
            builder.AppendLine("\n");
            builder.AppendLine("SELECT DISTINCT * FROM (");
            builder.Append(serializer.SerializeSource(query.PathSubSelect, false, context, formatter));
            foreach (SelectQuery secondary in query.Secondaries)
            {
                builder.AppendLine("\nUNION ALL");
                builder.Append(serializer.SerializeSource(secondary.PathSubSelect, false, context, formatter));
            }

            builder.AppendLine(") AS PC\n");
            builder.Append(serializer.SerializeSource(query.PathQuery, false, context, formatter));
            builder.AppendLine("\n");
            builder.Append(serializer.SerializeSource(query, false, context, formatter));
            foreach (SelectQuery secondary in query.Secondaries)
            {
                builder.AppendLine("\n");
                builder.Append(serializer.SerializeSource(secondary, false, context, formatter));
            }

            foreach (Parameter item in context.ReadAll())
            {
                if (query.Parameters.Any(p => p.ParameterName == item.ParameterName) == false)
                {
                    query.Parameters.Add(item);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Split and Compress the Path columns.
        /// </summary>
        /// <param name="list">The original list of path columns.</param>
        /// <param name="key">The key to define.</param>
        /// <param name="id">The id to define.</param>
        /// <returns>The filter type to assign.</returns>
        private static FilterType SplitColumns(
            IEnumerable<QueryColumn> list,
            out QueryColumn key,
            out QueryColumn id)
        {
            List<QueryColumn> keys = new List<QueryColumn>();
            FilterType filter = new FilterType();
            AndType and = new AndType();
            filter.Item = and;
            id = new QueryColumn();

            int indexer = 0;
            foreach (QueryColumn qc in list)
            {
                if (indexer > 0)
                {
                    keys.Add(qc);
                    and.Items.Add(new NotEqualType() { Subject = new PropertyNameType { Value = qc.Alias, Alias = "Path" }, Predicate = new NullType() });
                }
                else
                {
                    id.Name = qc.Alias;
                    id.ElementType = qc.ElementType;
                    id.Alias = "ComponentId";
                    indexer++;
                }
            }

            key = CreateCompressedColumn("ChildId", keys, null);

            return filter;
        }

        /// <summary>
        /// Compress the keys of a projection member into a single select column from the path query.
        /// </summary>
        /// <param name="useNameAndAlias">True to use the name and source alias, otherwise false.</param>
        /// <param name="keys">The keys to compress.</param>
        /// <returns>The compressed column string.</returns>
        private static string CompressKeys(bool useNameAndAlias, IEnumerable<QueryColumn> keys)
        {
            StringBuilder builder = new StringBuilder();
            string separator = string.Empty;
            foreach (QueryColumn qc in keys)
            {
                builder.Append(separator);
                builder.Append("CAST(");
                if (useNameAndAlias == false)
                {
                    builder.AppendFormat("[{0}]", qc.Alias);
                }
                else
                {
                    builder.AppendFormat("[{1}].[{0}]", qc.Name, qc.Source.Alias);
                }

                builder.Append(" AS nvarchar(max))");
                separator = " + '|' + ";
            }

            return builder.ToString();
        }

        /// <summary>
        /// Create a compressed column.
        /// </summary>
        /// <param name="alias">The alias to assign.</param>
        /// <param name="keys">The columns to compress.</param>
        /// <param name="source">The source of the column.</param>
        /// <returns>The compressed column.</returns>
        private static QueryColumn CreateCompressedColumn(
            string alias,
            IEnumerable<QueryColumn> keys,
            QuerySource source)
        {
            QueryColumn column = new QueryColumn();
            column.Expression = CompressKeys(alias[0] == '$', keys);
            column.Source = source;
            column.ElementType = typeof(string);
            column.Alias = alias;

            return column;
        }

        /// <summary>
        /// Convert the clr type name to a sql type name.
        /// </summary>
        /// <param name="name">The name of the clr type.</param>
        /// <returns>The sql type name.</returns>
        private static string SwitchToSqlTypeName(string name)
        {
            switch (name)
            {
                case "Int32": return "int";
                case "Int64": return "bigint";
                case "String": return "nvarchar(max)";
                case "Guid": return "uniqueidentifier";
                case "DateTime": return "datetime";
                case "DateTimeOffset": return "datetimeoffset";
                default: throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Process the results of the projection execution.
        /// </summary>
        /// <typeparam name="T">The type of the seed nodes.</typeparam>
        /// <param name="projection">The projection to execute.</param>
        /// <param name="spm">The stored procedure with loaded results.</param>
        /// <returns>The structures result set.</returns>
        private static IQueryable<T> ProcessResults<T>(SelectQuery projection, StoredProcedureMultiple spm)
        {
            HashSet<Result> results = new HashSet<Result>();
            Dictionary<string, CompositeNode> nodeMap = new Dictionary<string, CompositeNode>();
            Dictionary<string, Dictionary<string, DataRow>> data = new Dictionary<string, Dictionary<string, DataRow>>();

            int indexer = 0;
            DataTable edges = spm.ReadRawData(indexer++);
            DataTable paths = spm.ReadRawData(indexer++);
            Dictionary<string, DataRow> cluster = new Dictionary<string, DataRow>();
            List<SelectQuery> queries = new List<SelectQuery>();
            queries.Add(projection);
            queries.AddRange(projection.Secondaries);

            foreach (SelectQuery q in queries)
            {
                cluster = new Dictionary<string, DataRow>();
                DataTable table = spm.ReadRawData(indexer++);
                foreach (DataRow row in table.Rows)
                {
                    cluster[row["$Key"].ToString()] = row;
                }

                data[q.RootNode.ComponentId] = cluster;
                nodeMap[q.RootNode.ComponentId] = q.RootNode;
            }

            // identify all the locations in the graph.
            Dictionary<KeyValuePair<string, string>, Result> members =
                new Dictionary<KeyValuePair<string, string>, Result>();
            foreach (DataRow row in edges.Rows)
            {
                string componentid = row[2].ToString();
                CompositeNode current = nodeMap[componentid];

                string id = row[1].ToString();
                string seedid = row[0].ToString();
                KeyValuePair<string, string> keypair = new KeyValuePair<string, string>(id, current.ComponentId);

                if (members.ContainsKey(keypair) == false &&
                    data[componentid].ContainsKey(id) == true)
                {
                    members.Add(keypair, new Result());
                    members[keypair].ComponentId = current.ComponentId;
                    members[keypair].Path = current.Path;
                    members[keypair].Member = TypeCache.ToObject(current.ElementType, data[componentid][id]);
                }
            }

            // Find the relevant columns in the paths result set.
            List<DataColumn> columnSet = new List<DataColumn>();
            foreach (DataColumn dc in paths.Columns)
            {
                if (nodeMap.ContainsKey(dc.ColumnName) == true ||
                    dc.ColumnName.EndsWith("Key") == true)
                {
                    columnSet.Add(dc);
                }
            }

            // populate the result set, making connections between endpoints.
            foreach (DataRow row in paths.Rows)
            {
                string seedid = row[columnSet[1]].ToString();
                KeyValuePair<string, string> seedpair = new KeyValuePair<string, string>(seedid, "0");
                Result seed = members[seedpair];
                if (results.Contains(seed) == false)
                {
                    results.Add(seed);
                }

                for (int i = 2; i < columnSet.Count; i = i + 2)
                {
                    string componentid = row[columnSet[i]].ToString();
                    if (row.IsNull(columnSet[i + 1]) == true)
                    {
                        continue;
                    }

                    string id = row[columnSet[i + 1]].ToString();
                    CompositeNode current = nodeMap[componentid];
                    KeyValuePair<string, string> keypair = new KeyValuePair<string, string>(id, componentid);
                    if (members.ContainsKey(keypair) == false)
                    {
                        continue;
                    }

                    Result node = members[keypair];
                    string parentKey = row[current.Parent.ComponentId + "Key"].ToString();
                    KeyValuePair<string, string> pkey = new KeyValuePair<string, string>(parentKey, current.Parent.ComponentId);
                    if (members.ContainsKey(pkey) == false)
                    {
                        continue;
                    }

                    Result parent = members[pkey];

                    // add the member node to the right container.
                    if (parent.Nodes.Contains(node) == false)
                    {
                        parent.Nodes.Add(node);
                    }
                }
            }

            Stack<Result> stack = new Stack<Result>(results);
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

            return results.Select(p => p.Member).Cast<T>().AsQueryable();
        }
    }
}
