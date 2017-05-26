// -----------------------------------------------------------------------
// <copyright company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Net;

    /// <summary>
    /// Extension class for HttpWebRequest
    /// </summary>
    internal static class HttpWebRequestExtensions
    {
        /// <summary>
        /// Set HttpWebRequest properties for authentication
        /// </summary>
        /// <param name="request">The http request</param>
        /// <param name="authParameters">The authentication parameters</param>
        public static void SetAuthentication(this HttpWebRequest request, AuthParameters authParameters)
        {
            if (authParameters == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(authParameters.BearerToken) == false)
            {
                request.Headers.Add("Authorization", authParameters.BearerToken);
            }
        }
    }
}
