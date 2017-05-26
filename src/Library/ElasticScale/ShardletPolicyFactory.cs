// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// The factory class for creating shardlet policies.
    /// </summary>
    internal static class ShardletPolicyFactory
    {
        /// <summary>
        /// Create the relevant policy.
        /// </summary>
        /// <param name="databaseType">The store type.</param>
        /// <returns>The relevant policy.</returns>
        public static IAddShardletPolicy Create(DatabaseType databaseType)
        {
            if (databaseType != null && databaseType.ShardletPolicy != null)
            {
                return databaseType.ShardletPolicy;
            }
            else if (databaseType == null || databaseType.Federated == true)
            {
                return new LeastPopulatedPolicy();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
