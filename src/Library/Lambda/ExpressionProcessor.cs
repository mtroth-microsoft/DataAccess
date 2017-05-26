// -----------------------------------------------------------------------
// <copyright file="ExpressionProcessor.cs" Company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;

    /// <summary>
    /// Expression Processor declaration.
    /// </summary>
    internal abstract class ExpressionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the ExpressionProcessor class.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        protected ExpressionProcessor(IContext context, ExpressionType expression)
        {
            this.Context = context;
            this.Expression = expression;
        }

        /// <summary>
        /// Gets the context.
        /// </summary>
        public IContext Context
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public ExpressionType Expression
        {
            get;
            private set;
        }

        /// <summary>
        /// Process the expression.
        /// </summary>
        /// <returns>The return value.</returns>
        internal abstract object Process();
    }

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal abstract class AndExpressionProcessor : ExpressionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the AndExpressionProcessor class.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        protected AndExpressionProcessor(IContext context, ExpressionType expression)
            : base(context, expression)
        {
        }
    }

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal abstract class OrExpressionProcessor : ExpressionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the OrExpressionProcessor class.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        protected OrExpressionProcessor(IContext context, ExpressionType expression)
            : base(context, expression)
        {
        }
    }

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal abstract class SimpleExpressionProcessor : ExpressionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the SimpleExpressionProcessor class.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="expression">The expression to process.</param>
        protected SimpleExpressionProcessor(IContext context, ExpressionType expression)
            : base(context, expression)
        {
        }
    }
}
