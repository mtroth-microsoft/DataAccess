// -----------------------------------------------------------------------
// <copyright file="LessThanType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    /// <summary>
    /// Corresponds to LessThanType in model.
    /// </summary>
    public partial class LessThanType
    {
        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns>The serialized string.</returns>
        internal override string Serialize()
        {
            return this.SerializePredicate(" lt ");
        }

        /// <summary>
        /// Split the current predicate into an or statement.
        /// </summary>
        /// <returns>The or statement.</returns>
        protected override ConditionType Split()
        {
            return this.Split<LessThanType>(new OrType());
        }
    }
}
