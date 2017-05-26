// -----------------------------------------------------------------------
// <copyright file="Feed.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions for the Feed class.
    /// </summary>
    public abstract partial class Feed
    {
        /// <summary>
        /// Merge data from a base feed into the current feed.
        /// </summary>
        /// <param name="baseFeed">The base feed to merge.</param>
        protected virtual void Merge(Feed baseFeed)
        {
            if (baseFeed == null || baseFeed.GetType() != this.GetType())
            {
                throw new NotSupportedException("Only feeds of the same kind can be merged.");
            }

            string baseName = baseFeed.Namespace + '.' + baseFeed.Name;
            if (string.CompareOrdinal(baseName, this.Base) == 0)
            {
                foreach (PropertyRef key in baseFeed.Keys)
                {
                    if (this.Keys.Any(k => k.Name.Equals(key.Name)) == false)
                    {
                        this.Keys.Add(key);
                    }
                }

                if (this.MaximumDays == null)
                {
                    this.MaximumDays = baseFeed.MaximumDays;
                }

                if (this.Filter == null)
                {
                    this.Filter = baseFeed.Filter;
                }

                foreach (Property property in baseFeed.Properties)
                {
                    if (this.Properties.Any(p => p.Name.Equals(property.Name)) == false)
                    {
                        this.Properties.Add(property);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("The supplied feed is not declared as the base of this feed.");
            }
        }
    }
}
