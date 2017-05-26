// -----------------------------------------------------------------------
// <copyright file="BulkWriterSettings.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;

    public sealed class BulkWriterSettings
    {
        /// <summary>
        /// Gets or sets the store to target for the bulk write.
        /// </summary>
        public DatabaseType Store
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the list of shard identifiers for the bulk write.
        /// </summary>
        public IEnumerable<ShardIdentifier> ShardIds
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to perform a concurrency check.
        /// </summary>
        public bool DoConcurrencyCheck
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to leave the reader open.
        /// </summary>
        public bool LeaveReaderOpen
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to only update changed rows.
        /// </summary>
        public bool OnlyUpdateChanged
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the changed by user.
        /// </summary>
        public string ChangedByUser
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the transaction id.
        /// </summary>
        public Guid TransactionId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the changed time.
        /// </summary>
        public DateTimeOffset ChangedTime
        {
            get;
            set;
        }
    }
}
