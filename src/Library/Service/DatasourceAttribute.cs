// -----------------------------------------------------------------------
// <copyright file="DatasourceAttribute.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// <summary>The file summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// The datasource attribute class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DatasourceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the DatasourceAttribute class.
        /// </summary>
        /// <param name="activeModel">The active model.</param>
        public DatasourceAttribute(string activeModel)
        {
            this.ActiveModel = activeModel;
        }

        /// <summary>
        /// Gets the active model.
        /// </summary>
        public string ActiveModel
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the database type.
        /// </summary>
        public Type DatabaseType
        {
            get;
            set;
        }
    }
}
