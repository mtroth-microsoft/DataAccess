// -----------------------------------------------------------------------
// <copyright file="WebMethodUrlHelper.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// Helper class to construct URLs.
    /// </summary>
    public class WebMethodUrlHelper
    {
        /// <summary>
        /// Initializes a new instance of the WebMethodUrlHelper class.
        /// </summary>
        public WebMethodUrlHelper(Uri root, string entitySet, string methodName, Dictionary<string, object> args)
        {
            this.Root = root;
            this.EntitySet = entitySet;
            this.MethodName = methodName;
            this.Arguments = args;
        }

        /// <summary>
        /// Gets the root of the url.
        /// </summary>
        public Uri Root
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the entity set name.
        /// </summary>
        public string EntitySet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the method name.
        /// </summary>
        public string MethodName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of arguments.
        /// </summary>
        public IDictionary<string, object> Arguments
        {
            get;
            private set;
        }

        /// <summary>
        /// Override the default implementation of tostring.
        /// </summary>
        /// <returns>The serialized version of the url.</returns>
        public override string ToString()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            string delimitter = string.Empty;

            StringBuilder builder = new StringBuilder();
            if (this.Root != null)
            {
                builder.Append(this.Root.AbsoluteUri);
                builder.Append("/");
            }

            if (string.IsNullOrEmpty(this.EntitySet) == false)
            {
                builder.Append(this.EntitySet);
                builder.Append("/");
            }

            builder.Append(this.MethodName);
            builder.Append("(");

            int index = 0;
            foreach (string key in this.Arguments.Keys)
            {
                builder.Append(delimitter);
                builder.Append(key);
                builder.Append("=");
                if (this.Arguments[key] == null)
                {
                    builder.Append("null");
                }
                else if (this.RequiresTicks(key) == true)
                {
                    string name = "@p" + index++;
                    parameters.Add(name, this.Arguments[key].ToString());
                    builder.Append(name);
                }
                else
                {
                    builder.Append(this.ConfigureValue(key));
                }

                delimitter = ",";
            }

            builder.Append(")");

            delimitter = string.Empty;
            if (parameters.Count > 0)
            {
                builder.Append("?");
                foreach (string key in parameters.Keys)
                {
                    builder.Append(delimitter);
                    builder.Append(key);
                    builder.Append("=");
                    builder.Append("'");
                    builder.Append(parameters[key]);
                    builder.Append("'");
                    delimitter = "&";
                }
            }


            return builder.ToString();
        }

        /// <summary>
        /// Configure the value associated with the provided key.
        /// </summary>
        /// <param name="key">The corresponding key.</param>
        /// <returns>The matching object value.</returns>
        private object ConfigureValue(string key)
        {
            if (this.Arguments[key] != null)
            {
                Type type = this.Arguments[key].GetType();
                if (type.IsEnum == true)
                {
                    return string.Format("{0}'{1}'", type.FullName, this.Arguments[key]);
                }
                else if (type == typeof(EnumValueType))
                {
                    EnumValueType evt = (EnumValueType)this.Arguments[key];
                    return string.Format("{0}'{1}'", evt.Type, evt.Value);
                }
            }

            return this.Arguments[key];
        }

        /// <summary>
        /// Determine whether an argument requires ticks or not.
        /// </summary>
        /// <param name="key">The key in the dictionary to check.</param>
        /// <returns>True if ticks are required, otherwise false.</returns>
        private bool RequiresTicks(string key)
        {
            if (this.Arguments.ContainsKey(key) == false ||
                this.Arguments[key] == null)
            {
                return false;
            }

            Type type = this.Arguments[key].GetType();

            return type == typeof(string);
        }
    }
}
