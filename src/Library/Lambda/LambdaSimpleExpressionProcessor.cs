// -----------------------------------------------------------------------
// <copyright file="LambdaSimpleExpressionProcessor.cs" Company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal abstract class LambdaSimpleExpressionProcessor : SimpleExpressionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the LambdaSimpleExpressionProcessor class.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        protected LambdaSimpleExpressionProcessor(IContext context, ExpressionType expression)
            : base(context, expression)
        {
        }

        /// <summary>
        /// Base class definition for get expression signature.
        /// </summary>
        /// <param name="simpleExpression">The simple expression.</param>
        /// <param name="parameter">The parameter to use when getting the expression.</param>
        /// <param name="constant">The constant to use when getting the expression.</param>
        /// <returns>The generated expression.</returns>
        public abstract Expression GetExpression(
            PredicateType simpleExpression,
            ParameterExpression parameter,
            ConstantExpression constant);

        /// <summary>
        /// Process the expression.
        /// </summary>
        /// <returns>The lambda generated from the expression.</returns>
        internal override object Process()
        {
            PredicateType simpleCriteriaType = this.Expression as PredicateType;
            if (simpleCriteriaType == null)
            {
                throw new ArgumentNullException("expression");
            }

            PropertyNameType property = simpleCriteriaType.Subject as PropertyNameType;
            object value = simpleCriteriaType.Predicate;
            string name = property.Value;

            ParameterExpression parameter = StandardLambdaContext.GetParameter(name, value.GetType(), this.Context);
            ConstantExpression constant = ExpressionLibrary.GetConstant(value, value.GetType());

            return this.GetExpression(simpleCriteriaType, parameter, constant);
        }
    }
}
