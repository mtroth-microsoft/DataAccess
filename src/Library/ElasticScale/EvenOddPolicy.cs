// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;

    public sealed class EvenOddPolicy : IAddShardletPolicy
    {
        /// <summary>
        /// Sorts a list of shardlets into a the shards where they should be added, according to policy.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardLets">The list of shardlets.</param>
        /// <returns>The map of shards to shardlets.</returns>
        public Dictionary<ShardIdentifier, List<int>> Distribute(DatabaseType databaseType, IEnumerable<int> shardLets)
        {
            IEnumerable<ShardIdentifier> ids = ShardMapManagement.Instance.GetShards(databaseType);

            Dictionary<ShardIdentifier, List<int>> map = new Dictionary<ShardIdentifier, List<int>>();
            ShardIdentifier even = ids.Single(p => IsOdd(p.DataSource) == false);
            ShardIdentifier odd = ids.Single(p => IsOdd(p.DataSource) == true);
            map.Add(even, new List<int>());
            map.Add(odd, new List<int>());

            foreach (int shardlet in shardLets)
            {
                if (shardlet % 2 == 0)
                {
                    map[even].Add(shardlet);
                }
                else
                {
                    map[odd].Add(shardlet);
                }
            }

            return map;
        }

        /// <summary>
        /// Determines if the datasource is odd or not.
        /// </summary>
        /// <param name="dataSource">The datasource name.</param>
        /// <returns>True if it is odd, otherwise false.</returns>
        private static bool IsOdd(string dataSource)
        {
            int pos = dataSource.IndexOf('.');
            if (pos > 0)
            {
                dataSource = dataSource.Substring(0, pos);
            }

            char last = dataSource.Last();
            return (int.Parse(last.ToString()) % 2) != 0;
        }
    }
}
