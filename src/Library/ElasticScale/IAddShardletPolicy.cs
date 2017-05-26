// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;

    /// <summary>
    /// The internal interface for defining shardlet additions to the system.
    /// </summary>
    public interface IAddShardletPolicy
    {
        /// <summary>
        /// Sorts a list of shardlets into a the shards where they should be added, according to policy.
        /// </summary>
        /// <param name="databaseType">The type of the store.</param>
        /// <param name="shardlets">The list of shardlets.</param>
        /// <returns>The map of shards to shardlets.</returns>
        Dictionary<ShardIdentifier, List<int>> Distribute(
            DatabaseType databaseType,
            IEnumerable<int> shardlets);
    }
}
