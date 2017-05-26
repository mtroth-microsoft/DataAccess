// -----------------------------------------------------------------------
// <copyright file="BatchRequestType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to BatchRequestType in model.
    /// </summary>
    public partial class BatchRequestType
    {
        /// <summary>
        /// The content format.
        /// </summary>
        private const string ContentType = "multipart/mixed; boundary=\"batch_{0}\"";

        /// <summary>
        /// the post format.
        /// </summary>
        private const string PostFormat = "{0}/$batch";

        /// <summary>
        /// The batch id.
        /// </summary>
        private Guid id = Guid.NewGuid();

        /// <summary>
        /// Post the current batch request.
        /// </summary>
        /// <param name="parameters">The parameter values to populate.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns>The raw response for the batch post.</returns>
        public IEnumerable<BatchResponse> Post(Dictionary<string, object> parameters, AuthParameters authParameters = null)
        {
            if (this.HostAndODataPath == null)
            {
                throw new ArgumentNullException("HostAndODataPath");
            }

            string url = string.Format(PostFormat, this.HostAndODataPath);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.UseDefaultCredentials = true;
            request.PreAuthenticate = true;
            request.SetAuthentication(authParameters);
            request.ContentType = string.Format(ContentType, this.id.ToString());

            StringBuilder builder = new StringBuilder();
            this.SerializeBody(builder, parameters);
            byte[] byteArray = Encoding.UTF8.GetBytes(builder.ToString());
            request.ContentLength = byteArray.Length;

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            string response = ODataQueryType.ExecuteRequest(request);

            return BatchResponse.Parse(response);
        }

        /// <summary>
        /// Extract an instance from an xml reader.
        /// </summary>
        /// <param name="reader">The xmlreader to introspect.</param>
        internal void Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
            }

            reader.MoveToContent();
            this.Name = reader.GetAttribute("Name");
            this.Namespace = reader.GetAttribute("Namespace");
            string url = reader.GetAttribute("HostAndODataPath");
            if (string.IsNullOrEmpty(url) == false)
            {
                this.HostAndODataPath = new Uri(url);
            }

            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Request")
                {
                    BatchedQueryType request = new BatchedQueryType();
                    request.Deserialize(reader.ReadSubtree());
                    this.Requests.Add(request);
                }
                else if (reader.IsStartElement() == true &&
                    reader.LocalName == "ChangeSet")
                {
                    ChangeSetType changeset = new ChangeSetType();
                    changeset.Deserialize(reader.ReadSubtree());
                    this.Requests.Add(changeset);
                }
            }
        }

        /// <summary>
        /// Serialize this instance to a json batch post.
        /// </summary>
        /// <param name="parameters">The parameter values to populate.</param>
        /// <param name="debug">True if running in debug mode, otherwise false.</param>
        /// <returns>The serialized string.</returns>
        internal string Serialize(Dictionary<string, object> parameters, bool debug)
        {
            StringBuilder builder = new StringBuilder();
            this.SerializeHeaders(builder, debug);
            this.SerializeBody(builder, parameters);

            return builder.ToString();
        }

        /// <summary>
        /// Serialize the headers for the current request.
        /// </summary>
        /// <param name="builder">The string builder to populate.</param>
        /// <param name="debug">True if running in debug mode, otherwise false.</param>
        private void SerializeHeaders(StringBuilder builder, bool debug)
        {
            if (this.HostAndODataPath.ToString().EndsWith("/") == true ||
                this.HostAndODataPath.ToString().EndsWith("$batch") == true)
            {
                throw new ArgumentException("The HostAndODataPath should not end with a slash or include the $batch keyword.");
            }

            if (debug == true)
            {
                builder.AppendFormat("POST {0} {1}", string.Format(PostFormat, this.HostAndODataPath), "HTTP/1.1");
                builder.AppendLine();
            }

            builder.Append("Content-Type: ");
            builder.AppendFormat(ContentType, this.id.ToString());
            builder.AppendLine();
            builder.AppendFormat("Host: {0}:{1}", this.HostAndODataPath.Host, this.HostAndODataPath.Port);
            builder.AppendLine();
            builder.AppendLine("Content-Length: {contentlength}");
            builder.AppendLine();
        }

        /// <summary>
        /// Serialize the body for the current request.
        /// </summary>
        /// <param name="builder">The string builder to populate.</param>
        /// <param name="parameters">The parameter values to populate.</param>
        private void SerializeBody(
            StringBuilder builder,
            Dictionary<string, object> parameters)
        {
            int initial = builder.Length;
            bool hasChangesets = this.Requests.Where(p => p.GetType() == typeof(ChangeSetType)).Any();

            foreach (QueryType query in this.Requests)
            {
                IBatchable batchable = query as IBatchable;
                if (batchable == null)
                {
                    throw new NotSupportedException(string.Format("This is not batchable query type: {0}", query.GetType().Name));
                }

                string request = batchable.SerializeForBatch(parameters);
                builder.AppendLine("--batch_" + this.id);
                if (hasChangesets == false)
                {
                    builder.AppendLine("Content-Type: application/http");
                    builder.AppendLine("Content-Transfer-Encoding: binary");
                    builder.AppendLine();
                }

                builder.AppendLine(request);
            }

            builder.Append("--batch_" + this.id + "--");
            builder.Replace("{host}", this.HostAndODataPath.Host + ':' + this.HostAndODataPath.Port);
            builder.Replace("{route}", this.HostAndODataPath.AbsolutePath);
            builder.Replace("{contentlength}", (builder.Length - initial).ToString());
        }
    }
}
