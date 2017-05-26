// -----------------------------------------------------------------------
// <copyright file="EntityType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    using System.Linq;

    /// <summary>
    /// Extensions for the EntityType class.
    /// </summary>
    public partial class EntityType : IHierarchical
    {
        /// <summary>
        /// Merge this instance with all of its base instances.
        /// </summary>
        /// <param name="baseInstance">The current instance's base instance.</param>
        void IHierarchical.Merge(IHierarchical baseInstance)
        {
            this.Merge(baseInstance as Feed);
        }

        /// <summary>
        /// Merge data from a base feed into the current feed.
        /// </summary>
        /// <param name="baseFeed">The base feed to merge.</param>
        protected override void Merge(Feed baseFeed)
        {
            base.Merge(baseFeed);
            EntityType baseEntityType = baseFeed as EntityType;

            if (string.IsNullOrEmpty(this.EntitySet) == true)
            {
                this.EntitySet = baseEntityType.EntitySet;
            }

            if (this.Url == null)
            {
                this.Url = baseEntityType.Url;
            }

            if (this.Authentication == null)
            {
                this.Authentication = baseEntityType.Authentication;
            }

            if (this.Watermark == null)
            {
                this.Watermark = baseEntityType.Watermark;
            }

            if (this.Operation == null)
            {
                this.Operation = baseEntityType.Operation;
            }

            foreach (NavigationProperty property in baseEntityType.NavigationProperties)
            {
                if (this.NavigationProperties.Any(p => p.Name.Equals(property.Name)) == false)
                {
                    this.NavigationProperties.Add(property);
                }
            }
        }
    }
}
