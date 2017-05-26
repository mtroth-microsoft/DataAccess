// -----------------------------------------------------------------------
// <copyright company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    /// <summary>
    /// Extensions for the SourceTargetPath class.
    /// </summary>
    public partial class SourceTargetPath
    {
        /// <summary>
        /// Gets or sets the source
        /// </summary>
        public FeedReference Source
        {
            get
            {
                return this.Path[0];
            }
            set
            {
                this.Path[0] = value;
            }
        }

        /// <summary>
        /// Gets or sets the target
        /// </summary>
        public FeedReference Target
        {
            get
            {
                return this.Path[1];
            }
            set
            {
                this.Path[1] = value;
            }
        }
    }
}