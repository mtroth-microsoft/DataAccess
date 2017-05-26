// -----------------------------------------------------------------------
// <copyright file="Stream.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    /// <summary>
    /// Extensions for the Stream class.
    /// </summary>
    public partial class Stream : IHierarchical
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
            Stream baseStream = baseFeed as Stream;
            if (string.IsNullOrEmpty(this.Prefix) == true)
            {
                this.Prefix = baseStream.Prefix;
            }

            if (string.IsNullOrEmpty(this.UrlFormat) == true)
            {
                this.UrlFormat = baseStream.UrlFormat;
            }

            if (string.IsNullOrEmpty(this.VcPath) == true)
            {
                this.VcPath = baseStream.VcPath;
            }

            if (string.IsNullOrEmpty(this.VcRoot) == true)
            {
                this.VcRoot = baseStream.VcRoot;
            }

            if (this.StreamUpdateFrequency == null)
            {
                this.StreamUpdateFrequency = baseStream.StreamUpdateFrequency;
            }

            if (this.Partition == null)
            {
                this.Partition = baseStream.Partition;
            }

            if (this.ExportMode == default(ExportMode))
            {
                this.ExportMode = baseStream.ExportMode;
            }
        }
    }
}
