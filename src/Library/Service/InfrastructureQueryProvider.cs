// -----------------------------------------------------------------------
// <copyright file="InfrastructureQueryProvider.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Web.OData;

    /// <summary>
    /// The query provider for InfrastructureQueryable.
    /// </summary>
    internal class InfrastructureQueryProvider : IQueryProvider
    {
        /// <summary>
        /// Initializes a new instance of the InfrastructureQueryProvider class.
        /// </summary>
        /// <param name="state">Persistent state between provider and queryable.</param>
        /// <param name="originalType">The original type of the query.</param>
        public InfrastructureQueryProvider(object state, Type originalType)
        {
            this.OriginalType = originalType;
            this.State = state;
        }

        /// <summary>
        /// Gets the state object.
        /// </summary>
        public object State
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the original type of the query.
        /// </summary>
        public Type OriginalType
        {
            get;
            private set;
        }

        /// <summary>
        /// Create a typed query using the given expression.
        /// </summary>
        /// <typeparam name="TElement">The type of the query.</typeparam>
        /// <param name="expression">The expression to use.</param>
        /// <returns>The resuling query.</returns>
        public IQueryable<T> CreateQuery<T>(Expression expression)
        {
            // This should be the source of any web api call.
            return new InfrastructureQueryable<T>(this, expression);
        }

        /// <summary>
        /// Create a query using the given expression.
        /// </summary>
        /// <param name="expression">The expression to use.</param>
        /// <returns>The resulting query.</returns>
        public IQueryable CreateQuery(Expression expression)
        {
            object result = this.ExecuteCreateQuery(expression);

            return result as IQueryable;
        }

        /// <summary>
        /// Execute the given query.
        /// </summary>
        /// <typeparam name="TResult">The type of the output.</typeparam>
        /// <param name="expression">The expression to use.</param>
        /// <returns>The result.</returns>
        public T Execute<T>(Expression expression)
        {
            // Handle for the case where we are resolving a count.
            // In this case T will of type int.
            if (expression.NodeType == ExpressionType.Call)
            {
                MethodCallExpression mce = expression as MethodCallExpression;
                switch (mce.Method.Name)
                {
                    case "LongCount":

                        Expression e = mce.Arguments[0] as Expression;
                        object result = this.ExecuteCreateQuery(e);
                        IQueryable q = result as IQueryable;

                        long longcount = 0;
                        foreach (object item in q)
                        {
                            longcount++;
                        }

                        return (T)(object)longcount;
                }
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute the given query.
        /// </summary>
        /// <param name="expression">The expression to use.</param>
        /// <returns>The result.</returns>
        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute the create query using the original type.
        /// </summary>
        /// <param name="expression">The expression to pass.</param>
        /// <returns>The unwrapped object result.</returns>
        private object ExecuteCreateQuery(Expression expression)
        {
            MethodInfo generic = this.GetType().GetMethods()
                .Where(p => p.IsGenericMethod == true && p.Name.StartsWith("CreateQuery"))
                .SingleOrDefault();
            MethodInfo method = generic.MakeGenericMethod(this.OriginalType);

            object result = method.Invoke(this, new object[] { expression });

            return result;
        }
    }
}
