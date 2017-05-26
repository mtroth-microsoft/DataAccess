// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// Derived class for Default Database Type
    /// </summary>
    internal class DefaultStoreType : DatabaseType
    {
        /// <summary>
        /// Gets a value indicating whether the database is federated.
        /// True if database is federated i.e. it has more than one shard; otherwise, false. The default is false.
        /// </summary>
        public override bool Federated
        {
            get
            {
                return true;
            }
        }
    }
}
