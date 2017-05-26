// -----------------------------------------------------------------------
// <copyright file="EntitySetType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Corresponds to EntitySetType in model.
    /// </summary>
    public partial class EntitySetType
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal override void Deserialize(XmlReader reader)
        {
            this.Deserialize(reader, new Action<XmlReader>(this.DeserializeSubType));
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal override string Serialize()
        {
            string subtype = null;
            if (this.SubType != null)
            {
                subtype = this.SubType.Serialize();
            }

            StringBuilder query = new StringBuilder();
            query.Append(this.Serialize(subtype));

            return query.ToString();
        }

        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeSubType(XmlReader reader)
        {
            switch (reader.LocalName)
            {
                case "SubType":
                    this.SubType = new EntityType();
                    this.SubType.Deserialize(reader.ReadSubtree());
                    break;
            }
        }
    }
}
