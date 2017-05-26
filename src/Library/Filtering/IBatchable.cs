// -----------------------------------------------------------------------
// <copyright file="IBatchable.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// Helper interface to associate Batchable types.
    /// </summary>
    internal interface IBatchable
    {
        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        void Deserialize(XmlReader reader);

        /// <summary>
        /// Serialize the batch.
        /// </summary>
        /// <returns>The serialized value.</returns>
        string SerializeForBatch(Dictionary<string, object> parameters);
    }
}
