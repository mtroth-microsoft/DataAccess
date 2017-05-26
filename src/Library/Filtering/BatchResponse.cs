// -----------------------------------------------------------------------
// <copyright file="BatchResponse.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// The container class for responses from the batch post operation.
    /// </summary>
    public sealed class BatchResponse
    {
        /// <summary>
        /// Prevents the initialization an instance of the BatchResponse class.
        /// </summary>
        private BatchResponse()
        {
        }

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        public HttpStatusCode ResponseStatusCode
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the response status number.
        /// </summary>
        public int ResponseStatusNumber
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the response payload in json.
        /// </summary>
        public string Payload
        {
            get;
            private set;
        }

        /// <summary>
        /// Parse the response json into batch responses.
        /// </summary>
        /// <param name="json">The json to parse.</param>
        /// <returns>The list of responses.</returns>
        internal static List<BatchResponse> Parse(string json)
        {
            bool hasChangesets = json.Contains("--changeset");
            List<BatchResponse> responses = new List<BatchResponse>();
            using (StringReader reader = new StringReader(json))
            {
                bool foundJson = false;
                string line;
                StringBuilder body = new StringBuilder();
                BatchResponse response = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (IsResponseStart(line, hasChangesets) == true)
                    {
                        if (response != null && response.ResponseStatusNumber != 0)
                        {
                            response.Payload = body.ToString();
                            responses.Add(response);
                            body.Clear();
                        }

                        foundJson = false;
                        response = new BatchResponse();
                    }
                    else if (line.Length > 0 && line[0] == '{' && foundJson == false)
                    {
                        body.AppendLine(line);
                        foundJson = true;
                    }
                    else if (foundJson == true)
                    {
                        body.AppendLine(line);
                    }
                    else if (line.StartsWith("HTTP/") == true)
                    {
                        string[] codes = line.Split(' ');
                        string statusCodeToParse = null;
                        for (int i = 2; i < codes.Length; i++)
                        {
                            statusCodeToParse += codes[i];
                        }

                        HttpStatusCode statusCode;
                        if (Enum.TryParse<HttpStatusCode>(statusCodeToParse, out statusCode) == true)
                        {
                            response.ResponseStatusCode = statusCode;
                        }

                        int statusNumber;
                        if (int.TryParse(codes[1], out statusNumber) == true)
                        {
                            response.ResponseStatusNumber = statusNumber;
                        }
                    }
                }

                if (response == null && body.Length > 0)
                {
                    response = new BatchResponse();
                    response.Payload = body.ToString();
                    responses.Add(response);
                }
            }

            return responses;
        }

        /// <summary>
        /// Helper to indicate whether a line is the start of a response.
        /// </summary>
        /// <param name="line">The line to evaluate.</param>
        /// <param name="hasChangesets">True if there are changesets, otherwise false.</param>
        /// <returns>True if line is the start of a response, otherwise false.</returns>
        private static bool IsResponseStart(string line, bool hasChangesets)
        {
            if (line.Length > 0)
            {
                if (hasChangesets == true && line.StartsWith("--changeset") == true)
                {
                    return true;
                }
                else if (hasChangesets == false && line.StartsWith("--batch") == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
