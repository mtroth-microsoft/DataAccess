// -----------------------------------------------------------------------
// <copyright file="ChangeSetType.cs" company="Lensgrinder, Ltd.">
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
    /// Corresponds to ChangeSetType in model.
    /// </summary>
    public partial class ChangeSetType : IBatchable
    {
        /// <summary>
        /// The change set id.
        /// </summary>
        private Guid id = Guid.NewGuid();

        /// <summary>
        /// Gets the change set Id.
        /// </summary>
        internal Guid Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Convert the change set into a string for use by the batch request type.
        /// </summary>
        /// <param name="parameters">The populated parameters.</param>
        /// <returns>The serialized string for the batch.</returns>
        internal string SerializeForBatch(Dictionary<string, object> parameters)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Content-Type: multipart/mixed; boundary=changeset_" + this.Id);
            builder.AppendLine();
            builder.AppendLine();

            int index = 1;
            foreach (BatchedQueryType query in this.Requests)
            {
                builder.AppendLine("--changeset_" + this.Id);
                builder.AppendLine("Content-Type: application/http");
                builder.AppendLine("Content-Transfer-Encoding: binary");
                builder.AppendLine("Content-ID: " + index++);
                builder.AppendLine();

                string request = query.SerializeForBatch(parameters);
                builder.AppendLine(request);
            }

            builder.Append("--changeset_" + this.Id + "--");

            return builder.ToString();
        }

        /// <summary>
        /// Extract an instance from an xml reader.
        /// </summary>
        /// <param name="reader">The xmlreader to introspect.</param>
        internal void Deserialize(XmlReader reader)
        {
            if (reader.Read() == true)
            {
                this.Name = reader.GetAttribute("Name");
                this.Namespace = reader.GetAttribute("Namespace");
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
            }
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
