// -----------------------------------------------------------------------
// <copyright file="BatchedQueryType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to BatchedQueryType in model.
    /// </summary>
    public partial class BatchedQueryType : IBatchable
    {
        /// <summary>
        /// An already serialized url that is to be used for serializing the query.
        /// This is meant to be a backdoor into the class to allow for rapid debug
        /// or easy incorporation of odata queries into the batch request mechanism.
        /// </summary>
        private string hardCodedUrl;

        /// <summary>
        /// Override of the tostring method outputs the basic odata query.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.Serialize(null);
        }

        /// <summary>
        /// Initialize the current instance with a completely serialized url.
        /// </summary>
        /// <param name="url">The url to use.</param>
        internal void InitializeWithUrl(Uri url)
        {
            this.hardCodedUrl = Uri.UnescapeDataString(url.AbsoluteUri);
        }

        /// <summary>
        /// Extract an instance from an xml reader.
        /// </summary>
        /// <param name="reader">The xmlreader to introspect.</param>
        internal override void Deserialize(XmlReader reader)
        {
            if (reader.Read() == true)
            {
                this.Method = (MethodType)Enum.Parse(typeof(MethodType), reader.GetAttribute("Method"));
                base.Deserialize(reader);
                if (reader.LocalName == "Payload")
                {
                    reader.Read();
                    this.Payload = reader.Value;
                }
            }
        }

        /// <summary>
        /// Override of base class serialize behavior.
        /// </summary>
        /// <param name="parameters">The parameters to replace.</param>
        /// <returns>The serialized query.</returns>
        internal override string Serialize(Dictionary<string, object> parameters)
        {
            return "http://{host}{route}" + base.Serialize(parameters);
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <param name="parameters">The populated parameters.</param>
        /// <returns>The serialized string.</returns>
        internal string SerializeForBatch(Dictionary<string, object> parameters)
        {
            StringBuilder builder = new StringBuilder();
            string method = this.Method.ToString().ToUpperInvariant();
            string url = "http://{host}{route}" + base.Serialize(parameters);
            if (this.Method == MethodType.CreateRef)
            {
                method = "POST";
            }
            else if (this.Method == MethodType.DeleteRef)
            {
                method = "DELETE";
            }

            builder.Append(method);
            builder.Append(" ");
            builder.Append(this.hardCodedUrl ?? url);
            builder.Append(" ");
            builder.Append("HTTP/1.1");
            builder.AppendLine();

            if (this.Payload != null)
            {
                builder.AppendLine("Content-Type: application/json; charset=utf-8");
                if (this.Method == MethodType.Put || this.Method == MethodType.Patch || this.Method == MethodType.Post)
                {
                    builder.AppendLine("Prefer: return=representation");
                }

                builder.AppendLine();
                builder.Append(this.Payload.ToString());
            }
            else
            {
                builder.AppendLine();
            }

            return builder.ToString();
        }

        /// <summary>
        /// Explicit implementation of deserialize.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        void IBatchable.Deserialize(XmlReader reader)
        {
            this.Deserialize(reader);
        }

        /// <summary>
        /// Explicit implementation of serialize for batch.
        /// </summary>
        /// <param name="parameters">The list of populated parameters.</param>
        /// <returns>The serialized batched query.</returns>
        string IBatchable.SerializeForBatch(Dictionary<string, object> parameters)
        {
            return this.SerializeForBatch(parameters);
        }
    }
}
