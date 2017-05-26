// -----------------------------------------------------------------------
// <copyright file="IProjectionHelper.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Interface for projection helpers to implement.
    /// </summary>
    internal interface IProjectionHelper
    {
        /// <summary>
        /// Load data from the stored procedure.
        /// </summary>
        /// <typeparam name="T">The type of the seed nodes.</typeparam>
        /// <param name="projection">The projection to execute.</param>
        /// <param name="spm">The stored procedure with data already populated.</param>
        /// <returns>The populated seed data.</returns>
        IQueryable<T> LoadDataFromProcedure<T>(
            SelectQuery projection,
            StoredProcedureMultiple spm);

        /// <summary>
        /// Fix the query to work with distinct and orderby.
        /// </summary>
        /// <param name="query">The query to fix.</param>
        void FixUpOrderBy(SelectQuery query);

        /// <summary>
        /// Locathe projection columns for a given node.
        /// </summary>
        /// <param name="query">The correspnding query.</param>
        /// <param name="node">The node to inspect.</param>
        /// <returns>The correlative columns for the node.</returns>
        IEnumerable<QueryColumn> LocateColumns(SelectQuery query, CompositeNode node);

        /// <summary>
        /// Align the given columns to the path table.
        /// </summary>
        /// <param name="columns">The columns to put into the table.</param>
        /// <returns>The aligned query table.</returns>
        SelectQuery CreatePathQuery(List<QueryColumn> columns);

        /// <summary>
        /// Copy the provided columns into a path subselect for the given projection keys.
        /// </summary>
        /// <param name="projection">The projection keys to include.</param>
        /// <param name="seedKeys">The seed keys to include.</param>
        /// <returns></returns>
        SelectQuery AlignColumnsToPath(
            IEnumerable<QueryColumn> projection,
            IEnumerable<QueryColumn> seedKeys);

        /// <summary>
        /// Configure the query joins so that they work with the Path Query.
        /// </summary>
        /// <param name="query">The root query to configure.</param>
        void ReConfigureJoins(SelectQuery query);

        /// <summary>
        /// Assign the path source to each of the relevant subselects.
        /// </summary>
        /// <param name="query">The seed query to fix.</param>
        void FixSubSelects(SelectQuery query);

        /// <summary>
        /// Locate the source, given a query and a composite node.
        /// </summary>
        /// <param name="query">The root query to inspect.</param>
        /// <param name="node">The node to match.</param>
        /// <returns>The correlative query source.</returns>
        QuerySource LocateSource(SelectQuery query, CompositeNode node);
    }
}