// -----------------------------------------------------------------------
// <copyright file="SupplementalValue.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    internal sealed class SupplementalValue
    {
        /// <summary>
        /// Gets or sets my key name.
        /// </summary>
        public string MyKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the other key name.
        /// </summary>
        public string OtherKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of the key.
        /// </summary>
        public object Value
        {
            get;
            set;
        }
    }
}
