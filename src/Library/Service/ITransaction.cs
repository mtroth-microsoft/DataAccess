// -----------------------------------------------------------------------
// <copyright file="ITransaction.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The interface for transactions.
    /// </summary>
    public interface ITransaction
    {
        /// <summary>
        /// Gets the transaction id for the current transaction.
        /// </summary>
        Guid TransactionId
        {
            get;
        }

        /// <summary>
        /// Commit the transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollback the transaction.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Adds the object to the transaction.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <param name="entity">The entity.</param>
        void AddObject(int id, object entity);

        /// <summary>
        /// Reads the object from the transaction.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <returns>The entity.</returns>
        object GetObject(int id);

        /// <summary>
        /// Read the list of keys in the transaction.
        /// </summary>
        /// <returns>The list of keys.</returns>
        IEnumerable<int> GetKeys();
    }
}
