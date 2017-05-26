// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The shardlet add policy that puts new shardlets into the least populated shard.
    /// </summary>
    internal sealed class LeastPopulatedPolicy : IAddShardletPolicy
    {
        /// <summary>
        /// Sorts a list of shardlets into the shards where they should be added, according to policy.
        /// </summary>
        /// <param name="databaseType">The type of the store.</param>
        /// <param name="shardlets">The list of shardlets.</param>
        /// <returns>The map of shards to shardlets.</returns>
        public Dictionary<ShardIdentifier, List<int>> Distribute(
            DatabaseType databaseType,
            IEnumerable<int> shardlets)
        {
            Dictionary<ShardIdentifier, List<int>> map = new Dictionary<ShardIdentifier, List<int>>();

            Dictionary<ShardIdentifier, int> counts = new Dictionary<ShardIdentifier, int>();
            IEnumerable<ShardIdentifier> shardIds = ShardMapManagement.Instance.GetShards(databaseType);
            foreach (ShardIdentifier shardId in shardIds)
            {
                IEnumerable<int> ids = ShardMapManagement.Instance.GetShardlets(databaseType, shardId);
                counts[shardId] = ids.Count();
                map[shardId] = new List<int>();
            }

            foreach (int id in shardlets)
            {
                ShardIdentifier min = FindMin(counts);
                map[min].Add(id);
                counts[min]++;
            }

            return map;
        }

        /// <summary>
        /// Helper to find the counts member with the fewest shardlets.
        /// </summary>
        /// <param name="counts"></param>
        /// <returns></returns>
        private static ShardIdentifier FindMin(Dictionary<ShardIdentifier, int> counts)
        {
            int min = counts.Min(p => p.Value);

            return counts.Where(p => p.Value == min).First().Key;
        }
    }
}
