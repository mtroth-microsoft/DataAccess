// -----------------------------------------------------------------------
// <copyright file="LambdaAndExpressionProcessor.cs" Company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Linq.Expressions;
    using System.Text;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class LambdaAndExpressionProcessor : AndExpressionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the LambdaAndExpressionProcessor class.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        public LambdaAndExpressionProcessor(IContext context, ExpressionType expression)
            : base(context, expression)
        {
        }

        /// <summary>
        /// Process the expression.
        /// </summary>
        /// <returns>The return value.</returns>
        internal override object Process()
        {
            AndType andType = this.Expression as AndType;
            if (andType == null)
            {
                throw new ArgumentNullException("expression");
            }

            Expression and = null;
            Expression right = null;

            int i = 0;
            foreach (ExpressionType expr in andType.Items)
            {
                i++;
                right = expr.Process(this.Context) as Expression;
                if (i == 1)
                {
                    and = right;
                    continue;
                }

                and = ExpressionLibrary.GetAnd(and, right, null);
            }

            return and;
        }
    }
}
