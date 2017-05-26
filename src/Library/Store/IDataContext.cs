// -----------------------------------------------------------------------
// <copyright file="IDataContext.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Interface for managing store connectivity.
    /// </summary>
    public interface IDataContext : IDisposable
    {
        /// <summary>
        /// Gets the interface to execute queries.
        /// </summary>
        IStore Store { get; }
    }
}