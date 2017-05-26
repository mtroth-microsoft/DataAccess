// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Generic interface for all stores.
    /// </summary>
    public interface IStore
    {
        /// <summary>
        /// The command timeout to use.
        /// </summary>
        int SqlCommandTimeout { get; set; }

        /// <summary>
        /// Gets the collection of connection strings.
        /// Will return only 1 connection string if querying one store;
        /// Otherwise, will return a collection of connection strings in case of a federated query
        /// </summary>
        /// <returns>Collection of connection strings</returns>
        IEnumerable<string> GetConnectionStrings();

        /// <summary>
        /// Execute stored procedure non query
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>Number of rows affected</returns>
        int Execute(
            StoredProcedureNonQuery storedProcedure,
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output);

        /// <summary>
        /// Execute stored procedure non query
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Collection of input parameters</param>
        /// <param name="tx">The current transaction.</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>Number of rows affected</returns>
        int Execute(
            StoredProcedureNonQuery storedProcedure,
            IEnumerable<IParameter> parameters,
            IDbTransaction tx,
            out IDictionary<string, IParameter> output);

        /// <summary>
        /// Execute stored procedure
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Collection of input parameters</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The data set</returns>
        DataSet GetData(
            StoredProcedureBase storedProcedure,
            IEnumerable<IParameter> parameters,
            out IDictionary<string, IParameter> output);

        /// <summary>
        /// Execute stored procedure
        /// </summary>
        /// <param name="storedProcedure">The stored procedure</param>
        /// <param name="parameters">Collection of input parameters</param>
        /// <param name="tx">The current transaction.</param>
        /// <param name="output">The output parameters.</param>
        /// <returns>The data set</returns>
        DataSet GetData(
            StoredProcedureBase storedProcedure,
            IEnumerable<IParameter> parameters,
            IDbTransaction tx,
            out IDictionary<string, IParameter> output);
    }
}