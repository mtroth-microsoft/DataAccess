// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

    /// <summary>
    /// This class provides APIs to interact with the Shard Map Manager.
    /// </summary>
    public class ShardMapManagement : CacheBase<ShardIdentifier, int, string>
    {
        /// <summary>
        /// The shard map manager
        /// </summary>
        private static ShardMapManager shardMapManager = CreateShardMapManager();

        /// <summary>
        /// The instance of this singleton class
        /// </summary>
        private static ShardMapManagement instance = new ShardMapManagement();

        /// <summary>
        /// The add shardlets lock
        /// </summary>
        private object addShardletsLock = new object();

        /// <summary>
        /// The complete list of shardlets.
        /// </summary>
        private HashSet<Shardlet<int>> shardlets;

        /// <summary>
        /// Magic Shardlet mapping
        /// </summary>
        private ConcurrentDictionary<int, ShardIdentifier> magicShardletMapping = new ConcurrentDictionary<int, ShardIdentifier>();

        /// <summary>
        /// Prevents a default instance of the <see cref="ShardMapManagement"/> class from being created.
        /// </summary>
        private ShardMapManagement()
        {
        }

        /// <summary>
        /// Gets the instance of ShardMapManagement.
        /// </summary>
        /// <value>
        /// The instance of ShardMapManagement.
        /// </value>
        public static ShardMapManagement Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Get the default shard.
        /// </summary>
        /// <returns>The default shard's id.</returns>
        public ShardIdentifier GetDefaultShard(DatabaseType databaseType)
        {
            return this.magicShardletMapping.GetOrAdd(
                DataAccessConstants.MagicShardlet, 
                this.GetDefaultShardIdentifier(databaseType));
        }

        /// <summary>
        /// Gets the list of all databases and server combinations that are mapped in the Shard Map Manager.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <returns>The list of shard identifiers.</returns>
        public IEnumerable<ShardIdentifier> GetShards(DatabaseType databaseType)
        {
            return this.SearchCache(p => 1 == 1);
        }

        /// <summary>
        /// Gets the list of shard identifiers for a given list of shardlets.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardlets">The given list of shardlets.</param>
        /// <returns>The list of shard identifiers.</returns>
        public IEnumerable<ShardIdentifier> GetShards(DatabaseType databaseType, IEnumerable<int> shardlets)
        {
            HashSet<ShardIdentifier> results = new HashSet<ShardIdentifier>();
            IEnumerable<ShardIdentifier> shardIds = this.GetShards(databaseType);
            foreach (ShardIdentifier shardId in shardIds)
            {
                if (shardId.Shardlets.Select(p => p.Value).Intersect(shardlets).Count() > 0)
                {
                    results.Add(shardId);
                }
            }

            return results;
        }

        /// <summary>
        /// Add a list of shardlets to the given store.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardlets">The list of shardlet identifiers.</param>
        public void AddShardlets(DatabaseType databaseType, IEnumerable<int> shardlets)
        {
            IAddShardletPolicy policy = ShardletPolicyFactory.Create(databaseType);
            this.AddShardlets(databaseType, shardlets, policy);
        }

        /// <summary>
        /// Add a list of shardlets to the given store.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardLets">The list of shardlet identifiers.</param>
        /// <param name="policy">The policy to use.</param>
        public void AddShardlets(DatabaseType databaseType, IEnumerable<int> shardlets, IAddShardletPolicy policy)
        {
            Dictionary<ShardIdentifier, List<int>> map = policy.Distribute(databaseType, shardlets);
            foreach (ShardIdentifier key in map.Keys)
            {
                this.AddShardlets(databaseType, key, map[key]);
            }
        }

        /// <summary>
        /// This API will get list of all shardlets
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <returns>Collection of Shardlets</returns>
        public IEnumerable<Shardlet<int>> GetShardlets(DatabaseType databaseType)
        {
            return this.shardlets.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the list of shardlet identifiers (keys) associated with databases/servers.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIdentifier">The shard identifier.</param>
        /// <returns>Collection of Shardlets</returns>
        public IEnumerable<int> GetShardlets(DatabaseType databaseType, ShardIdentifier shardIdentifier)
        {
            ShardIdentifier value;
            if (this.TryGetValue(shardIdentifier.ToString(), out value) == true)
            {
                return value.Shardlets.Select(p => p.Value);
            }

            throw new KeyNotFoundException(shardIdentifier.ToString());
        }

        /// <summary>
        /// Helper to translate shard identifiers into shards.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardIdentifiers">The list of shard identifiers.</param>
        /// <returns>The list of shards.</returns>
        internal IEnumerable<Shard> GetShards(DatabaseType databaseType, IEnumerable<ShardIdentifier> shardIdentifiers)
        {
            if (shardIdentifiers == null || shardIdentifiers.Any() == false)
            {
                throw new ArgumentNullException("shardletIdentifiers", "Collection cannot be null / empty");
            }

            HashSet<Shard> shardSet = new HashSet<Shard>();
            ShardIdentifier shard;
            foreach (ShardIdentifier shardIdentifier in shardIdentifiers)
            {
                if (this.TryGetValue(shardIdentifier.ToString(), out shard))
                {
                    shardSet.Add(shard.Shard);
                }
            }

            return shardSet.ToList<Shard>();
        }

        /// <summary>
        /// Removes shardlets (mappings of keys to shards) from the Shard Map Manager. Does not delete associated data.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardLets">The identifiers of the shards to remove.</param>
        /// <returns>A count of the number of shards impacted by this removal</returns>
        internal void RemoveShardlets(DatabaseType databaseType, IEnumerable<int> shardLets)
        {
            ListShardMap<int> listShardMap = this.GetListShardMap(this.GetShardMap(databaseType));
            IReadOnlyList<PointMapping<int>> mappings = listShardMap.GetMappings();

            foreach (int id in shardLets)
            {
                PointMapping<int> pointMapping = mappings.SingleOrDefault(m => m.Value == id);
                if (pointMapping != null)
                {
                    // in order to delete a mapping, it must be first marked offline
                    listShardMap.DeleteMapping(listShardMap.MarkMappingOffline(pointMapping));
                }
            }
        }

        /// <summary>
        /// Add shard to Shard Map Manager
        /// </summary>
        /// <param name="shard">The shard identifier to add.</param>
        internal void AddShard(DatabaseType databaseType, ShardIdentifier shardIdentifier)
        {
            if (shardIdentifier == null)
            {
                throw new ArgumentNullException("shardIdentifier");
            }

            ShardMap shardMap = this.GetShardMap(databaseType);
            ShardLocation shardLocation = new ShardLocation(
                shardIdentifier.DataSource, 
                shardIdentifier.Catalog, 
                SqlProtocol.Tcp, 
                shardIdentifier.Port);

            Shard shard;
            if (shardMap.TryGetShard(shardLocation, out shard) == false)
            {
                shard = shardMap.CreateShard(shardLocation);
                this.RunNow(true);
            }
        }

        /// <summary>
        /// Load the watermark based data.
        /// </summary>
        /// <param name="arg">The chunking argument.</param>
        /// <param name="lastWrittenTime">The watermark.</param>
        protected override void Load(int arg, DateTimeOffset lastWrittenTime)
        {
            this.Load(arg);
        }

        /// <summary>
        /// Load the data.
        /// </summary>
        /// <param name="arg">The chunking argument.</param>
        protected override void Load(int arg)
        {
            // TODO: load all maps instead of just the default.
            ShardMap shardMap = this.GetShardMap(new DefaultStoreType());
            ListShardMap<int> shardMappings = this.GetListShardMap(shardMap);
            IReadOnlyList<PointMapping<int>> mappings = shardMappings.GetMappings();
            HashSet<Shardlet<int>> shardlets = new HashSet<Shardlet<int>>();

            List<ShardIdentifier> shardIds = shardMap.GetShards().Select(
                x => new ShardIdentifier(
                    x.Location.Server, 
                    x.Location.Database, 
                    x.Location.Port)
                {
                    Shard = x
                }).ToList();

            foreach (PointMapping<int> item in mappings)
            {
                ShardIdentifier shardId = shardIds.Single(p => 
                    p.DataSource == item.Shard.Location.Server && 
                    p.Catalog == item.Shard.Location.Database);
                Shardlet<int> shardlet = new Shardlet<int>();
                shardlet.Value = item.Value;
                shardlet.Status = item.Status;
                shardId.Shardlets.Add(shardlet);
                shardlets.Add(shardlet);
            }

            foreach (ShardIdentifier id in shardIds)
            {
                this.AddToCache(id.ToString(), id);
            }

            this.shardlets = shardlets;
            if (this.Initialized == true)
            {
                IEnumerable<ShardIdentifier> existing = this.SearchCache(p => true);
                IEnumerable<string> removed = existing.Except(shardIds).Select(p => p.ToString());
                this.RemoveFromCache(removed);
            }
        }

        /// <summary>
        /// Creates the Shard Map Manager.
        /// </summary>
        private static ShardMapManager CreateShardMapManager()
        {
            IConnectionFactory connectionStringFactory = Container.Get<IConnectionFactory>();
            string shardMapManagerConnectionString =
                connectionStringFactory.GetConnectionString("ShardMapManager");

            ShardMapManager smm;
            if (ShardMapManagerFactory.TryGetSqlShardMapManager(
                shardMapManagerConnectionString,
                ShardMapManagerLoadPolicy.Eager,
                out smm))
            {
                shardMapManager = smm;
            }
            else
            {
                shardMapManager = ShardMapManagerFactory.CreateSqlShardMapManager(shardMapManagerConnectionString);
            }

            return smm;
        }

        /// <summary>
        /// Adds shardlets to the Shard Map Manager.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <param name="shardId">The shard id to target.</param>
        /// <param name="shardLets">The shardlets to add.</param>
        private void AddShardlets(
            DatabaseType databaseType,
            ShardIdentifier shardId, 
            IEnumerable<int> shardLets)
        {
            lock (this.addShardletsLock)
            {
                ShardMap shardMap = this.GetShardMap(databaseType);
                ListShardMap<int> shardMappings = this.GetListShardMap(shardMap);
                HashSet<int> pointMappings = new HashSet<int>(shardMappings.GetMappings().Select(m => m.Value));

                // Get the shard associated with a server/database pair
                Shard shard = shardMap
                    .GetShards()
                    .Single(s =>
                        s.Location.Server == shardId.DataSource &&
                        s.Location.Database == shardId.Catalog);

                foreach (int identifier in shardLets)
                {
                    // Only create if the shardlet does not already exist
                    if (pointMappings.Contains(identifier) == false)
                    {
                        shardMappings.CreatePointMapping(identifier, shard);
                    }
                    else
                    {
                        PointMapping<int> mapping = shardMappings.GetMappings().Single(v => v.Value == identifier);

                        // Would the update be a noop? If so, skip
                        if (shard.Location.Server != mapping.Shard.Location.Server
                            || shard.Location.Database != mapping.Shard.Location.Database)
                        {
                            PointMappingUpdate update = new PointMappingUpdate();
                            update.Shard = shard;
                            if (mapping.Status != MappingStatus.Offline)
                            {
                                mapping = shardMappings.MarkMappingOffline(mapping);
                            }

                            mapping = shardMappings.UpdateMapping(mapping, update);
                            mapping = shardMappings.MarkMappingOnline(mapping);
                        }
                        else if (mapping.Status == MappingStatus.Offline)
                        {
                            // If the shardlet isn't being moved and is offline, make it online?
                            mapping = shardMappings.MarkMappingOnline(mapping);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the shard map: a collection of shards and mappings between keys and shards in the collection (shardlets).
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <returns>
        /// True if shard maps were found, false otherwise (false does not mean the request failed)
        /// </returns>
        private ShardMap GetShardMap(DatabaseType databaseType)
        {
            string mapName = DataAccessConstants.ShardMapName;
            ShardMap shardMap;
            // If shard map with the specified name was not found
            if (shardMapManager.TryGetShardMap(mapName, out shardMap) == false)
            {
                throw new KeyNotFoundException(
                    string.Format(@"The shard map with the name ""{0}"" does not exist", DataAccessConstants.ShardMapName));
            }

            return shardMap;
        }

        /// <summary>
        /// Get the default shard identifier.
        /// </summary>
        /// <param name="databaseType">The database type to use.</param>
        /// <returns>The default shard's id.</returns>
        private ShardIdentifier GetDefaultShardIdentifier(DatabaseType databaseType)
        {
            IEnumerable<ShardIdentifier> shardIds = this.GetShards(databaseType, new List<int>() { DataAccessConstants.MagicShardlet });
            if (shardIds.Count() != 1)
            {
                this.AddShardlets(databaseType, new List<int>() { DataAccessConstants.MagicShardlet });
                shardIds = this.GetShards(databaseType, new List<int>() { DataAccessConstants.MagicShardlet });
            }

            return shardIds.Single();
        }

        /// <summary>
        /// Converts a shard map into its base type "ListShardMap" which exposes the ability to interact with the point
        /// mappings (keys to shards = shardlets) contained within the shards.
        /// </summary>
        /// <param name="shardMap">The shard map.</param>
        /// <returns>ListShardMap, an object containing mapping of keys to shards</returns>
        /// <exception cref="System.InvalidCastException">Cannot cast shard map into ListShardMap</exception>
        private ListShardMap<int> GetListShardMap(ShardMap shardMap)
        {
            if (!(shardMap is ListShardMap<int>))
            {
                throw new InvalidCastException("Cannot cast shard map into ListShardMap");
            }

            return (ListShardMap<int>)shardMap;
        }
    }
}