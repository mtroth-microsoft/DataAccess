// -----------------------------------------------------------------------
// <copyright file="BulkWriterSettings.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Data;

    internal interface IExecuteTransaction
    {
        /// <summary>
        /// Execute with a transaction.
        /// </summary>
        /// <param name="tx">The transaction to use.</param>
        /// <returns>The result.</returns>
        object Execute(IDbTransaction tx);
    }
}
