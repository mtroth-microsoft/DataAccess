// -----------------------------------------------------------------------
// <copyright file="DeleteQueryBuilder.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OdataExpressionModel;

    /// <summary>
    /// Delete query builder class.
    /// </summary>
    /// <typeparam name="T">The type of the entity to delete.</typeparam>
    internal sealed class DeleteQueryBuilder<T>
    {
        /// <summary>
        /// Initializes a new instance of the DeleteQueryBuilder class.
        /// </summary>
        /// <param name="request"></param>
        public DeleteQueryBuilder(WriteRequest request)
        {
            this.DeleteQueries = CreateDelete(request);
        }

        /// <summary>
        /// Gets or sets the list of delete queries.
        /// </summary>
        public List<DeleteQuery> DeleteQueries
        {
            get;
            private set;
        }

        /// <summary>
        /// Create the delete query.
        /// </summary>
        /// <param name="request">The request to inspect.</param>
        /// <returns>The resulting delete queries.</returns>
        private static List<DeleteQuery> CreateDelete(WriteRequest request)
        {
            IQueryBuilder builder = TypeCache.ReflectCorrectBuilder(typeof(T), new QueryBuilderSettings());
            List<DeleteQuery> deletes = CreateDeletes(typeof(T), builder.Query.Source, request);
            deletes.Reverse();

            return deletes;
        }

        /// <summary>
        /// Creates the filter for performing the delete.
        /// </summary>
        /// <param name="request">The write request being executed.</param>
        /// <returns>The correlative filter type.</returns>
        private static FilterType CreateFilter(WriteRequest request)
        {
            FilterType filter = new FilterType();
            List<EqualType> equals = new List<EqualType>();
            foreach (KeyValuePair<string, object> pair in request.Key.EntityKeyValues)
            {
                EqualType equal = new EqualType();
                equal.Subject = new PropertyNameType() { Value = pair.Key };
                equal.Predicate = pair.Value;
                equals.Add(equal);
            }

            if (equals.Count == 1)
            {
                filter.Item = equals.Single();
            }
            else
            {
                AndType and = new AndType();
                and.Items.AddRange(equals);
                filter.Item = and;
            }

            return filter;
        }

        /// <summary>
        /// Create delete queries for the given type.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="source">The query source to inspect.</param>
        /// <param name="request">The writer request.</param>
        /// <returns>The list of delete queries.</returns>
        private static List<DeleteQuery> CreateDeletes(
            Type typeToWrite, 
            QuerySource source,
            WriteRequest request)
        {
            UnionQuery union = source as UnionQuery;
            SelectQuery select = source as SelectQuery;
            QueryTable table = source as QueryTable;
            if (union != null)
            {
                return CreateDeletesForBaseType(typeToWrite, union, request);
            }
            else if (select != null)
            {
                return CreateDeletesForTablePerType(typeToWrite, select, request);
            }
            else if (table != null)
            {
                return CreateDeletesForTable(typeToWrite, table, request);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Create delete queries for a table.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="table">The table to inspect</param>
        /// <param name="request">The writer request.</param>
        /// <returns>The list of delete queries.</returns>
        private static List<DeleteQuery> CreateDeletesForTable(
            Type typeToWrite, 
            QueryTable table, 
            WriteRequest request)
        {
            table.Hint = HintType.None;
            List<DeleteQuery> deletes = new List<DeleteQuery>();
            DeleteQuery dq = new DeleteQuery();
            dq.Filter = CreateFilter(request);
            dq.Target = table;
            deletes.Add(dq);

            return deletes;
        }

        /// <summary>
        /// Create delete queries for table per type data.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="select">The query to inspect.</param>
        /// <param name="request">The writer request.</param>
        /// <returns>The list of delete queries.</returns>
        private static List<DeleteQuery> CreateDeletesForTablePerType(
            Type typeToWrite, 
            SelectQuery select,
            WriteRequest request)
        {
            List<DeleteQuery> deletes = new List<DeleteQuery>();
            for (int i = select.Joins.Count - 1; i >= 0; i--)
            {
                QueryJoin join = select.Joins[i];
                List<DeleteQuery> nested = CreateDeletes(join.Target.Type, join.Target, request);
                foreach (DeleteQuery dq in nested)
                {
                    if (deletes.Any(p => p.Target.Type == dq.Target.Type) == false)
                    {
                        deletes.Add(dq);
                    }
                }
            }

            deletes.AddRange(CreateDeletes(typeToWrite, select.Source, request));

            return deletes;
        }

        /// <summary>
        /// Create delete queries for base type data.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="select">The query to inspect.</param>
        /// <param name="request">The writer request.</param>
        /// <returns>The list of delete queries.</returns>
        private static List<DeleteQuery> CreateDeletesForBaseType(
            Type typeToWrite, 
            UnionQuery union, 
            WriteRequest request)
        {
            List<DeleteQuery> deletes = new List<DeleteQuery>();
            foreach (SelectQuery query in union.Queries)
            {
                List<DeleteQuery> nested = CreateDeletes(query.Type, query, request);
                foreach (DeleteQuery dq in nested)
                {
                    if (deletes.Any(p => p.Target.Type == dq.Target.Type) == false)
                    {
                        deletes.Add(dq);
                    }
                }
            }

            return deletes;
        }
    }
}
