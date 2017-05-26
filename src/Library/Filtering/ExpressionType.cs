// -----------------------------------------------------------------------
// <copyright file="ExpressionType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Xml;

    /// <summary>
    /// Corresponds to ExpressionType in model.
    /// </summary>
    public abstract partial class ExpressionType
    {
        /// <summary>
        /// Factory method to create an expression instance.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The instance.</returns>
        internal static ExpressionType Create(string name)
        {
            ExpressionType expression = null;
            switch (name)
            {
                case "Any":
                    expression = new AnyType();
                    break;

                case "All":
                    expression = new AllType();
                    break;

                case "And":
                    expression = new AndType();
                    break;

                case "Or":
                    expression = new OrType();
                    break;

                case "Equal":
                    expression = new EqualType();
                    break;

                case "NotEqual":
                    expression = new NotEqualType();
                    break;

                case "GreaterThan":
                    expression = new GreaterThanType();
                    break;

                case "GreaterThanOrEqual":
                    expression = new GreaterThanOrEqualType();
                    break;

                case "LessThan":
                    expression = new LessThanType();
                    break;

                case "LessThanOrEqual":
                    expression = new LessThanOrEqualType();
                    break;

                case "Has":
                    expression = new HasType();
                    break;

                case "In":
                    expression = new InType();
                    break;
            }

            return expression;
        }

        /// <summary>
        /// Deserialize the instance.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        internal abstract void Deserialize(XmlReader reader);

        /// <summary>
        /// Serialize the instance into a string.
        /// </summary>
        /// <returns>The serialized string.</returns>
        internal abstract string Serialize();

        /// <summary>
        /// Produces a deep copy of the expression.
        /// </summary>
        /// <returns>The copy.</returns>
        internal abstract ExpressionType DeepCopy();

        /// <summary>
        /// Set the prefixes on all the property name in the clause.
        /// </summary>
        /// <param name="prefix">The prefix to set.</param>
        internal abstract void SetPrefixes(string prefix);

        /// <summary>
        /// Populate the parameters, converting if necessary.
        /// </summary>
        /// <param name="parameters">The assigned parameters.</param>
        /// <returns>The ortype instance resulting from conversion.</returns>
        internal abstract ConditionType Convert(Dictionary<string, object> parameters);

        /// <summary>
        /// Process the underlying expression into a linq expression.
        /// </summary>
        /// <param name="context">The context of the current process operation.</param>
        /// <returns>The resulting expression</returns>
        internal virtual Expression Process(IContext context)
        {
            ExpressionProcessor processor = context.GetCriteriaProcessor(this);
            return processor.Process() as Expression;
        }
    }
}
