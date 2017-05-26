// -----------------------------------------------------------------------
// <copyright file="QueryTranslator.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Linq.Expressions;

    /// <summary>
    /// Query translator.
    /// </summary>
    internal class QueryTranslator : ExpressionVisitor
    {
        /// <summary>
        /// The private storage of the translation object.
        /// </summary>
        private object state;

        /// <summary>
        /// Initializes a new instance of the QueryTranslator class.
        /// </summary>
        /// <param name="state">The dbquery to translate the expression into.</param>
        public QueryTranslator(object state)
        {
            this.state = state;
        }

        /// <summary>
        /// Translate the expression.
        /// </summary>
        /// <param name="expression">The expression to translate.</param>
        /// <returns>Translated expression.</returns>
        internal Expression Translate(Expression expression)
        {
            return this.Visit(expression);
        }

        /// <summary>
        /// Intercept the visit constant call so as to replace 
        /// the customer queryable will the EF queryable.
        /// </summary>
        /// <param name="node">The visited node.</param>
        /// <returns>The translated node.</returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            // The only translation required is to replace the infrastructure queryable with the 
            // state object. in the multiple dbproxy case, this state object will be one of the multiple
            // entity framework context instances. performing the replacement means that the linq operation
            // will be resolved by the EF context rather than the multiple datasource placeholder.
            if (node.Type.Name.StartsWith(typeof(InfrastructureQueryable<>).Name) == true)
            {
                return Expression.Constant(this.state, this.state.GetType());
            }
            else
            {
                return base.VisitConstant(node);
            }
        }
    }
}
