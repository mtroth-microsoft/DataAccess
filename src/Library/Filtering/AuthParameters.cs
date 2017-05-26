// -----------------------------------------------------------------------
// <copyright company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    /// <summary>
    /// This class encaspulates parameters used for authentication
    /// </summary>
    public sealed class AuthParameters
    {
        /// <summary>
        /// Gets or sets BearerToken
        /// </summary>
        public string BearerToken
        {
            get;
            set;
        }
    }
}
