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
    internal class ShardMapManagementAlt : CacheBase<ShardIdentifier, int, string>
    {
        /// <summary>
        /// The shardlet that designates the default shard.
        /// </summary>
        private const int MagicShardlet = 0;

        /// <summary>
        /// The shard map manager
        /// </summary>
        private static ShardMapManager shardMapManager = CreateShardMapManager();

        /// <summary>
        /// The instance of this singleton class
        /// </summary>
        private static ShardMapManagementAlt instance = new ShardMapManagementAlt();

        /// <summary>
        /// The add shardlets lock
        /// </summary>
        private object addShardletsLock = new object();

        /// <summary>
        /// The complete list of shardlets.
        /// </summary>
        private HashSet<int> shardlets;

        /// <summary>
        /// Magic Shardlet mapping
        /// </summary>
        private static ConcurrentDictionary<int, ShardIdentifier> magicShardletMapping = new ConcurrentDictionary<int, ShardIdentifier>();

        /// <summary>
        /// Prevents a default instance of the <see cref="ShardMapManagementAlt"/> class from being created.
        /// </summary>
        private ShardMapManagementAlt()
        {
        }

        /// <summary>
        /// Gets the instance of ShardMapManagement.
        /// </summary>
        /// <value>
        /// The instance of ShardMapManagement.
        /// </value>
        public static ShardMapManagementAlt Instance
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
        public ShardIdentifier GetDefaultShard()
        {
            if (magicShardletMapping.Count == 0)
            {
                lock (magicShardletMapping)
                {
                    if (magicShardletMapping.Count == 0)
                    {
                        magicShardletMapping.TryAdd(MagicShardlet, this.GetDefaultShardIdentifier());
                    }
                }
            }

            return magicShardletMapping[MagicShardlet];
        }

        /// <summary>
        /// Creates shardlets (mappings of keys to shards) in the Shard Map Manager.
        /// </summary>
        /// <param name="shardletIdentifiers">The identifiers used to create and identify shardlets.</param>
        /// <returns>
        /// The list of shards where the shardlets were mapped.
        /// </returns>
        public IEnumerable<ShardIdentifier> AddShardlets(IEnumerable<int> shardletIdentifiers)
        {
            return this.AddShardlets(shardletIdentifiers, false);
        }

        /// <summary>
        /// Creates shardlets (mappings of keys to shards) in the Shard Map Manager using the default policy.
        /// </summary>
        /// <param name="shardletIdentifiers">The identifiers used to create and identify shardlets.</param>
        /// <param name="policy">The policy to use for the add.</param>
        /// <returns>The list of shards where the shardlets were mapped.</returns>
        public IEnumerable<ShardIdentifier> AddShardlets(IEnumerable<int> shardletIdentifiers, IAddShardletPolicy policy)
        {
            return this.AddShardlets(shardletIdentifiers, policy, false);
        }

        /// <summary>
        /// Creates shardlets (mappings of keys to shards) in the Shard Map Manager using the default policy.
        /// </summary>
        /// <param name="shardletIdentifiers">The identifiers used to create and identify shardlets.</param>
        /// <param name="upsert">If set to <c>true</c> [permit updating of existing shardlets (remapping)].</param>
        /// <returns>
        /// The list of shards where the shardlets were mapped.
        /// </returns>
        public IEnumerable<ShardIdentifier> AddShardlets(IEnumerable<int> shardletIdentifiers, bool upsert)
        {
            return this.AddShardlets(shardletIdentifiers, ShardletPolicyFactory.Create(null), upsert);
        }

        /// <summary>
        /// Creates shardlets (mappings of keys to shards) in the Shard Map Manager.
        /// </summary>
        /// <param name="shardletIdentifiers">The identifiers used to create and identify shardlets.</param>
        /// <param name="policy">The policy to use for the add.</param>
        /// <param name="upsert">If set to <c>true</c> [permit updating of existing shardlets (remapping)].</param>
        /// <returns>The list of shards where the shardlets were mapped.</returns>
        public IEnumerable<ShardIdentifier> AddShardlets(IEnumerable<int> shardletIdentifiers, IAddShardletPolicy policy, bool upsert)
        {
            if (shardletIdentifiers == null)
            {
                throw new ArgumentNullException("shardletIdentifiers", "Cannot create shardlets as no shardlet identifiers were provided.");
            }

            HashSet<ShardIdentifier> shardIdentifiers = new HashSet<ShardIdentifier>();
            Dictionary<ShardIdentifier, List<int>> map;

            // Prevent double adds
            lock (addShardletsLock)
            {
                ShardMap shardMap = this.GetShardMap();
                ListShardMap<int> shardMappings = this.GetListShardMap(shardMap);
                HashSet<int> pointMappings = new HashSet<int>(shardMappings.GetMappings().Select(m => m.Value));

                map = policy.Distribute(null, shardletIdentifiers);
                foreach (ShardIdentifier key in map.Keys)
                {
                    // Get the shard associated with a server/database pair
                    Shard shard = shardMap
                        .GetShards()
                        .Single(s =>
                            string.Equals(s.Location.Server, key.DataSource, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(s.Location.Database, key.Catalog, StringComparison.OrdinalIgnoreCase));

                    StoreTelemetryEvent addShardTelemetry =
                        new StoreTelemetryEvent("AddShardlets", key.DataSource, key.Catalog);
                    addShardTelemetry.IsMultiTarget = false;
                    addShardTelemetry.Size = map[key].Count();

                    DataAccessTelemetry.Instance.Instrument(
                        addShardTelemetry,
                        (action) =>
                        {
                            foreach (int identifier in map[key])
                            {
                                // If the shardlet does not already exist
                                if (pointMappings.Contains(identifier) == false)
                                {
                                    shardMappings.CreatePointMapping(identifier, shard);
                                }
                                else if (upsert)
                                {
                                    PointMapping<int> mapping = shardMappings.GetMappings().Single(v => v.Value == identifier);

                                    // Would the update be a noop? If so, skip
                                    if (string.Equals(shard.Location.Server, mapping.Shard.Location.Server, StringComparison.OrdinalIgnoreCase)
                                        && string.Equals(shard.Location.Database, mapping.Shard.Location.Database, StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        PointMappingUpdate update = new PointMappingUpdate();
                                        update.Shard = shard;
                                        MappingStatus initialShardStatus = mapping.Status;

                                        // If the shardlet is online, mark offline
                                        if (mapping.Status == MappingStatus.Online)
                                        {
                                            mapping = shardMappings.MarkMappingOffline(mapping);
                                        }

                                        mapping = shardMappings.UpdateMapping(mapping, update);

                                        // If the shardlet was initially online, bring it back online
                                        if (initialShardStatus == MappingStatus.Online)
                                        {
                                            mapping = shardMappings.MarkMappingOnline(mapping);
                                        }
                                    }
                                }

                                shardIdentifiers.Add(key);
                            }
                        });
                }
            }

            this.RunNow(true);
            return shardIdentifiers;
        }

        /// <summary>
        /// Read all the currently configured shardlets in the system.
        /// </summary>
        /// <returns>The list of shardlets.</returns>
        public IEnumerable<int> GetShardlets()
        {
            return this.shardlets.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the list of shardlets (keys) associated with a specific shard.
        /// </summary>
        /// <param name="shardIdentifier">The location where these shardlets should be created.</param>
        /// <returns>The list of shardlet identifiers mapped to the specified shard.</returns>
        /// <exception cref="ArgumentNullException">shardIdentifier;Cannot query shardlets as the shard identifier is
        /// not provided.</exception>
        /// <exception cref="NullReferenceException">
        /// Cannot query shardlets as the database name is not specified.
        /// or
        /// Cannot query shardlets as the server name is not specified.
        /// </exception>
        public IEnumerable<int> GetShardlets(ShardIdentifier shardIdentifier)
        {
            if (shardIdentifier == null)
            {
                throw new ArgumentNullException("shardIdentifier", "Cannot query shardlets as the shard identifier is not provided.");
            }

            if (string.IsNullOrEmpty(shardIdentifier.Catalog))
            {
                throw new ArgumentNullException("Cannot query shardlets as the database name is not specified.");
            }

            if (string.IsNullOrEmpty(shardIdentifier.DataSource))
            {
                throw new ArgumentNullException("Cannot query shardlets as the server name is not specified.");
            }

            ShardIdentifier value;
            this.TryGetValue(shardIdentifier.ToString(), out value);

            return value.Shardlets.Select(p => p.Value);
        }

        /// <summary>
        /// Removes shardlets (mappings of keys to shards) from the Shard Map Manager. Does not delete associated data.
        /// </summary>
        /// <param name="shardletIdentifiers">The identifiers of the shardlets to remove.</param>
        /// <param name="excludeSpecialShardlets">if set to <c>true</c> [automatically ignore special shardlets when deleting shards].</param>
        /// <returns>
        /// The number of shardlets impacted by this removal.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">shardletIdentifiers;Cannot create shardlets as no shardlet identifiers were provided.</exception>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.NullReferenceException">Cannot create shardlets as no shardlet identifiers were
        /// provided.</exception>
        public int RemoveShardlets(IEnumerable<int> shardletIdentifiers, bool excludeSpecialShardlets = false)
        {
            if (shardletIdentifiers == null)
            {
                throw new ArgumentNullException("shardletIdentifiers", "Cannot create shardlets as no shardlet identifiers were provided.");
            }

            if (excludeSpecialShardlets)
            {
                shardletIdentifiers = shardletIdentifiers.Where(s => s != MagicShardlet);
            }

            int impactedCount = 0;
            ListShardMap<int> listShardMap = this.GetListShardMap(this.GetShardMap());
            IReadOnlyList<PointMapping<int>> mappings = listShardMap.GetMappings();

            foreach (int identifier in shardletIdentifiers)
            {
                PointMapping<int> pointMapping = mappings.SingleOrDefault(m => m.Value == identifier);

                if (pointMapping != null)
                {
                    // in order to delete a mapping, it must be first marked offline
                    if (pointMapping.Status != MappingStatus.Offline)
                    {
                        pointMapping = listShardMap.MarkMappingOffline(pointMapping);
                    }

                    listShardMap.DeleteMapping(pointMapping);
                    impactedCount++;
                }
            }

            this.RunNow(true);
            return impactedCount;
        }

        /// <summary>
        /// Gets the list of all databases and server combinations that are mapped in the Shard Map Manager.
        /// </summary>
        /// <returns> The list of databases and servers (shard identifiers). </returns>
        public IEnumerable<ShardIdentifier> GetShards()
        {
            return this.SearchCache(p => true);
        }

        /// <summary>
        /// Gets the list of shard identifiers for a given list of shardlets.
        /// </summary>
        /// <param name="shardlets">The given list of shardlets.</param>
        /// <returns>The list of shard identifiers.</returns>
        public IEnumerable<ShardIdentifier> GetShards(IEnumerable<int> shardlets)
        {
            HashSet<ShardIdentifier> results = new HashSet<ShardIdentifier>();
            IEnumerable<ShardIdentifier> shardIds = this.GetShards();
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
        /// Add shard to Shard Map Manager
        /// </summary>
        /// <param name="shard">The shard identifier to add.</param>
        internal void AddShard(ShardIdentifier shardIdentifier)
        {
            if (shardIdentifier == null)
            {
                throw new ArgumentNullException("shardIdentifiers", "Cannot add shard no shard identifier was provided.");
            }

            ShardMap shardMap = this.GetShardMap();
            ShardLocation shardLocation = new ShardLocation(shardIdentifier.DataSource, shardIdentifier.Catalog);
            Shard shard;

            if (shardMap.TryGetShard(shardLocation, out shard) == true)
            {
                throw new ArgumentException(string.Format("Shard (ServerName {0}, Database {1} ) has already been added to the Shard Map", shardLocation.Server, shardLocation.Database));
            }
            else
            {
                // The Shard Map does not exist, so create it
                shard = shardMap.CreateShard(shardLocation);
            }

            this.RunNow(true);
        }

        /// <summary>
        /// Get the default shardlet identifier.
        /// </summary>
        /// <returns>The default shardlet's id.</returns>
        internal int GetDefaultShardletIdentifier()
        {
            return MagicShardlet;
        }

        /// <summary>
        /// Gets the collection of shards based on shard identifiers
        /// </summary>
        /// <param name="shardIdentifiers">The identifiers of the shards.</param>
        /// <returns>Collection of shards.</returns>
        internal IEnumerable<Shard> GetElasticScaleShards(IEnumerable<ShardIdentifier> shardIdentifiers)
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
        /// Gets the shard map: a collection of shards and mappings between keys and shards in the collection (shardlets).
        /// </summary>
        /// <returns>
        /// True, if shard maps were found, false otherwise.
        /// </returns>
        /// <exception cref="System.InvalidCastException">Cannot cast shard map into ListShardMap</exception>
        internal ShardMap GetShardMap()
        {
            ShardMap shardMap;
            // If shard map with the specified name was not found
            if (!shardMapManager.TryGetShardMap(DataAccessConstants.ShardMapName, out shardMap))
            {
                throw new KeyNotFoundException(
                    string.Format(@"The shard map with the name ""{0}"" does not exist", DataAccessConstants.ShardMapName));
            }

            return shardMap;
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
            ShardMap shardMap = this.GetShardMap();
            ListShardMap<int> shardMappings = this.GetListShardMap(shardMap);
            IReadOnlyList<PointMapping<int>> mappings = shardMappings.GetMappings();
            HashSet<int> shardlets = new HashSet<int>();

            IEnumerable<ShardIdentifier> shardIds = shardMap.GetShards().Select(x => 
                new ShardIdentifier(x.Location.Server, x.Location.Database, x.Location.Port)
                {
                    Shard = x
                }).ToArray();

            foreach (PointMapping<int> item in mappings)
            {
                ShardIdentifier shardId = shardIds.Single(p =>
                    p.DataSource == item.Shard.Location.Server &&
                    p.Catalog == item.Shard.Location.Database);
                Shardlet<int> shardlet = new Shardlet<int>();
                shardlet.Value = item.Value;
                shardlet.Status = item.Status;
                shardId.Shardlets.Add(shardlet);
                if (shardlet.Status == MappingStatus.Online)
                {
                    shardlets.Add(shardlet.Value);
                }
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
        /// Get the default shard identifier.
        /// </summary>
        /// <returns>The default shard's id.</returns>
        private ShardIdentifier GetDefaultShardIdentifier()
        {
            IEnumerable<ShardIdentifier> shardIds = this.GetShards(new List<int>() { MagicShardlet });
            if (shardIds.Count() != 1)
            {
                this.AddShardlets(new List<int>() { MagicShardlet });
                shardIds = this.GetShards(new List<int>() { MagicShardlet });
            }

            return shardIds.Single();
        }

        /// <summary>
        /// This private helper fetches a fresh copy of the pointmapping from SMM
        /// </summary>
        /// <returns>PointMappings of the shardmap</returns>
        private IReadOnlyList<PointMapping<int>> GetPointMappings()
        {
            ShardMap shardMap = this.GetShardMap();
            ListShardMap<int> shardMappings = this.GetListShardMap(shardMap);
            IReadOnlyList<PointMapping<int>> mappings = shardMappings.GetMappings();
            return mappings;
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