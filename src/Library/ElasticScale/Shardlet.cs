// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

    /// <summary>
    /// Represents a shardlet, the partitioned entity.
    /// </summary>
    public class Shardlet<T> 
        where T : struct
    {
        /// <summary>
        /// Gets or sets the value of the shardlet.
        /// </summary>
        public T Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the status of the shardlet.
        /// </summary>
        public MappingStatus Status
        {
            get;
            set;
        }
    }
}
