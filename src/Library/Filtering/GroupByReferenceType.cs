// -----------------------------------------------------------------------
// <copyright file="GroupByReferenceType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Xml;

    /// <summary>
    /// Helper class to declare a group reference.
    /// </summary>
    public partial class GroupByReferenceType
    {
        /// <summary>
        /// Deserialize the reader into the current instance.
        /// </summary>
        /// <param name="reader">The reader to deserialize.</param>
        internal void Deserialize(XmlReader reader)
        {
            reader.MoveToContent();
            string grouping = reader.GetAttribute("GroupingType");
            if (string.IsNullOrEmpty(grouping) == false)
            {
                this.GroupingType = (GroupingType)Enum.Parse(typeof(GroupingType), grouping);
            }

            while (reader.Read() == true)
            {
                if (reader.IsStartElement() && reader.LocalName == "Property")
                {
                    PropertyType pt = new PropertyType();
                    pt.Name = reader.GetAttribute("Name");
                    this.Properties.Add(pt);
                }
            }
        }
    }
}
