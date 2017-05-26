// -----------------------------------------------------------------------
// <copyright file="InType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    /// <summary>
    /// Corresponds to InType in model.
    /// </summary>
    public partial class InType
    {
        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns>The serialized string.</returns>
        internal override string Serialize()
        {
            return this.SerializePredicate(" in ");
        }

        /// <summary>
        /// Split the current predicate into an or statement.
        /// </summary>
        /// <returns>The or statement.</returns>
        protected override ConditionType Split()
        {
            return this.Split<InType>(new OrType());
        }
    }
}
