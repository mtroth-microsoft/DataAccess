// -----------------------------------------------------------------------
// <copyright file="TopOrSkipType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to TopOrSkipType in model.
    /// </summary>
    public partial class TopOrSkipType
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal virtual void Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.None)
            {
                throw new XmlException("Reader is not in a state to be read.");
            }

            // move into the subtree.
            if (reader.Read() == true)
            {
                string rawtop = reader.GetAttribute("Top");
                if (string.IsNullOrEmpty(rawtop) == false)
                {
                    this.Top = XmlConvert.ToUInt32(rawtop);
                    this.TopSpecified = true;
                }

                string rawskip = reader.GetAttribute("Skip");
                if (string.IsNullOrEmpty(rawskip) == false)
                {
                    this.Skip = XmlConvert.ToUInt32(rawskip);
                    this.SkipSpecified = true;
                }
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal string Serialize()
        {
            StringBuilder query = new StringBuilder();
            string token = string.Empty;

            if (this.SkipSpecified == true)
            {
                query.Append(token);
                query.Append("$skip=");
                query.Append(this.Skip);
                token = "&";
            }

            if (this.TopSpecified == true)
            {
                query.Append(token);
                query.Append("$top=");
                query.Append(this.Top);
                token = "&";
            }

            return query.ToString();
        }
    }
}
