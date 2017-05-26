// -----------------------------------------------------------------------
// <copyright company="Lensgrinder, Ltd.">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an API through which to capture telemetry events originating within DataAccess.
    /// </summary>
    public sealed class DataAccessTelemetry : IObservable<StoreTelemetryEvent>
    {
        /// <summary>
        /// The store telemetry instance
        /// </summary>
        private static readonly DataAccessTelemetry DataAccessTelemetryInstance = new DataAccessTelemetry();

        /// <summary>
        /// The observer read write lock
        /// </summary>
        private static readonly ReaderWriterLockSlim ObserverReadWriteLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The telemetry observers of SQL store actions
        /// </summary>
        private readonly List<IObserver<StoreTelemetryEvent>> observers;

        /// <summary>
        /// Prevents a default instance of the <see cref="DataAccessTelemetry"/> class from being created.
        /// </summary>
        private DataAccessTelemetry()
        {
            this.observers = new List<IObserver<StoreTelemetryEvent>>();
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>
        /// The singleton instance.
        /// </value>
        public static DataAccessTelemetry Instance
        {
            get
            {
                return DataAccessTelemetryInstance;
            }
        }

        /// <summary>
        /// Allows subscription to telemetry events.
        /// </summary>
        /// <param name="observer">The observer to the telemetry events.</param>
        /// <returns>An unsubscriber</returns>
        public IDisposable Subscribe(IObserver<StoreTelemetryEvent> observer)
        {
            ObserverReadWriteLock.EnterWriteLock();
            try
            {
                if (!observers.Contains(observer))
                {
                    this.observers.Add(observer);
                }
            }
            finally
            {
                ObserverReadWriteLock.ExitWriteLock();
            }

            return new Unsubscriber(observers, observer);
        }

        /// <summary>
        /// Notifies the observers of a telemetry event.
        /// </summary>
        /// <param name="telemetry">The telemetry event.</param>
        public void Notify(StoreTelemetryEvent telemetry)
        {
            ObserverReadWriteLock.EnterReadLock();
            try
            {
                Parallel.ForEach(
                    this.observers,
                    observer => observer.OnNext(telemetry));
            }
            finally
            {
                ObserverReadWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Instruments an action where the sql connection is provided.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="database">The database.</param>
        /// <param name="storedProcedureName">The stored procedure being called.</param>
        /// <param name="action">The action to be executed.</param>
        internal void Instrument(string dataSource, string database, string storedProcedureName, Action<StoreTelemetryEvent> action)
        {
            StoreTelemetryEvent storeTelemetryEvent = new StoreTelemetryEvent(storedProcedureName, dataSource, database);
            this.Instrument(storeTelemetryEvent, action);
        }

        /// <summary>
        /// Instruments an action where the sql connection is not provided.
        /// </summary>
        /// <param name="storedProcedureName">The stored procedure being called.</param>
        /// <param name="action">The action to be executed.</param>
        internal void Instrument(string storedProcedureName, Action<StoreTelemetryEvent> action)
        {
            StoreTelemetryEvent storeTelemetryEvent = new StoreTelemetryEvent(storedProcedureName);
            this.Instrument(storeTelemetryEvent, action);
        }

        /// <summary>
        /// Instruments a specified action.
        /// </summary>
        /// <param name="storeTelemetryEvent">The telemetry event.</param>
        /// <param name="action">The action to be executed.</param>
        internal void Instrument(StoreTelemetryEvent storeTelemetryEvent, Action<StoreTelemetryEvent> action)
        {
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();

                action.Invoke(storeTelemetryEvent);

                stopwatch.Stop();
                storeTelemetryEvent.LatencyMs = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception e)
            {
                // if an exception occurred, check the stopwatch is not still running before capturing the time
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }

                storeTelemetryEvent.LatencyMs = stopwatch.ElapsedMilliseconds;
                storeTelemetryEvent.Exception = e;
                throw;
            }
            finally
            {
                // publish the telemetry event
                this.Notify(storeTelemetryEvent);
            }
        }

        /// <summary>
        /// Provides the ability for subscribers to unsubscribe from store telemetry events.
        /// </summary>
        private class Unsubscriber : IDisposable
        {
            /// <summary>
            /// The observers
            /// </summary>
            private readonly List<IObserver<StoreTelemetryEvent>> observers;

            /// <summary>
            /// The observer
            /// </summary>
            private readonly IObserver<StoreTelemetryEvent> observer;

            /// <summary>
            /// Initializes a new instance of the <see cref="Unsubscriber"/> class.
            /// </summary>
            /// <param name="observers">The observers.</param>
            /// <param name="observer">The observer who subscribed.</param>
            public Unsubscriber(List<IObserver<StoreTelemetryEvent>> observers, IObserver<StoreTelemetryEvent> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            /// <summary>
            /// Performs an unsubscription operation from telemetry events at the time of disposal.
            /// </summary>
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Releases unmanaged and - optionally - managed resources.
            /// </summary>
            /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (ObserverReadWriteLock != null)
                    {
                        ObserverReadWriteLock.EnterWriteLock();
                        try
                        {
                            if (observer != null && observers != null && observers.Contains(observer))
                            {
                                observers.Remove(observer);
                            }
                        }
                        finally
                        {
                            ObserverReadWriteLock.ExitWriteLock();
                        }
                    }
                }
            }
        }
    }
}
