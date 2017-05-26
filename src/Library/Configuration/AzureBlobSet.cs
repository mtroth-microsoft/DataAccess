// -----------------------------------------------------------------------
// <copyright company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    using System;

    /// <summary>
    /// Extensions for the AzureBlobSet class
    /// </summary>
    public partial class AzureBlobSet : IHierarchical
    {
        /// <summary>
        /// Get the frequency time span.
        /// </summary>
        public TimeSpan FrequencyTimeSpan
        {
            get
            {
                switch (this.StreamUpdateFrequency)
                {
                    case TimePeriodType.Daily:
                        return TimeSpan.FromDays(1);
                    case TimePeriodType.Weekly:
                        return TimeSpan.FromDays(7);
                    default:
                        return TimeSpan.FromHours(1);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the blob set is partitioned.
        /// </summary>
        public bool IsPartitioned
        {
            get
            {
                return this.Partition != null;
            }
        }

        /// <summary>
        /// Rounds up a date time offset to the model granularity 
        /// </summary>
        /// <param name="dateTimeToRound">the date time offset to round</param>
        /// <returns>The rounded up value</returns>
        public DateTimeOffset RoundUpToGranularity(DateTimeOffset dateTimeToRound)
        {
            DateTimeOffset roundedDateTime;
            if (dateTimeToRound == null)
            {
                throw new ArgumentNullException("dateTimeToRound");
            }

            switch (this.StreamUpdateFrequency)
            {
                case TimePeriodType.Daily:
                    roundedDateTime = (dateTimeToRound - dateTimeToRound.TimeOfDay).AddDays(1);
                    break;

                case TimePeriodType.Hourly:
                    roundedDateTime = new DateTimeOffset(dateTimeToRound.Year, dateTimeToRound.Month, dateTimeToRound.Day, dateTimeToRound.Hour, 0, 0, dateTimeToRound.Offset);
                    roundedDateTime = roundedDateTime.AddHours(1);
                    break;

                default:
                    throw new InvalidOperationException(string.Format("Failed round timestamp granularity. Unrecognized frequency: {0}", this.StreamUpdateFrequency));
            }

            return roundedDateTime;
        }

        /// <summary>
        /// Gets the data blob url
        /// </summary>
        /// <param name="partitionId">The partition identifier</param>
        /// <param name="timestamp">The blob timestamp</param>
        /// <returns>The url</returns>
        public string GetDataBlob(long? partitionId, DateTimeOffset timestamp)
        {
            if (this.IsPartitioned && partitionId == null)
            {
                throw new NotSupportedException();
            }
            else if (this.IsPartitioned == false && partitionId != null)
            {
                throw new NotSupportedException();
            }

            return this.ReplaceVariables(this.UrlFormat, timestamp, partitionId);
        }

        /// <summary>
        /// Gets the data blobs folder
        /// </summary>
        /// <param name="timestamp">The blobs timestamps</param>
        /// <returns>The url</returns>
        public string GetDataFolder(DateTimeOffset timestamp)
        {
            int index = this.UrlFormat.LastIndexOf("/");
            return this.ReplaceVariables(this.UrlFormat.Substring(0, index), timestamp, 0);
        }

        /// <summary>
        /// Gets the metadata blob url
        /// </summary>
        /// <param name="timestamp">The blob timestamp</param>
        /// <returns>The url</returns>
        public string GetMetadataBlob(DateTimeOffset timestamp)
        {
            return this.ReplaceVariables(this.MetadataUrlFormat, timestamp, 0);
        }

        /// <summary>
        /// Gets the metadata blobs folder
        /// </summary>
        /// <param name="timestamp">The blobs timestamp</param>
        /// <returns>The url</returns>
        public string GetMetadataFolder(DateTimeOffset timestamp)
        {
            int index = this.MetadataUrlFormat.LastIndexOf("/");
            return this.ReplaceVariables(this.MetadataUrlFormat.Substring(0, index), timestamp, 0);
        }

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
            AzureBlobSet baseBlobSet = baseFeed as AzureBlobSet;
            if (string.IsNullOrEmpty(this.Prefix) == true)
            {
                this.Prefix = baseBlobSet.Prefix;
            }

            if (string.IsNullOrEmpty(this.UrlFormat) == true)
            {
                this.UrlFormat = baseBlobSet.UrlFormat;
            }

            if (string.IsNullOrEmpty(this.MetadataUrlFormat) == true)
            {
                this.MetadataUrlFormat = baseBlobSet.MetadataUrlFormat;
            }

            if (this.StreamUpdateFrequency == default(TimePeriodType))
            {
                this.StreamUpdateFrequency = baseBlobSet.StreamUpdateFrequency;
            }

            if (this.Partition == null)
            {
                this.Partition = baseBlobSet.Partition;
            }
        }

        /// <summary>
        /// Replaces the variables that may be present in a string
        /// </summary>
        /// <param name="inputString">The string that may contain variables</param>
        /// <param name="timestamp">The blob timestamp</param>
        /// <param name="partitionId">The partition identifier</param>
        /// <returns></returns>
        private string ReplaceVariables(string inputString, DateTimeOffset timestamp, long? partitionId)
        {
            string returnString = inputString
                .Replace("{YYYY}", timestamp.Year.ToString("0000"))
                .Replace("{MM}", timestamp.Month.ToString("00"))
                .Replace("{DD}", timestamp.Day.ToString("00"))
                .Replace("{HH}", timestamp.Hour.ToString("00"))
                .Replace("{Prefix}", this.Prefix);

            if (returnString.Contains("{PartitionId}"))
            {
                returnString = returnString.Replace("{PartitionId}", partitionId.Value.ToString());
            }

            return returnString;
        }
    }
}