// -----------------------------------------------------------------------
// <copyright file="LambdaOrExpressionProcessor.cs" Company="Lensgrinder, Ltd.">
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
    internal class LambdaOrExpressionProcessor : OrExpressionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the LambdaOrExpressionProcessor class.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        public LambdaOrExpressionProcessor(IContext context, ExpressionType expression)
            : base(context, expression)
        {
        }

        /// <summary>
        /// Process the expression.
        /// </summary>
        /// <returns>The return value.</returns>
        internal override object Process()
        {
            OrType ortype = this.Expression as OrType;
            if (ortype == null)
            {
                throw new ArgumentNullException("expression");
            }

            Expression or = null;
            Expression right = null;

            int i = 0;
            foreach (ExpressionType expr in ortype.Items)
            {
                i++;
                right = expr.Process(this.Context) as Expression;
                if (i == 1)
                {
                    or = right;
                    continue;
                }

                or = ExpressionLibrary.GetOr(or, right, null);
            }

            return or;
        }
    }
}
