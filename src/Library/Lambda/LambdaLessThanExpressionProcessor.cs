// -----------------------------------------------------------------------
// <copyright file="LambdaLessThanExpressionProcessor.cs" Company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------

namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Text;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class LambdaLessThanExpressionProcessor : LambdaSimpleExpressionProcessor
    {
        /// <summary>
        /// True indicates greater than equal, false indicates less than.
        /// </summary>
        private bool negate;

        /// <summary>
        /// Initializes an instance of the LambdaEqualExpressionProcessor.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        /// <param name="negate">True to negate processing, otherwise false.</param>
        public LambdaLessThanExpressionProcessor(IContext context, ExpressionType expression, bool negate)
            : base(context, expression)
        {
            this.negate = negate;
        }

        /// <summary>
        /// Get the expression.
        /// </summary>
        /// <param name="simpleCriteriaType">The simple criteria parameter value.</param>
        /// <param name="parameter">The parameter value.</param>
        /// <param name="constant">The constant parameter value.</param>
        /// <returns>The return value.</returns>
        public override Expression GetExpression(
            PredicateType simpleCriteriaType, 
            ParameterExpression parameter, 
            ConstantExpression constant)
        {
            if (this.negate == true)
            {
                return ExpressionLibrary.GetGreaterThan(parameter, constant, false, null, true);
            }
            else
            {
                return ExpressionLibrary.GetLessThan(parameter, constant, false, null, false);
            }
        }
    }
}
