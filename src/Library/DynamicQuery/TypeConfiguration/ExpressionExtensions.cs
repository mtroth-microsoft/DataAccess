// -----------------------------------------------------------------------
// <copyright file="ExpressionExtensions.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Helper to extend expression functionality.
    /// </summary>
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Extend lambda to enable property set look up.
        /// </summary>
        /// <param name="propertyAccessExpression">The lambda expression.</param>
        /// <returns>The property set.</returns>
        public static PropertyInfo GetSimplePropertyAccess(this LambdaExpression propertyAccessExpression)
        {
            PropertyInfo path = propertyAccessExpression.Parameters.Single<ParameterExpression>().MatchSimplePropertyAccess(propertyAccessExpression.Body);

            return path;
        }

        /// <summary>
        /// Extend parameter to enable property discovery.
        /// </summary>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="propertyAccessExpression">The member accessor.</param>
        /// <returns>The property set.</returns>
        private static List<PropertyInfo> MatchPropertyAccess(this Expression parameterExpression, Expression propertyAccessExpression)
        {
            MemberExpression expression;
            List<PropertyInfo> components = new List<PropertyInfo>();
            do
            {
                expression = propertyAccessExpression.RemoveConvert() as MemberExpression;
                if (expression == null)
                {
                    return null;
                }

                PropertyInfo member = expression.Member as PropertyInfo;
                if (member == null)
                {
                    return null;
                }

                components.Insert(0, member);
                propertyAccessExpression = expression.Expression;
            }
            while (expression.Expression != parameterExpression);

            return components;
        }

        /// <summary>
        /// Match a simple property.
        /// </summary>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="propertyAccessExpression">The member accessor expression.</param>
        /// <returns></returns>
        private static PropertyInfo MatchSimplePropertyAccess(this Expression parameterExpression, Expression propertyAccessExpression)
        {
            PropertyInfo path = parameterExpression.MatchPropertyAccess(propertyAccessExpression).SingleOrDefault();

            return path;
        }

        /// <summary>
        /// Simplify an expression.
        /// </summary>
        /// <param name="expression">The expression to simplify.</param>
        /// <returns>The simplified expression.</returns>
        public static Expression RemoveConvert(this Expression expression)
        {
            while ((expression.NodeType == ExpressionType.Convert) || (expression.NodeType == ExpressionType.ConvertChecked))
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression;
        }
    }
}
