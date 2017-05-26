// -----------------------------------------------------------------------
// <copyright file="ODataOperationType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    public partial class ODataOperationType
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal void Deserialize(XmlReader reader)
        {
            this.Name = reader.GetAttribute("Name");
            this.EntitySet = reader.GetAttribute("EntitySet");
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Argument")
                {
                    EqualType eq = new EqualType();
                    eq.Deserialize(reader.ReadSubtree());
                    this.Arguments.Add(eq);
                }
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal string Serialize()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            foreach (EqualType eq in this.Arguments)
            {
                string name = eq.FindSubject().Serialize();
                args[name] = eq.FindValue();
            }

            WebMethodUrlHelper helper = new WebMethodUrlHelper(null, this.EntitySet, this.Name, args);

            return string.Format("/{0}", helper.ToString());
        }
    }
}
