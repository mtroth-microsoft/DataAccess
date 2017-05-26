// -----------------------------------------------------------------------
// <copyright file="AggregateResult.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AggregateResult
    {
        /// <summary>
        /// Initializes a new instance of the AggregateResult class.
        /// </summary>
        public AggregateResult()
        {
            this.DynamicProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the aggregate key value.
        /// </summary>
        [Key]
        public int AggregateKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the dynamic properties.
        /// </summary>
        public IDictionary<string, object> DynamicProperties
        {
            get;
            set;
        }
    }
}
