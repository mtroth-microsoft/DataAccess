// -----------------------------------------------------------------------
// <copyright file="OrType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    /// <summary>
    /// Corresponds to OrType in model.
    /// </summary>
    public partial class OrType
    {
        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns></returns>
        internal override string Serialize()
        {
            return this.SerializeCondition(" or ");
        }
    }
}
