// -----------------------------------------------------------------------
// <copyright file="ExpressionLibrary.cs" Company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Delegation of the directives.
    /// </summary>
    /// <typeparam name="T">The type of the return.</typeparam>
    /// <param name="args">The input arguments.</param>
    /// <returns>The instanc eof T generating within the run time delegation.</returns>
    internal delegate T ObjectActivator<T>(params object[] args); // where T : struct;

    /// <summary>
    /// Delegation of the directives.
    /// </summary>
    /// <param name="args">The input arguments.</param>
    internal delegate void ObjectActivator(params object[] args);

    /// <summary>
    /// Common functions for processing expressions.
    /// </summary>
    public static class ExpressionLibrary
    {
        /// <summary>
        /// Method to process the given expression.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="result">The expression type to start with.</param>
        /// <param name="valueBag">The values to apply to the given expression.</param>
        /// <returns>The instance of the return type.</returns>
        public static T Process<T>(ExpressionType result, IList<SimpleParameter> valueBag) 
            //// where T : struct
        {
            // get all the declared types to resolve their expressions.
            StandardLambdaContext context = new StandardLambdaContext();
            Expression expression = result.Process(context) as Expression;
            ICollection<object> values = new List<object>();
            ICollection<ParameterExpression> parameters = new List<ParameterExpression>();

            // build a list of types present in the expression tree.
            List<Type> types = new List<Type>();
            ICollection<SimpleParameter> variables = context.Parameters();
            foreach (SimpleParameter sp in variables)
            {
                ParameterExpression parameter = sp.Value as ParameterExpression;
                object value = valueBag.Where(p => p.Name == parameter.Name).Single().Value;

                if (value != null && value.GetType() != parameter.Type)
                {
                    value = ConvertValue(parameter.Type, value);
                }

                types.Add(parameter.Type);
                values.Add(value);
                parameters.Add(parameter);
            }

            // add the return type so it is calculated into the activactor's anonymous delegate
            types.Add(typeof(T));
            ObjectActivator<T> activator = ExpressionLibrary.GetActivator<T>(
                expression, 
                parameters, 
                types.ToArray());

            return activator(values.ToArray());
        }

        /// <summary>
        /// Get the method call.
        /// </summary>
        /// <param name="instance">The instance parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <param name="arguments">The arguments parameter value.</param>
        /// <returns>The return value.</returns>
        internal static MethodCallExpression GetMethodCall(Expression instance, MethodInfo method, IEnumerable<Expression> arguments)
        {
            MethodCallExpression methodCall = Expression.Call(instance, method, arguments);

            return methodCall;
        }

        /// <summary>
        /// Get the constant.
        /// </summary>
        /// <param name="value">The value parameter value.</param>
        /// <param name="type">The type parameter value.</param>
        /// <returns>The return value.</returns>
        internal static ConstantExpression GetConstant(object value, Type type)
        {
            ConstantExpression constant = NewExpression.Constant(value, type);

            return constant;
        }

        /// <summary>
        /// Get the parameter.
        /// </summary>
        /// <param name="type">The type parameter value.</param>
        /// <param name="name">The name parameter value.</param>
        /// <returns>The return value.</returns>
        internal static ParameterExpression GetParameter(Type type, string name)
        {
            ParameterExpression parameter = NewExpression.Parameter(type, name);

            return parameter;
        }

        /// <summary>
        /// Get the field.
        /// </summary>
        /// <param name="expression">The expression parameter value.</param>
        /// <param name="type">The type parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static UnaryExpression GetField(Expression expression, Type type, MethodInfo method)
        {
            UnaryExpression unary = NewExpression.Convert(expression, type, method);

            return unary;
        }

        /// <summary>
        /// Get the new call.
        /// </summary>
        /// <param name="ctor">The ctor parameter value.</param>
        /// <param name="arguments">The arguments parameter value.</param>
        /// <param name="members">The members parameter value.</param>
        /// <returns>The return value.</returns>
        internal static NewExpression GetNewCall(ConstructorInfo ctor, IEnumerable<Expression> arguments, IEnumerable<MemberInfo> members)
        {
            NewExpression newExp = Expression.New(ctor, arguments, members);

            return newExp;
        }

        /// <summary>
        /// Get and expression.
        /// </summary>
        /// <param name="left">The left parameter value.</param>
        /// <param name="right">The right parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static BinaryExpression GetAnd(Expression left, Expression right, MethodInfo method)
        {
            BinaryExpression and = Expression.And(left, right, method);

            return and;
        }

        /// <summary>
        /// Get or expression.
        /// </summary>
        /// <param name="left">The left parameter value.</param>
        /// <param name="right">The right parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static BinaryExpression GetOr(Expression left, Expression right, MethodInfo method)
        {
            BinaryExpression or = Expression.Or(left, right, method);

            return or;
        }

        /// <summary>
        /// Get or expression.
        /// </summary>
        /// <param name="expression">The parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static UnaryExpression GetNot(Expression expression, MethodInfo method)
        {
            UnaryExpression not = Expression.Not(expression, method);

            return not;
        }

        /// <summary>
        /// Get equals expression.
        /// </summary>
        /// <param name="left">The left parameter value.</param>
        /// <param name="right">The right parameter value.</param>
        /// <param name="liftToNull">The lift to null parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static BinaryExpression GetEqual(Expression left, Expression right, bool liftToNull, MethodInfo method)
        {
            BinaryExpression eq = Expression.Equal(left, right, liftToNull, method);

            return eq;
        }

        /// <summary>
        /// Get not equals expression.
        /// </summary>
        /// <param name="left">The left parameter value.</param>
        /// <param name="right">The right parameter value.</param>
        /// <param name="liftToNull">The life to null parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static BinaryExpression GetNotEqual(Expression left, Expression right, bool liftToNull, MethodInfo method)
        {
            BinaryExpression ne = Expression.NotEqual(left, right, liftToNull, method);

            return ne;
        }

        /// <summary>
        /// Get less than expression.
        /// </summary>
        /// <param name="left">The left parameter value.</param>
        /// <param name="right">The right parameter value.</param>
        /// <param name="liftToNull">The lift to null parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <param name="includeEqual">The inlcud equal parameter value.</param>
        /// <returns>The return value.</returns>
        internal static BinaryExpression GetLessThan(Expression left, Expression right, bool liftToNull, MethodInfo method, bool includeEqual)
        {
            BinaryExpression lt = null;
            if (includeEqual == true)
            {
                lt = Expression.LessThanOrEqual(left, right, liftToNull, method);
            }
            else
            {
                lt = Expression.LessThan(left, right, liftToNull, method);
            }

            return lt;
        }

        /// <summary>
        /// Get greather than expression.
        /// </summary>
        /// <param name="left">The left parameter value.</param>
        /// <param name="right">The right parameter value.</param>
        /// <param name="liftToNull">The lift to null parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <param name="includeEqual">The inlcude equal parameter value.</param>
        /// <returns>The return value.</returns>
        internal static BinaryExpression GetGreaterThan(Expression left, Expression right, bool liftToNull, MethodInfo method, bool includeEqual)
        {
            BinaryExpression gt = null;
            if (includeEqual == true)
            {
                gt = Expression.GreaterThanOrEqual(left, right, liftToNull, method);
            }
            else
            {
                gt = Expression.GreaterThan(left, right, liftToNull, method);
            }

            return gt;
        }

        /// <summary>
        /// Get condition expression.
        /// </summary>
        /// <param name="test">The test parameter value.</param>
        /// <param name="ifTrue">The if true parameter value.</param>
        /// <param name="ifFalse">The if false parameter value.</param>
        /// <returns>The return value.</returns>
        internal static ConditionalExpression GetCondition(Expression test, Expression ifTrue, Expression ifFalse)
        {
            ConditionalExpression conditional = Expression.Condition(test, ifTrue, ifFalse);

            return conditional;
        }

        /// <summary>
        /// Get object activator.
        /// </summary>
        /// <param name="instance">The instance parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static ObjectActivator GetActivator(object instance, MethodInfo method)
        {
            System.Linq.Expressions.LambdaExpression lambda = ConstructLambdaMethodCall<ObjectActivator>(instance, method);

            // compile it
            ObjectActivator compiled = (ObjectActivator)lambda.Compile();
            return compiled;
        }

        /// <summary>
        /// Get generic object activator.
        /// </summary>
        /// <typeparam name="T">The type of the activator.</typeparam>
        /// <param name="instance">The instance parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        internal static ObjectActivator<T> GetActivator<T>(object instance, MethodInfo method)
        {
            System.Linq.Expressions.LambdaExpression lambda = ConstructLambdaMethodCall<ObjectActivator<T>>(instance, method);

            // compile it
            ObjectActivator<T> compiled = (ObjectActivator<T>)lambda.Compile();
            return compiled;
        }

        /// <summary>
        ///  T here is bool [return value from activator].
        /// </summary>
        /// <typeparam name="T">The type of the activator.</typeparam>
        /// <param name="expressionBody">The expression body parameter value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <param name="types">The types parameter value.</param>
        /// <returns>The return value.</returns>
        private static ObjectActivator<T> GetActivator<T>(
            Expression expressionBody, 
            IEnumerable<ParameterExpression> parameters, 
            params Type[] types) 
            //// where T : struct
        {
            // need to find out the delegate type and use lambda generator to get one.
            Type funcType = Expression.GetFuncType(types);
            MethodInfo generic = typeof(ExpressionLibrary).GetMethod("GenerateLambda", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo method = generic.MakeGenericMethod(funcType);

            // use the lambda generator to get an instance of the delegate.
            object[] arguments = new object[] { expressionBody, parameters };
            object result = method.Invoke(null, arguments);

            // wrap the anonymous delegate in an activator.
            MethodInfo invokeMethod = funcType.GetMethod("Invoke");
            LambdaExpression lambda = ExpressionLibrary.ConstructLambdaMethodCall<ObjectActivator<T>>(result, invokeMethod);
            ObjectActivator<T> activator = (ObjectActivator<T>)lambda.Compile();

            return activator;
        }

        /// <summary>
        /// T here is some kind of Func.
        /// </summary>
        /// <typeparam name="T">The type of the lambda.</typeparam>
        /// <param name="expressionBody">The expression body parameter value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The return value.</returns>
        private static T GenerateLambda<T>(Expression expressionBody, IEnumerable<ParameterExpression> parameters)
        {
            Expression<T> lambda =
               Expression.Lambda<T>(
                   expressionBody,
                   parameters);

            return lambda.Compile();
        }

        /// <summary>
        /// Construct the lambda method call.
        /// </summary>
        /// <typeparam name="T">The type of the lambda.</typeparam>
        /// <param name="instance">The instance parameter value.</param>
        /// <param name="method">The method parameter value.</param>
        /// <returns>The return value.</returns>
        private static LambdaExpression ConstructLambdaMethodCall<T>(object instance, MethodInfo method)
        {
            // Type type = method.DeclaringType;
            ParameterInfo[] paramsInfo = method.GetParameters();

            // create a single param of type object[]
            System.Linq.Expressions.ParameterExpression param =
                System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");

            System.Linq.Expressions.Expression[] argsExp =
                new System.Linq.Expressions.Expression[paramsInfo.Length];

            // pick each arg from the params array 
            // and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                System.Linq.Expressions.Expression index = System.Linq.Expressions.Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                System.Linq.Expressions.Expression paramAccessorExp =
                    System.Linq.Expressions.Expression.ArrayIndex(param, index);

                System.Linq.Expressions.Expression paramCastExp =
                    System.Linq.Expressions.Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            // the instance
            System.Linq.Expressions.ConstantExpression host = System.Linq.Expressions.NewExpression.Constant(instance, method.DeclaringType);

            // make a NewExpression that calls the
            // ctor with the args we just created
            System.Linq.Expressions.MethodCallExpression newExp = System.Linq.Expressions.Expression.Call(host, method, argsExp);

            // create a lambda with the New
            // Expression as body and our param object[] as arg
            System.Linq.Expressions.LambdaExpression lambda =
                System.Linq.Expressions.Expression.Lambda(typeof(T), newExp, param);

            return lambda;
        }

        /// <summary>
        /// Converts the value to the given type.
        /// </summary>
        /// <param name="type">The type to convert to.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        private static object ConvertValue(Type type, object value)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            object converted = null;
            if (type == typeof(string))
            {
                converted = value == DBNull.Value ? null : value;
                return converted;
            }
            else if (type == typeof(double))
            {
                converted = Convert.ToDouble(value, culture);
            }
            else if (type == typeof(float))
            {
                converted = Convert.ToSingle(value, culture);
            }
            else if (type == typeof(decimal))
            {
                value = Convert.ToDecimal(value, culture);
            }
            else if (type == typeof(long))
            {
                converted = Convert.ToInt64(value, culture);
            }
            else if (type == typeof(int))
            {
                converted = Convert.ToInt32(value, culture);
            }
            else if (type == typeof(short))
            {
                converted = Convert.ToInt16(value, culture);
            }
            else if (type == typeof(byte))
            {
                converted = Convert.ToByte(value, culture);
            }
            else if (type == typeof(sbyte))
            {
                converted = Convert.ToSByte(value, culture);
            }
            else if (type == typeof(ulong))
            {
                converted = Convert.ToUInt64(value, culture);
            }
            else if (type == typeof(uint))
            {
                converted = Convert.ToUInt32(value, culture);
            }
            else if (type == typeof(ushort))
            {
                converted = Convert.ToUInt16(value, culture);
            }

            return converted ?? value;
        }
    }
}
