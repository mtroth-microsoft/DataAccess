// -----------------------------------------------------------------------
// <copyright file="IContext.cs" Company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal interface IContext
    {
        /// <summary>
        /// Gets the next parameter name.
        /// </summary>
        /// <returns>The string name to use.</returns>
        string GetNextName();

        /// <summary>
        /// Load the parameters.
        /// </summary>
        /// <param name="parameters">The parameter value.</param>
        void LoadParameters(IEnumerable<SimpleParameter> parameters);

        /// <summary>
        /// Return the criteria processor.
        /// </summary>
        /// <param name="expression">The parameter value.</param>
        /// <returns>The return value.</returns>
        ExpressionProcessor GetCriteriaProcessor(ExpressionType expression);

        /// <summary>
        /// Read the parameters.
        /// </summary>
        /// <returns>The return value.</returns>
        ICollection<SimpleParameter> Parameters();
    }
}
