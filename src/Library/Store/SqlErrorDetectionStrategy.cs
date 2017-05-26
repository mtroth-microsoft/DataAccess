// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.ComponentModel;
    using System.Data.SqlClient;
    using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

    /// <summary>
    /// Extended sql transient error detection strategy that performs additional transient error
    /// checks besides the ones done by the enterprise library.
    /// </summary>
    internal sealed class SqlErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        /// <summary>
        /// Enterprise transient error detection strategy.
        /// </summary>
        private SqlDatabaseTransientErrorDetectionStrategy sqltransientErrorDetectionStrategy = new SqlDatabaseTransientErrorDetectionStrategy();

        /// <summary>
        /// Checks with enterprise library's default handler to see if the error is transient, additionally checks
        /// for such errors using the code in the in <see cref="IsTransientException"/> function.
        /// </summary>
        /// <param name="ex">Exception being checked.</param>
        /// <returns>true if exception is considered transient; false, otherwise.</returns>
        public bool IsTransient(Exception ex)
        {
            return this.sqltransientErrorDetectionStrategy.IsTransient(ex) || IsTransientException(ex);
        }

        /// <summary>
        /// Detects transient errors not currently considered as transient by the enterprise library's strategy.
        /// </summary>
        /// <param name="ex">Input exception.</param>
        /// <returns>true if exception is considered transient, false otherwise.</returns>
        private static bool IsTransientException(Exception ex)
        {
            SqlException se = ex as SqlException;

            if (se != null && se.InnerException != null)
            {
                Win32Exception we = se.InnerException as Win32Exception;

                if (we != null)
                {
                    switch (we.NativeErrorCode)
                    {
                        case 0x102:
                            // Transient wait expired error resulting in timeout
                            return true;
                        case 0x121:
                            // Transient semaphore wait expired error resulting in timeout
                            return true;
                    }
                }
            }

            return false;
        }
    }
}