// -----------------------------------------------------------------------
// <copyright file="UserDefinedFunction.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class for declaring custom user defined functions.
    /// </summary>
    public class UserDefinedFunction
    {
        /// <summary>
        /// Gets or sets the user defined function.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the format template.
        /// </summary>
        public string Format
        {
            get;
            set;
        }

        /// <summary>
        /// Imprint the udf's template with the provided values.
        /// </summary>
        /// <param name="args">The list of raw values.</param>
        /// <param name="context">The parameter context in the current scope.</param>
        /// <param name="predicatableSerializer">A helper delegate to convert the args to strings, if applicable.</param>
        /// <returns>The serialized string.</returns>
        internal string SerializeToSql(
            List<object> args,
            ParameterContext context,
            Func<object, ParameterContext, string> predicatableSerializer)
        {
            List<string> values = new List<string>();
            foreach (object arg in args)
            {
                values.Add(predicatableSerializer(arg, context));
            }

            return string.Format(this.Format, values.ToArray());
        }
    }
}
