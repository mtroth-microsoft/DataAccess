// -----------------------------------------------------------------------
// <copyright file="ConfigurationCache.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using OdataExpressionModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using IO = System.IO;

    /// <summary>
    /// Implementation of the configuration cache.
    /// </summary>
    public class ConfigurationCache : IConfigurationCache
    {
        /// <summary>
        /// The instance.
        /// </summary>
        private static IConfigurationCache instance = new ConfigurationCache();

        /// <summary>
        /// The cached configuration data.
        /// </summary>
        private Dictionary<string, string> data = new Dictionary<string, string>();

        /// <summary>
        /// The cached strongly typed data.
        /// </summary>
        private ConcurrentDictionary<string, IStronglyNamed> typedData = new ConcurrentDictionary<string, IStronglyNamed>();

        /// <summary>
        /// Prevents the initialization of an instance of the ConfigurationCache class.
        /// </summary>
        private ConfigurationCache()
        {
            this.Initialize();
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static IConfigurationCache Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Read the contents of the cache.
        /// </summary>
        /// <returns>The list of keys.</returns>
        public IEnumerable<string> ReadInventory()
        {
            return this.data.Keys;
        }

        /// <summary>
        /// Read the raw json for an entity.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>The related json.</returns>
        public string ReadRaw(string key)
        {
            return this.data[key];
        }

        /// <summary>
        /// Load lists of root objects into the cache.
        /// </summary>
        /// <typeparam name="T">The concrete type of the root objects.</typeparam>
        /// <param name="entities">The list of entities to add.</param>
        public void Load<T>(IEnumerable<T> entities)
            where T : class, IStronglyNamed
        {
            foreach (T item in entities)
            {
                string key = item.Namespace + '.' + item.Name;
                this.typedData.AddOrUpdate(key, p => item, (p, q) => item);
            }

            IEnumerable<T> existing = this.typedData.Values.OfType<T>();
            foreach (T item in existing)
            {
                string key = item.Namespace + '.' + item.Name;
                if (entities.Any(p => p.Name.Equals(item.Name) && p.Namespace.Equals(item.Namespace)) == false)
                {
                    IStronglyNamed ro;
                    this.typedData.TryRemove(key, out ro);
                }
            }
        }

        /// <summary>
        /// Query for a specific instance of a given type.
        /// </summary>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <param name="fullName">The full name of the instance.</param>
        /// <returns>The correlated instance.</returns>
        T IConfigurationCache.GetWellKnownInstance<T>(string fullName)
        {
            IStronglyNamed ro = null;
            if (this.typedData.TryGetValue(fullName, out ro) == false)
            {
                return this.GetInstance<T>(fullName);
            }

            return ro as T;
        }

        /// <summary>
        /// Query for a specific instance of a given type.
        /// </summary>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <param name="fullName">The full name of the instance.</param>
        /// <returns>The correlated instance.</returns>
        T IConfigurationCache.GetWellKnownInstance<T>(FullyQualifiedNamedObject fullName)
        {
            string key = fullName.Namespace + '.' + fullName.Name;
            IStronglyNamed ro = null;
            if (this.typedData.TryGetValue(key, out ro) == false)
            {
                return this.GetInstance<T>(key);
            }

            return ro as T;
        }

        /// <summary>
        /// Get the set of instances corresponding to the provided type.
        /// </summary>
        /// <typeparam name="T">The type to look for.</typeparam>
        /// <returns>The list of corresponding entities.</returns>
        IEnumerable<T> IConfigurationCache.GetTypedSet<T>()
        {
            return this.typedData.Values.OfType<T>();
        }

        /// <summary>
        /// Locates the fact base instance for the feed reference.
        /// </summary>
        /// <param name="feedReference">The feed reference to inspect.</param>
        /// <returns>The fact base.</returns>
        FactBase IConfigurationCache.GetFactBase(FeedReference feedReference)
        {
            if (feedReference == null)
            {
                throw new ArgumentNullException("feedReference");
            }

            IEnumerable<Fact> facts = ConfigurationCache.Instance.GetTypedSet<Fact>();
            IEnumerable<Aggregate> aggs = ConfigurationCache.Instance.GetTypedSet<Aggregate>();
            Fact fact = facts
                .SingleOrDefault(p => p.Name.Equals(feedReference.Name) && p.Namespace.Equals(feedReference.Namespace));
            Aggregate agg = aggs
                .SingleOrDefault(p => p.Name.Equals(feedReference.Name) && p.Namespace.Equals(feedReference.Namespace));

            if (fact != null)
            {
                return fact;
            }
            else if (agg != null)
            {
                return agg;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Read the instance from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <param name="fullName">The full name.</param>
        /// <returns>The correlated instance.</returns>
        private T GetInstance<T>(string fullName)
            where T : class
        {
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("fullName");
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;

            if (this.data.ContainsKey(fullName) == true)
            {
                T item = JsonConvert.DeserializeObject<T>(this.data[fullName], settings);
                IHierarchical hierarchical = item as IHierarchical;
                if (hierarchical != null)
                {
                    this.MergeHierarchy<T>(hierarchical);
                }

                return item;
            }

            throw new KeyNotFoundException(string.Format("Missing key: {0}", fullName));
        }

        /// <summary>
        /// Merge the hierarchy.
        /// </summary>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <param name="entity">The current entity.</param>
        private void MergeHierarchy<T>(IHierarchical entity)
            where T : class
        {
            string baseName = entity.Base;
            if (string.IsNullOrEmpty(baseName) == false)
            {
                T baseInstance = ConfigurationCache.Instance.GetWellKnownInstance<T>(baseName);
                if (baseInstance != null)
                {
                    entity.Merge(baseInstance as IHierarchical);
                }
            }
        }

        /// <summary>
        /// Initialize the cache.
        /// </summary>
        private void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyLoad += this.OnAssemblyLoad;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                this.LoadDataFromAssembly(assembly);
            }
        }

        /// <summary>
        /// Listener for new assembly load events.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            this.LoadDataFromAssembly(args.LoadedAssembly);
        }

        /// <summary>
        /// Load the configuration data from an assembly.
        /// </summary>
        /// <param name="loadedAssembly">The assembly to inspect.</param>
        private void LoadDataFromAssembly(Assembly loadedAssembly)
        {
            if (loadedAssembly.IsDynamic == true)
            {
                return;
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;

            string[] names = null;
            try
            {
                names = loadedAssembly.GetManifestResourceNames();
            }
            catch (Exception)
            {
                return;
            }

            foreach (string name in names)
            {
                if (name.EndsWith(".json") == true)
                {
                    using (IO.Stream stream = loadedAssembly.GetManifestResourceStream(name))
                    {
                        using (IO.StreamReader reader = new IO.StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            JObject item;
                            try
                            {
                                item = JObject.Parse(json);
                                if (item != null)
                                {
                                    JToken itemName = null, itemNs = null;
                                    if (item.TryGetValue("Name", out itemName) == true &&
                                        item.TryGetValue("Namespace", out itemNs) == true)
                                    {
                                        this.data[itemNs.ToString() + '.' + itemName.ToString()] = json;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                throw new FormatException("Configuration cache could not parse this json: " + json, e);
                            }
                        }

                    }
                }
            }
        }
    }
}
