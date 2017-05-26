// -----------------------------------------------------------------------
// <copyright file="Dimension.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions for the Dimension class.
    /// </summary>
    public partial class Dimension : IHierarchical
    {
        /// <summary>
        /// Merge this instance with all of its base instances.
        /// </summary>
        /// <param name="baseInstance">The current instance's base instance.</param>
        void IHierarchical.Merge(IHierarchical baseInstance)
        {
            this.Merge(baseInstance as Dimension);
        }

        /// <summary>
        /// Merge data from a base dimension into the current dimension.
        /// </summary>
        /// <param name="baseDimension">The base dimension to merge.</param>
        protected virtual void Merge(Dimension baseDimension)
        {
            string baseName = baseDimension.Namespace + '.' + baseDimension.Name;
            if (string.CompareOrdinal(baseName, this.Base) == 0)
            {
                if (this.Keys.Count == 0)
                {
                    this.Keys.AddRange(baseDimension.Keys);
                }

                if (this.DiscoveryMode == DiscoveryModeType.Unset)
                {
                    this.DiscoveryMode = baseDimension.DiscoveryMode;
                }

                foreach (Property property in baseDimension.Properties)
                {
                    if (this.Properties.Any(p => p.Name.Equals(property.Name)) == false)
                    {
                        this.Properties.Add(property);
                    }
                }

                foreach (Constraint constraint in baseDimension.Constraints)
                {
                    if (this.Constraints.Any(p => p.Name.Equals(constraint.Name)) == false)
                    {
                        this.Constraints.Add(constraint);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("The supplied dimension is not declared as the base of this dimension.");
            }
        }
    }
}
