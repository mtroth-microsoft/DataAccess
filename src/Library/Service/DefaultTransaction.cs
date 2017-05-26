// -----------------------------------------------------------------------
// <copyright file="DefaultTransaction.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Internal implementation of the ITransaction interface.
    /// </summary>
    internal class DefaultTransaction : ITransaction
    {
        /// <summary>
        /// Private storage of the datasource instance.
        /// </summary>
        private IDatasource datasource;

        /// <summary>
        /// Cache of trasacted objects.
        /// </summary>
        private ConcurrentDictionary<int, object> cache =
            new ConcurrentDictionary<int, object>();

        /// <summary>
        /// Initializes a new instance of the DefaultTransaction class.
        /// </summary>
        /// <param name="datasource">The datasource to transact against.</param>
        public DefaultTransaction(IDatasource datasource)
        {
            this.datasource = datasource;
            this.TransactionId = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the transaction id for the current transaction.
        /// </summary>
        public Guid TransactionId
        {
            get;
            private set;
        }

        /// <summary>
        /// Commit the transaction.
        /// </summary>
        public void Commit()
        {
            this.datasource.SaveChanges();
        }

        /// <summary>
        /// Rollback the transaction.
        /// </summary>
        public void Rollback()
        {
        }

        /// <summary>
        /// Adds the object to the transaction.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <param name="entity">The entity.</param>
        public void AddObject(int id, object entity)
        {
            this.cache.TryAdd(id, entity);
        }

        /// <summary>
        /// Reads the object from the transaction.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <returns>The entity.</returns>
        public object GetObject(int id)
        {
            object value;
            this.cache.TryGetValue(id, out value);

            return value;
        }

        /// <summary>
        /// Read the list of keys in the transaction.
        /// </summary>
        /// <returns>The list of keys.</returns>
        public IEnumerable<int> GetKeys()
        {
            return this.cache.Keys;
        }
    }
}
