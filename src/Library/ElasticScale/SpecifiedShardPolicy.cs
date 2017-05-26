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
    /// The shardlet add policy that puts new shardlets into the specified shard.
    /// </summary>
    public sealed class SpecifiedShardPolicy : IAddShardletPolicy
    {
        /// <summary>
        /// Gets or Sets the ShardIdentifier
        /// </summary>
        internal ShardIdentifier ShardIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Initialize an instance of the <see cref="SpecifiedShardPolicy"/> class.
        /// </summary>
        /// <param name="shardIdentifier">The shard identifier.</param>
        public SpecifiedShardPolicy(ShardIdentifier shardIdentifier)
        {
            this.ShardIdentifier = shardIdentifier;
        }

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
            if (shardlets == null || shardlets.Count() == 0)
            {
                map[this.ShardIdentifier] = new List<int>();
            }
            else
            {
                map[this.ShardIdentifier] = shardlets.ToList<int>();
            }

            return map;
        }
    }
}
