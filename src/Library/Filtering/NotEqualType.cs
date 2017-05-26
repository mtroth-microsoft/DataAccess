// -----------------------------------------------------------------------
// <copyright file="NotEqualType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    /// <summary>
    /// Corresponds to NotEqualType in model.
    /// </summary>
    public partial class NotEqualType
    {
        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <returns>The serialized string.</returns>
        internal override string Serialize()
        {
            return this.SerializePredicate(" ne ");
        }

        /// <summary>
        /// Split the current predicate into an or statement.
        /// </summary>
        /// <returns>The or statement.</returns>
        protected override ConditionType Split()
        {
            return this.Split<NotEqualType>(new AndType());
        }
    }
}
