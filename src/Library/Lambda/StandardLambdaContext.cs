// -----------------------------------------------------------------------
// <copyright file="StandardLambdaContext.cs" Company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------

namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class StandardLambdaContext : IContext
    {
        /// <summary>
        /// The list of parameters.
        /// </summary>
        private List<SimpleParameter> parameters = new List<SimpleParameter>();

        /// <summary>
        /// The counter to use for parameters.
        /// </summary>
        private int counter = 0;

        /// <summary>
        /// Gets the next name to use for a parameter.
        /// </summary>
        /// <returns>The name to use.</returns>
        public string GetNextName()
        {
            return "parameter" + this.counter++;
        }

        /// <summary>
        /// Load the parameters.
        /// </summary>
        /// <param name="value">The parameter value.</param>
        public void LoadParameters(IEnumerable<SimpleParameter> value)
        {
            this.parameters = new List<SimpleParameter>(value);

            return;
        }

        /// <summary>
        /// Get a criteria processor.
        /// </summary>
        /// <param name="expression">The parameter value.</param>
        /// <returns>The return value.</returns>
        public ExpressionProcessor GetCriteriaProcessor(ExpressionType expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            switch (expression.GetType().Name)
            {
                case "AndType": return new LambdaAndExpressionProcessor(this, expression);
                case "OrType": return new LambdaOrExpressionProcessor(this, expression);
                case "EqualType": return new LambdaEqualExpressionProcessor(this, expression, false);
                case "NotEqualType": return new LambdaEqualExpressionProcessor(this, expression, true);
                case "LessThanType": return new LambdaLessThanExpressionProcessor(this, expression, false);
                case "LessThanOrEqualType": return new LambdaGreaterThanExpressionProcessor(this, expression, true);
                case "GreaterThanType": return new LambdaGreaterThanExpressionProcessor(this, expression, false);
                case "GreaterThanOrEqualType": return new LambdaLessThanExpressionProcessor(this, expression, true);
            }

            throw new ArgumentOutOfRangeException("expression");
        }

        /// <summary>
        /// Read the parameters.
        /// </summary>
        /// <returns>The return value.</returns>
        public ICollection<SimpleParameter> Parameters()
        {
            return this.parameters;
        }

        /// <summary>
        /// Looks for and extracts the parameter from the list, otherwise creates.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="graph">The graph to populate/search.</param>
        /// <returns>The discovered/created parameter.</returns>
        internal static ParameterExpression GetParameter(
            string name,
            Type type,
            IContext context)
        {
            StandardLambdaContext cxt = context as StandardLambdaContext;
            ParameterExpression parameter = null;
            ICollection<SimpleParameter> parameters = cxt.Parameters();

            foreach (SimpleParameter p in parameters)
            {
                if (string.CompareOrdinal(p.Name, name) == 0)
                {
                    parameter = p.Value as ParameterExpression;
                    break;
                }
            }

            if (parameter == null)
            {
                parameter = ExpressionLibrary.GetParameter(type, name);
                SimpleParameter sp = new SimpleParameter(parameter.Name, parameter, parameter.Type);
                parameters.Add(sp);
            }

            return parameter;
        }
    }
}
