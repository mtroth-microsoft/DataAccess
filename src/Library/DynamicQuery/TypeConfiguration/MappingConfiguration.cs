// -----------------------------------------------------------------------
// <copyright file="MappingConfiguration.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;

    /// <summary>
    /// Base class for mapping configuration.
    /// </summary>
    public abstract class MappingConfiguration
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        internal string TableName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the schema name.
        /// </summary>
        internal string SchemaName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of left names.
        /// </summary>
        internal List<string> LeftNames
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of right names.
        /// </summary>
        internal List<string> RightNames
        {
            get;
            private set;
        }

        /// <summary>
        /// Maps the table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The schema of the table.</param>
        public void ToTable(string tableName, string schemaName)
        {
            this.TableName = tableName;
            this.SchemaName = schemaName;
        }

        /// <summary>
        /// Map the left side keys.
        /// </summary>
        /// <param name="names">The list of names of the keys.</param>
        public void MapLeftKey(params string[] names)
        {
            this.LeftNames = new List<string>(names);
        }

        /// <summary>
        /// Map th eright side keys.
        /// </summary>
        /// <param name="names">The list of names of the keys.</param>
        public void MapRightKey(params string[] names)
        {
            this.RightNames = new List<string>(names);
        }
    }
}
