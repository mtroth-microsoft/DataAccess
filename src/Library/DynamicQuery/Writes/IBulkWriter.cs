// -----------------------------------------------------------------------
// <copyright file="IBulkWriter.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Data;
    using Config = Configuration;

    /// <summary>
    /// Interface for bulk write operations.
    /// </summary>
    internal interface IBulkWriter
    {
        /// <summary>
        /// Load the writer data via a single connection.
        /// </summary>
        /// <param name="dimension">The dimension being written.</param>
        /// <param name="reader">The reader containing the feed's data.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Records written via bulk load.</returns>
        long Load(
            Config.Dimension dimension,
            IDataReader reader,
            BulkWriterSettings settings);

        /// <summary>
        /// Load the writer data via a single connection.
        /// </summary>
        /// <param name="feed">The feed being written.</param>
        /// <param name="reader">The reader containing the feed's data.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Records written via bulk load.</returns>
        long Load(
            Config.Feed feed,
            IDataReader reader,
            BulkWriterSettings settings);

        /// <summary>
        /// Load the writer reader data via a single connection.
        /// </summary>
        /// <param name="writerReader">The writer reader with data and set to the correct result.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Records written via bulk load.</returns>
        long LoadAndMerge(
            WriterReader writerReader,
            BulkWriterSettings settings);

        /// <summary>
        /// Load and merge all the data in a set of readers in a single transaction.
        /// </summary>
        /// <param name="executes">The set of operations to run.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>The list of results for each execute.</returns>
        List<object> LoadAndMergeInTransaction(
            List<object> executes,
            BulkWriterSettings settings);
    }
}
