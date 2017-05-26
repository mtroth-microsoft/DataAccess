// -----------------------------------------------------------------------
// <copyright file="IChangeLog.cs" company="Lensgrinder, Ltd.">
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
    /// The change log interface.
    /// </summary>
    public interface IChangeLog
    {
        /// <summary>
        /// Gets or sets the pre value.
        /// </summary>
        string Pre { get; set; }

        /// <summary>
        /// Gets or sets the post value.
        /// </summary>
        string Post { get; set; }

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        long Id { get; set; }

        /// <summary>
        /// Gets or sets the modified by.
        /// </summary>
        IUser ModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the change time.
        /// </summary>
        DateTimeOffset ChangeTime { get; set; }

        /// <summary>
        /// Gets or sets the change log type.
        /// </summary>
        int ChangeLogType { get; set; }

        /// <summary>
        /// Gets or sets the changing entity.
        /// </summary>
        IBaseEntity Entity { get; set; }

        /// <summary>
        /// Gets or sets the related entity, for navigational property changes.
        /// </summary>
        IBaseEntity RelatedEntity { get; set; }
    }
}
