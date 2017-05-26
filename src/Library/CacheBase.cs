// -----------------------------------------------------------------------
// <copyright file="CacheBase.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// The base class for caching data.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <typeparam name="U">The type of the chunking term.</typeparam>
    /// <typeparam name="V">The type of the key item.</typeparam>
    public abstract class CacheBase<T, U, V>
    {
        /// <summary>
        /// The thread lock.
        /// </summary>
        private static ReaderWriterLock threadLock =
            new ReaderWriterLock();

        /// <summary>
        /// The logger to use.
        /// </summary>
        private static IMessageLogger logger = Container.Get<IMessageLogger>();

        /// <summary>
        /// The wait handle for the background thread control.
        /// </summary>
        private ManualResetEvent handle = new ManualResetEvent(false);

        /// <summary>
        /// The thread timer.
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Run counter for full load.
        /// </summary>
        private int runsSinceLastFullLoad;

        /// <summary>
        /// Initialization state.
        /// </summary>
        private ManualResetEvent initialized = new ManualResetEvent(false);

        /// <summary>
        /// Lock when refreshing the cache.
        /// </summary>
        private ReaderWriterLock refreshLock = new ReaderWriterLock();

        /// <summary>
        /// Lock when writing to next written time.
        /// </summary>
        private object nextwrittenLock = new object();

        /// <summary>
        /// Last written time.
        /// </summary>
        private DateTimeOffset lastwritten = new DateTimeOffset(new DateTime(1753, 1, 1));

        /// <summary>
        /// Next written time.
        /// </summary>
        private DateTimeOffset nextwritten;

        /// <summary>
        /// The cache of data.
        /// </summary>
        private ConcurrentDictionary<V, T> cache = new ConcurrentDictionary<V, T>();

        /// <summary>
        /// Initializes a new instance of the CacheBase class.
        /// </summary>
        protected CacheBase()
        {
            this.Connected = true;
            this.Start();
        }

        /// <summary>
        /// Finalizes an instance of the CacheBase class.
        /// </summary>
        ~CacheBase()
        {
            this.Shutdown();
        }

        /// <summary>
        /// Gets a value indicating whether the cache is initialized.
        /// </summary>
        public bool Initialized
        {
            get
            {
                return this.initialized.WaitOne(0);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the cache is connected.
        /// </summary>
        protected bool Connected
        {
            get;
            set;
        }

        /// <summary>
        /// Turns off the connection for the cache.
        /// </summary>
        public void TurnOffConnection()
        {
            this.Connected = false;
            this.initialized.Set();
        }

        /// <summary>
        /// Run the cache load operation synchronously.
        /// </summary>
        /// <param name="full">True if a full load should be run, otherwise false.</param>
        public virtual void RunNow(bool full)
        {
            if (this.Initialized == true && this.Connected == true)
            {
                try
                {
                    this.Refresh(full);
                }
                catch (Exception ex)
                {
                    Log(ex, null);
                    throw;
                }
            }
        }

        /// <summary>
        /// Get the cached item for the given key.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <param name="value">The discovered value.</param>
        /// <returns>True if a value was discovered, otherwise false.</returns>
        public virtual bool TryGetValue(V key, out T value)
        {
            // wait until initialized by first refresh.
            if (this.initialized.WaitOne(TimeSpan.FromSeconds(300)) == true)
            {
                return this.cache.TryGetValue(key, out value);
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cache is non-functional: {0}",
                        this.GetType().Name));
            }
        }

        /// <summary>
        /// A helper function to fire the qos event.
        /// </summary>
        /// <param name="duration">The time span.</param>
        /// <param name="apiId">The api for the event.</param>
        /// <param name="exception">The exception encountered.</param>
        protected static void FireQosEvent(TimeSpan duration, string apiId, Exception exception)
        {
            if (exception != null && exception is AggregateException)
            {
                AggregateException agg = exception as AggregateException;
                if (agg.InnerExceptions != null)
                {
                    foreach (Exception ex in agg.InnerExceptions)
                    {
                        FireQosEvent(duration, apiId, ex);
                    }

                    return;
                }
            }

            try
            {
                Log("{0} executed in {1}. Outcome = {2}", 
                    apiId, 
                    duration.TotalSeconds, 
                    exception == null ? "Ok" : exception.Message + "\n" + exception.StackTrace);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// A helper function to fire the qos event.
        /// </summary>
        /// <param name="apiid">The identifier for the caller.</param>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments.</param>
        protected static void FireQosEvent(string apiid, string message, params object[] args)
        {
            Log(apiid + ":" + message, args);
        }

        /// <summary>
        /// Search the cache for data matching the given criteria.
        /// </summary>
        /// <param name="criteria">The criteria to use.</param>
        /// <returns>The list of cache items.</returns>
        protected IEnumerable<T> SearchCache(Func<T, bool> criteria)
        {
            if (this.initialized.WaitOne(TimeSpan.FromSeconds(300)) == true)
            {
                return this.cache.Values.Where(criteria);
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cache is non-functional: {0}",
                        this.GetType().Name));
            }
        }

        /// <summary>
        /// Read all the keys from the cache.
        /// </summary>
        /// <returns>The collection of keys.</returns>
        protected IEnumerable<V> ReadAllKeys()
        {
            if (this.initialized.WaitOne(TimeSpan.FromSeconds(300)) == true)
            {
                return this.cache.Keys;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cache is non-functional: {0}",
                        this.GetType().Name));
            }
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="arg">Argument for load.</param>
        /// <param name="lastWrittenTime">The last written time.</param>
        protected abstract void Load(U arg, DateTimeOffset lastWrittenTime);

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="arg">Argument for load.</param>
        protected abstract void Load(U arg);

        /// <summary>
        /// Read the items to be used for chunking.
        /// </summary>
        /// <returns>The list of chunk items.</returns>
        protected virtual IEnumerable<U> ReadChunkItems()
        {
            return new List<U>() { default(U) };
        }

        /// <summary>
        /// Read the size of the thread pool.
        /// </summary>
        /// <returns>The number of threads to use in the pool.</returns>
        protected virtual int ReadPoolSize()
        {
            return 1;
        }

        /// <summary>
        /// Read the number of minutes that should elapse between calls to full load.
        /// </summary>
        /// <returns>The timespan for time between full loads.</returns>
        protected virtual TimeSpan ReadTimeBetweenFullLoads()
        {
            return TimeSpan.FromMinutes(-1);
        }

        /// <summary>
        /// Read the interval at which the reload thread should execute.
        /// </summary>
        /// <returns>The timespan for the interval.</returns>
        protected virtual TimeSpan ReadInterval()
        {
            return TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Read the number of retries for a chunk in the load.
        /// </summary>
        /// <returns>The number of retries to have for a failed chunk.</returns>
        protected virtual int ReadRetries()
        {
            return 1;
        }

        /// <summary>
        /// Reads the api id to use.
        /// </summary>
        /// <param name="fullLoad">A value indicating whether a full load is running.</param>
        /// <returns>The apiid to use.</returns>
        protected virtual string ReadApiId(bool fullLoad)
        {
            if (fullLoad == true)
            {
                return this.GetType().Name + ".FullLoadCache";
            }
            else
            {
                return this.GetType().Name + ".LoadCache";
            }
        }

        /// <summary>
        /// Test the provided value against the next watermark value. Replace if applicable.
        /// </summary>
        /// <param name="candidate">The candidate watermark value.</param>
        protected void TestUpdateWatermark(DateTimeOffset candidate)
        {
            lock (this.nextwrittenLock)
            {
                if (candidate > this.nextwritten)
                {
                    this.nextwritten = candidate;
                }
            }
        }

        /// <summary>
        /// Adds an item to the cache.
        /// </summary>
        /// <param name="key">The item's key.</param>
        /// <param name="value">The item to add.</param>
        /// <returns>The added or updated value.</returns>
        protected T AddToCache(V key, T value)
        {
            return this.cache.AddOrUpdate(key, value, (p, m) => value);
        }

        /// <summary>
        /// Removes an item from the cache.
        /// </summary>
        /// <param name="key">The item's key.</param>
        /// <param name="value">The item to add.</param>
        /// <returns>True if the item was removed, otherwise false.</returns>
        protected bool RemoveFromCache(V key, out T value)
        {
            return this.cache.TryRemove(key, out value);
        }

        /// <summary>
        /// Purges a list of keys from the cache.
        /// </summary>
        /// <param name="keysToPurge">The list of keys to remove.</param>
        protected void RemoveFromCache(IEnumerable<V> keysToPurge)
        {
            foreach (V key in keysToPurge)
            {
                T value;
                this.RemoveFromCache(key, out value);
            }
        }

        /// <summary>
        /// Log Helper.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments.</param>
        private static void Log(string message, params object[] args)
        {
            try
            {
                // write to logger.
                logger.Info(message, args);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Log Helper.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        private static void Log(Exception exception, string message)
        {
            logger.Error(exception, message);
        }

        /// <summary>
        /// Refreshes the data in the cache.
        /// </summary>
        /// <param name="fullLoad">True to do a full load this run, otherwise false.</param>
        private void Refresh(bool fullLoad)
        {
            Stopwatch timer = new Stopwatch();

            try
            {
                // Attempt to acquire the lock. If the locking doesn't succeed in a timely manner, fail the call.
                try
                {
                    this.refreshLock.AcquireWriterLock(this.ReadInterval());
                }
                catch (ApplicationException)
                {
                    Log(string.Format("Timed out waiting for the refreshLock for type {0}", this.GetType().Name));

                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Cache refresh failed waiting for the refreshLock: {0}",
                            this.GetType().Name));
                }

                timer.Start();
                int poolsize = this.ReadPoolSize();
                if (poolsize > 100)
                {
                    poolsize = 100;
                }

                IEnumerable<U> chunks = this.ReadChunkItems();
                if (chunks.Count() == 0)
                {
                    Log("{0} has no work to do.", this.GetType().Name);
                    return;
                }

                AutoResetEvent[] events = new AutoResetEvent[poolsize];
                Thread[] threads = new Thread[poolsize];

                this.nextwritten = this.lastwritten;
                Dictionary<int, U> keys = this.BuildKeys(chunks);
                ConcurrentDictionary<int, int> errors = this.BuildErrors(keys);
                ConcurrentQueue<U> queue = new ConcurrentQueue<U>(chunks);
                for (int i = 0; i < poolsize; i++)
                {
                    WorkHelper wh = new WorkHelper(errors, keys)
                    {
                        AllClear = new AutoResetEvent(false),
                        Queue = queue,
                        RunFullLoad = fullLoad,
                    };

                    threads[i] = new Thread(this.RunWorker);
                    events[i] = wh.AllClear;
                    threads[i].Start(wh);
                }

                while (WaitHandle.WaitAll(events, 3000) == false)
                {
                    Log("{0} Waiting", this.GetType().Name);
                }

                Log("{0} Completed: {1}", this.GetType().Name, timer.Elapsed);
                foreach (Thread thread in threads)
                {
                    thread.Join(1000);
                }

                int failed = errors.Where(p => p.Value == this.ReadRetries()).Count();
                if (failed == 0 && errors.Count > 0)
                {
                    Log("{0} Updating watermark: {1}", this.GetType().Name, this.nextwritten);
                    this.lastwritten = this.nextwritten;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Cache refresh failed: {0}",
                            this.GetType().Name));
                }
            }
            catch (Exception)
            {
                // throw back to timer thread handler.
                throw;
            }
            finally
            {
                timer.Stop();
                if (this.refreshLock.IsWriterLockHeld == true)
                {
                    this.refreshLock.ReleaseWriterLock();
                }
            }
        }

        /// <summary>
        /// Build the errors map.
        /// </summary>
        /// <param name="keys">The key map.</param>
        /// <returns>The map of errors to keys.</returns>
        private ConcurrentDictionary<int, int> BuildErrors(Dictionary<int, U> keys)
        {
            ConcurrentDictionary<int, int> errors = new ConcurrentDictionary<int, int>();
            foreach (int key in keys.Keys)
            {
                if (errors.TryAdd(key, 0) == false)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "BuildErrors failed: {0}",
                            this.GetType().Name));
                }
            }

            return errors;
        }

        /// <summary>
        /// Build the map of keys.
        /// </summary>
        /// <param name="chunks">The lis of chunks.</param>
        /// <returns>The map of keys to chunks.</returns>
        private Dictionary<int, U> BuildKeys(IEnumerable<U> chunks)
        {
            if (chunks.Count() != chunks.Distinct().Count())
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "BuildKeys failed: {0}",
                        this.GetType().Name));
            }

            int i = 0;
            Dictionary<int, U> keys = new Dictionary<int, U>();
            foreach (U chunk in chunks)
            {
                keys[i++] = chunk;
            }

            return keys;
        }

        /// <summary>
        /// Run a worker thread to retrieve items.
        /// </summary>
        /// <param name="parameter">The state parameter for the thread.</param>
        private void RunWorker(object parameter)
        {
            WorkHelper wh = parameter as WorkHelper;

            U item;
            while (wh.Queue.TryDequeue(out item) == true &&
                this.handle.WaitOne(0) == false)
            {
                bool retrying = false;
                Exception exception = null;
                Stopwatch duration = Stopwatch.StartNew();

                try
                {
                    string msg = this.GetType().Name;
                    Log("{0} Getting Entries {1}", msg, item);
                    if (wh.RunFullLoad == true)
                    {
                        this.Load(item);
                    }
                    else
                    {
                        this.Load(item, this.lastwritten);
                    }
                }
                catch (Exception ex)
                {
                    // try again if max retries hasn't been hit.
                    int retries = this.ReadRetries();
                    exception = ex;
                    if (wh.Requeue(item, retries) == true)
                    {
                        wh.Queue.Enqueue(item);
                        retrying = true;
                    }

                    Log(ex, null);
                }
                finally
                {
                    duration.Stop();
                    if (retrying == false)
                    {
                        FireQosEvent(
                            duration.Elapsed,
                            this.ReadApiId(wh.RunFullLoad),
                            exception);
                    }
                }
            }

            wh.AllClear.Set();
        }

        /// <summary>
        /// Background processing to reload cache.
        /// </summary>
        /// <param name="handle">The wait handle to listen to.</param>
        private void TimerThread(object handle)
        {
            try
            {
                try
                {
                    threadLock.AcquireWriterLock(0);
                }
                catch (ApplicationException)
                {
                    Log(
                        "Failed to immediately acquire the threadLock for type {0}," +
                        " there is likely already an update occuring on another thread",
                        this.GetType().Name);
                    return;
                }

                ManualResetEvent reset = handle as ManualResetEvent;
                if (reset.WaitOne(0) == true)
                {
                    return;
                }
                else if (this.Connected == false)
                {
                    this.initialized.Set();
                    return;
                }

                double interval = this.ReadInterval().TotalMilliseconds;
                this.Refresh(this.runsSinceLastFullLoad == 0);
                this.initialized.Set();
                this.runsSinceLastFullLoad++;

                TimeSpan span = TimeSpan.FromMilliseconds(this.runsSinceLastFullLoad * interval);
                TimeSpan max = this.ReadTimeBetweenFullLoads();
                if (max.TotalSeconds >= 0 && (max - span).TotalMilliseconds <= 0)
                {
                    this.runsSinceLastFullLoad = 0;
                }
            }
            catch (Exception ex)
            {
                // can't initialize, cache is stale
                Log(ex, null);
            }
            finally
            {
                if (threadLock.IsWriterLockHeld == true)
                {
                    threadLock.ReleaseWriterLock();
                }
            }
        }

        /// <summary>
        /// Start the cache.
        /// </summary>
        private void Start()
        {
            TimeSpan interval = this.ReadInterval();

            if (interval.Ticks >= 0)
            {
                this.timer = new Timer(this.TimerThread, this.handle, TimeSpan.FromTicks(0), interval);
            }
            else
            {
                this.initialized.Set();
            }
        }

        /// <summary>
        /// Shutdown the timer thread and free the cache.
        /// </summary>
        private void Shutdown()
        {
            if (this.handle != null)
            {
                this.handle.Set();
                this.handle = null;
            }

            if (this.timer != null)
            {
                this.timer.Dispose();
                this.timer = null;
            }

            if (this.cache != null)
            {
                this.cache.Clear();
                this.cache = null;
            }
        }

        /// <summary>
        /// Helper class to pass data to threads.
        /// </summary>
        private class WorkHelper
        {
            /// <summary>
            /// The list of items.
            /// </summary>
            private ConcurrentDictionary<int, int> items;

            /// <summary>
            /// The list of keys.
            /// </summary>
            private Dictionary<int, U> keys;

            /// <summary>
            /// Initializes a new instance of the WorkHelper class.
            /// </summary>
            /// <param name="errors">The map of errors.</param>
            /// <param name="keys">The map of keys.</param>
            public WorkHelper(ConcurrentDictionary<int, int> errors, Dictionary<int, U> keys)
            {
                this.items = errors;
                this.keys = keys;
            }

            /// <summary>
            /// Gets or sets a value indicating whether or not to run a full load on this iteration.
            /// </summary>
            public bool RunFullLoad
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the completed event.
            /// </summary>
            public AutoResetEvent AllClear
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the concurrent queue of characters.
            /// </summary>
            public ConcurrentQueue<U> Queue
            {
                get;
                set;
            }

            /// <summary>
            /// Indicates whether failed load should be queued again.
            /// </summary>
            /// <param name="item">The failing item.</param>
            /// <param name="retries">The number of retries to use.</param>
            /// <returns>True to requeue, otherwise false.</returns>
            public bool Requeue(U item, int retries)
            {
                int key = 0;
                if (item != null)
                {
                    key = this.keys.Where(p => p.Value.Equals(item) == true).Single().Key;
                }
                else
                {
                    key = this.keys.Where(p => p.Value == null).Single().Key;
                }

                int value = this.items.AddOrUpdate(key, 0, (c, v) => ++v);

                return value < retries;
            }
        }
    }
}
