// -----------------------------------------------------------------------
// <copyright file="AllType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    /// <summary>
    /// Corresponds to AllType in model.
    /// </summary>
    public partial class AllType
    {
        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns>The serialized string.</returns>
        internal override string Serialize()
        {
            return this.SerializeCollectionSearch("all");
        }
    }
}
