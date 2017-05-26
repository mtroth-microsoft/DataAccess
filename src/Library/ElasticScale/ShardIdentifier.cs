// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

    /// <summary>
    /// Represents a shard, the information required to find a database in elastic scale
    /// </summary>
    public class ShardIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShardIdentifier"/> class.
        /// </summary>
        /// <param name="dataSource">The dataSource name containing the database.</param>
        /// <param name="catalog">Name of the database.</param>
        /// <param name="port">The port number to use.</param>
        public ShardIdentifier(string dataSource, string catalog, int port)
        {
            if (string.IsNullOrEmpty(dataSource))
            {
                throw new ArgumentNullException("dataSource", "To fully describe a shard, a dataSource name must be provided.");
            }

            if (string.IsNullOrEmpty(catalog))
            {
                throw new ArgumentNullException("catalog", "To fully describe a shard, a database name must be provided.");
            }

            this.DataSource = dataSource;
            this.Catalog = catalog;
            this.Port = port;
            this.Shardlets = new HashSet<Shardlet<int>>();
        }

        /// <summary>
        /// Gets the dataSource where the database is located.
        /// </summary>
        public string DataSource
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string Catalog
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the port number.
        /// </summary>
        public int Port
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of associated shardlets.
        /// </summary>
        public ICollection<Shardlet<int>> Shardlets
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the elastic scale shard.
        /// </summary>
        internal Shard Shard
        {
            get;
            set;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Datasource = {0}, Catalog = {1}, Port = {2}", this.DataSource, this.Catalog, this.Port);
        }

        /// <summary>
        /// Override the equals function for the shard identifier.
        /// </summary>
        /// <param name="obj">The other object to compare this one to.</param>
        /// <returns>True if two objects are equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (object.Equals(obj, null) == true)
            {
                return false;
            }

            ShardIdentifier si = obj as ShardIdentifier;
            if (object.Equals(si, null) == true)
            {
                return false;
            }

            return string.Compare(this.Catalog, si.Catalog, true) == 0 &&
                string.Compare(this.DataSource, si.DataSource, true) == 0 &&
                this.Port == si.Port;
        }

        /// <summary>
        /// Override the get hash code function for the shard identifier.
        /// </summary>
        /// <returns>The hash code for the current shard identifer.</returns>
        public override int GetHashCode()
        {
            return this.Catalog.GetHashCode() ^ this.DataSource.GetHashCode() ^ this.Port.GetHashCode();
        }
    }
}
