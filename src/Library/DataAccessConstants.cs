// -----------------------------------------------------------------------
// <copyright company="Lensgrinder, Ltd." file="DataAccessConstants.cs">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The primary service provider.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Constant values for use by DataAccess.
    /// </summary>
    internal static class DataAccessConstants
    {
        /// <summary>
        /// The name of the scenario portal database.
        /// </summary>
        public const string ScenarioPortalCatalog = "ScenarioPortalDB";

        /// <summary>
        /// The name of the shard map manager database.
        /// </summary>
        public const string ShardMapManagerCatalog = "ShardMapManagerDB";

        /// <summary>
        /// The name of the reporting database shard map in elastic scale.
        /// </summary>
        public const string ShardMapName = "ReportingShardMap";

        /// <summary>
        /// The number of the magic shardlet that indicates the default shard.
        /// </summary>
        public const int MagicShardlet = 0;
    }
}
