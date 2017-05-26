// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Represents a telemetry event occuring within a store.
    /// </summary>
    public class StoreTelemetryEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreTelemetryEvent"/> class for multi target events.
        /// </summary>
        /// <param name="api">The API name of the executed action.</param>
        public StoreTelemetryEvent(string api)
        {
            this.IsMultiTarget = true;
            this.Api = api;
            this.DataSource = "MultiTarget";
            this.Catalog = "Unknown";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreTelemetryEvent"/> class.
        /// </summary>
        /// <param name="api">The API name of the executed action.</param>
        /// <param name="dataSource">The machine/data source that this action was executed upon.</param>
        /// <param name="catalog">The database/catalog that this action was executed upon.</param>
        public StoreTelemetryEvent(string api, string dataSource, string catalog)
        {
            this.IsMultiTarget = false;
            this.Api = api;
            this.DataSource = dataSource;
            this.Catalog = catalog;
            this.TargetCount = 1;
        }

        /// <summary>
        /// Gets a value indicating whether this event represents a query that was executed across more than one shard,
        /// making its roleInstance unknown.
        /// </summary>
        /// <value>
        /// <c>true</c> if this event is representing part of a federated query; otherwise, <c>false</c>.
        /// </value>
        public bool IsMultiTarget
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of targets impacted by this event.
        /// </summary>
        /// <value>
        /// The target count.
        /// </value>
        public int TargetCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the latency of the operation in milliseconds.
        /// </summary>
        /// <value>
        /// The latency in milliseconds.
        /// </value>
        public long LatencyMs
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the executing API name.
        /// </summary>
        /// <value>
        /// The API.
        /// </value>
        public string Api
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the datasource, which represents the machine name, or the name of the entity executing the action.
        /// </summary>
        /// <value>
        /// The role instance.
        /// </value>
        public string DataSource
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the catalog used in the action (in most cases this would be the database name).
        /// </summary>
        /// <value>
        /// The role instance.
        /// </value>
        public string Catalog
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the event.
        /// </summary>
        /// <value>
        /// The size of the event.
        /// </value>
        public long Size
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an associated exception.
        /// </summary>
        /// <value>
        /// The associated exception.
        /// </value>
        public Exception Exception
        {
            get;
            set;
        }
    }
}
