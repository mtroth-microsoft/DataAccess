// -----------------------------------------------------------------------
// <copyright file="IBaseEntity.cs" company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The Base Entity Interface.
    /// </summary>
    public interface IBaseEntity
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        long Id { get; set; }

        /// <summary>
        /// Gets or sets the version data.
        /// </summary>
        byte[] Version { get; set; }

        /// <summary>
        /// Gets or sets the inserted time.
        /// </summary>
        DateTimeOffset InsertedTime { get; set; }

        /// <summary>
        /// Gets or sets the updated time.
        /// </summary>
        DateTimeOffset UpdatedTime { get; set; }
    }
}
