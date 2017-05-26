// -----------------------------------------------------------------------
// <copyright file="SequenceExecutor.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    internal sealed class SequenceExecutor<T> : IExecuteTransaction
    {
        /// <summary>
        /// Object to use when locking the instance for write.
        /// </summary>
        private object writeLock = new object();

        /// <summary>
        /// The writer reader for this instance.
        /// </summary>
        private WriterReader writerReader;

        /// <summary>
        /// The bulk writer settings.
        /// </summary>
        private BulkWriterSettings settings;

        /// <summary>
        /// The instance populated during execution.
        /// </summary>
        private T instance;

        /// <summary>
        /// Initializes a new instance of the SequenceExecutor class.
        /// </summary>
        /// <param name="writerReader">The writer reader to execute.</param>
        /// <param name="settings">The bulk writer settings to use.</param>
        public SequenceExecutor(WriterReader writerReader, BulkWriterSettings settings)
        {
            this.writerReader = writerReader;
            this.settings = settings;
            this.instance = (T)writerReader.PeekNextInstance();
        }

        /// <summary>
        /// Read the instance in its current state.
        /// </summary>
        /// <returns>The instance being built up via the sequence.</returns>
        public T ReadInstance()
        {
            lock (this.writeLock)
            {
                return this.instance;
            }
        }

        /// <summary>
        /// Execute the writer reader.
        /// </summary>
        /// <param name="tx">The transaction to use.</param>
        /// <returns>The result of the execution.</returns>
        public object Execute(IDbTransaction tx)
        {
            lock (this.writeLock)
            {
                T output = this.instance;
                bool initialized = false;
                do
                {
                    this.instance = (T)this.writerReader.PeekNextInstance();
                    while (this.writerReader.Read() == true)
                    {
                        if (initialized == true)
                        {
                            this.AugmentReaderFromInstance(this.writerReader.Current);
                        }

                        IEnumerable<WriterReader> supplements = this.writerReader.Current.LoadSupplements();
                        foreach (WriterReader supplement in supplements)
                        {
                            if (supplement.MustRunFirst == true)
                            {
                                object result = this.RunSupplement(supplement, tx);
                                this.AugmentReaderFromSupplement(result, supplement.SupplementedColumn);
                            }
                        }

                        ApplySupplements();
                        this.writerReader.Current.QueryTable.Parameters.Clear();
                        MergeQueryBuilder builder = new MergeQueryBuilder(this.writerReader.Current, this.settings);
                        StoredProcedure<T> procedure = settings.Store.Executor.CompileMerge<T>(
                            builder.Query,
                            settings.Store,
                            settings.ShardIds.EmptyIfNull().SingleOrDefault());

                        IEnumerable<T> results = null;
                        try
                        {
                            results = procedure.Execute(tx);
                            foreach (WriterReader supplement in supplements)
                            {
                                if (supplement.MustRunFirst == false)
                                {
                                    this.AugmentSupplementFromInstance(results.SingleOrDefault(), supplement);
                                    object result = this.RunSupplement(supplement, tx);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (builder.Query.ConcurrencyCheck != null && ex.Message.Contains(builder.Query.ConcurrencyError) == true)
                            {
                                throw new InvalidDataFilterException(builder.Query.ConcurrencyError, ex);
                            }
                            else
                            {
                                throw;
                            }
                        }

                        this.AugmentInstance(results.SingleOrDefault());
                        object next = this.writerReader.PeekNextInstance();
                        if (next == null && initialized == false)
                        {
                            initialized = true;
                        }
                    }
                }
                while (this.writerReader.NextResult() == true);

                this.instance = output;
                return this.instance;
            }
        }

        /// <summary>
        /// Apply any supplements to the current data.
        /// </summary>
        private void ApplySupplements()
        {
            foreach (SupplementalValue value in this.writerReader.SupplementalValues)
            {
                if (this.writerReader.Current.Data.ContainsKey(value.MyKey) == true)
                {
                    this.writerReader.Current.UpdateValue(value.MyKey, value.Value);
                }
            }
        }

        /// <summary>
        /// Add the tracked instance data to the next operation.
        /// </summary>
        /// <param name="set">The writer set to modify.</param>
        private void AugmentReaderFromInstance(WriteSet set)
        {
            // if the write set is a derived type, it should have the same key values.
            if (set.WriteType.IsAssignableFrom(typeof(T)) == true)
            {
                IEnumerable<QueryColumn> keys = set.Columns.Where(p => p.IsKeyColumn);
                foreach (QueryColumn key in keys)
                {
                    object value = TypeCache.GetValue(typeof(T), key.Alias, this.instance);
                    set.UpdateValue(key.Alias, value);
                }
            }
        }

        /// <summary>
        /// Add the result data to the tracked instance.
        /// </summary>
        /// <param name="result">The intermediate result to use when augmenting the instance.</param>
        private void AugmentInstance(T result)
        {
            foreach (QueryColumn column in this.writerReader.Current.Columns)
            {
                object value = null;
                if (typeof(T).GetProperty(column.Alias) == null)
                {
                    continue;
                }
                else if (result != null)
                {
                    value = TypeCache.GetValue(typeof(T), column.Alias, result);
                }
                else if (this.writerReader.Current.Data.ContainsKey(column.Alias) == true)
                {
                    value = this.writerReader.Current.Data[column.Alias];
                }

                if (result != null || value != null)
                {
                    TypeCache.SetValue(typeof(T), column.Alias, this.instance, value);
                }
            }

            IEnumerable<string> names = this.writerReader.Current.LocateSupplementColumnNames();
            foreach (string name in names)
            {
                object value = this.writerReader.Current.Data[name];
                if (result != null || value != null)
                {
                    TypeCache.SetValue(typeof(T), name, this.instance, value);
                }
            }
        }

        /// <summary>
        /// Augument navigation data from a supplemental object.
        /// </summary>
        /// <param name="result">The navigation data.</param>
        /// <param name="supplementedColumn">The column being supplemented by navigation.</param>
        private void AugmentReaderFromSupplement(object result, QueryColumn supplementedColumn)
        {
            object data = this.writerReader.Current.Data[supplementedColumn.Alias];
            if (data != result)
            {
                throw new InvalidOperationException("Linked objects don't match.");
            }

            QueryTable it;
            Dictionary<string, string> cols = TypeCache.GetOverride(
                this.writerReader.Current.WriteType, 
                supplementedColumn.Alias, 
                out it);

            foreach (string key in cols.Keys)
            {
                object value = TypeCache.GetValue(supplementedColumn.ElementType, cols[key], data);
                this.writerReader.Current.UpdateValue(key, value);
            }
        }

        /// <summary>
        /// Add the result data to the tracked instance.
        /// </summary>
        /// <param name="result">The intermediate result to use when augmenting the instance.</param>
        /// <param name="supplement">The supplemental reader.</param>
        private void AugmentSupplementFromInstance(T result, WriterReader supplement)
        {
            QueryTable it;
            Dictionary<string, string> cols = TypeCache.GetOverride(
                this.writerReader.Current.WriteType,
                supplement.SupplementedColumn.Alias,
                out it);

            foreach (string key in cols.Keys)
            {
                SupplementalValue value = new SupplementalValue();
                value.MyKey = cols[key];
                value.OtherKey = key;
                value.Value = TypeCache.GetValue(typeof(T), key, result);
                supplement.AddSupplementalValue(value);
            }
        }

        /// <summary>
        /// Execute a supplemental reader.
        /// </summary>
        /// <param name="supplement">The reader to execute.</param>
        /// <param name="tx">The current transaction.</param>
        /// <returns>The result of execution.</returns>
        private object RunSupplement(WriterReader supplement, IDbTransaction tx)
        {
            Type type = supplement.PeekNextInstance().GetType();
            Type generic = typeof(SequenceExecutor<>).MakeGenericType(type);
            IExecuteTransaction executor = Activator.CreateInstance(generic, supplement, this.settings) as IExecuteTransaction;

            return executor.Execute(tx);
        }
    }
}
