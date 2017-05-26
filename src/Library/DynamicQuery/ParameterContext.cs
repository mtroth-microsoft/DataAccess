// -----------------------------------------------------------------------
// <copyright file="ParameterContext.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using OdataExpressionModel;

    /// <summary>
    /// Helper class to parameterize filter statements.
    /// </summary>
    internal sealed class ParameterContext
    {
        /// <summary>
        /// The Template for name generation.
        /// </summary>
        private const string Template = "@parm";

        /// <summary>
        /// The internal counter for name generation.
        /// </summary>
        private int counter;

        /// <summary>
        /// The internal map of paramters.
        /// </summary>
        private Dictionary<string, object> parameters = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets a value indicating whether the current parameter context is
        /// for a single entity merge operation with hard coded values in the source columns.
        /// </summary>
        public bool IsSingleRowMerge
        {
            get;
            set;
        }

        /// <summary>
        /// Generates and returns the next parameter to use.
        /// </summary>
        /// <returns>The parameter name.</returns>
        public string GetNextName()
        {
            return string.Concat(Template, this.counter++);
        }

        /// <summary>
        /// Assign a name value pair for a given parameter.
        /// If the key already exists, it will be overwritten.
        /// </summary>
        /// <param name="name">The name of the parameter to assign.</param>
        /// <param name="value">The value of the parameter to assign.</param>
        public void Assign(string name, object value)
        {
            if (value is string)
            {
                value = value.ToString().Replace("''", "'");
            }

            this.parameters[name] = value;
        }

        /// <summary>
        /// Assign a name value pair for a given parameter.
        /// If the key already exists, it will be overwritten.
        /// </summary>
        /// <param name="name">The name of the parameter to assign.</param>
        /// <param name="value">The value of the parameter to assign.</param>
        public void Assign(string name, ParameterType value)
        {
            switch (name)
            {
                case "@Now":
                    this.parameters[name] = DateTimeOffset.Now;
                    break;
                case "@UtcNow":
                    this.parameters[name] = DateTimeOffset.UtcNow;
                    break;
                case "@Today":
                    this.parameters[name] = FloorDate(DateTimeOffset.Now);
                    break;
                case "@UtcToday":
                    this.parameters[name] = FloorDate(DateTimeOffset.UtcNow);
                    break;
            }
        }

        /// <summary>
        /// Generate and read the set of parameters.
        /// </summary>
        /// <returns>The list of parameters.</returns>
        public List<Parameter> ReadAll()
        {
            List<Parameter> list = new List<Parameter>();
            foreach (string key in this.parameters.Keys)
            {
                Parameter p = new Parameter();
                p.ParameterName = key;
                p.Value = Convert(this.parameters[key]);
                p.DbType = ConvertToSqlType(p.Value);
                list.Add(p);
            }

            return list;
        }

        /// <summary>
        /// Parameter Factory method.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value for the parameter.</param>
        /// <returns>The created parameter.</returns>
        internal static Parameter CreateParameter(string name, object value)
        {
            Parameter parameter = new Parameter(name, value);
            parameter.DbType = ConvertToSqlType(value);

            return parameter;
        }

        /// <summary>
        /// Remove the time of day from the datetime offset.
        /// </summary>
        /// <param name="now">The datetime to inspect.</param>
        /// <returns>The resulting datetimeoffset.</returns>
        private static DateTimeOffset FloorDate(DateTimeOffset now)
        {
            return now - now.TimeOfDay;
        }

        /// <summary>
        /// Handle special cases in values being parameterized.
        /// </summary>
        /// <param name="value">The the value to inspect.</param>
        /// <returns>The converted value, if applicable.</returns>
        private static object Convert(object value)
        {
            if (value != null)
            {
                if (value.GetType().IsEnum == true)
                {
                    Type underlying = value.GetType().GetEnumUnderlyingType();
                    if (underlying == typeof(int) || underlying == typeof(byte))
                    {
                        Enum instance = (Enum)value;
                        int converted = int.Parse(instance.ToString("d"));
                        return converted;
                    }

                    throw new InvalidOperationException("Unhandled underlying enum type.");
                }
            }

            return value;
        }

        /// <summary>
        /// Serialize the sql type name of the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The dbtype of the value.</returns>
        private static DbType ConvertToSqlType(object value)
        {
            DbType type = DbType.String;
            if (value != null)
            {
                if (value is int)
                {
                    type = DbType.Int32;
                }
                else if (value is DateTime)
                {
                    type = DbType.DateTime;
                }
                else if (value is DateTimeOffset)
                {
                    type = DbType.DateTimeOffset;
                }
                else if (value is Guid)
                {
                    type = DbType.Guid;
                }
                else if (value is long)
                {
                    type = DbType.Int64;
                }
                else if (value is short)
                {
                    type = DbType.Int16;
                }
                else if (value is byte)
                {
                    type = DbType.Byte;
                }
                else if (value is byte[])
                {
                    type = DbType.Binary;
                }
                else if (value is bool)
                {
                    type = DbType.Boolean;
                }
                else if (value is decimal)
                {
                    type = DbType.Decimal;
                }
                else if (value is float)
                {
                    type = DbType.Single;
                }
                else if (value is double)
                {
                    type = DbType.Double;
                }
            }

            return type;
        }
    }
}
