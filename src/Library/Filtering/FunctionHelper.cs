// -----------------------------------------------------------------------
// <copyright file="FunctionHelper.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper class for parsing function body.
    /// </summary>
    internal sealed class FunctionHelper
    {
        /// <summary>
        /// Initializes a new instance of the FunctionHelper class.
        /// </summary>
        /// <param name="body">The body of the function.</param>
        public FunctionHelper(string body)
        {
            int pos = body.IndexOf('(');
            this.Name = body.Substring(0, pos);
            this.Arguments = new List<object>();
            if (body.StartsWith("Not ", StringComparison.OrdinalIgnoreCase) == true)
            {
                this.Negate = true;
            }

            string extractFuncRegex = @"\b[^()]+\((.*)\)$";
            Match match = Regex.Match(body, extractFuncRegex);
            string innerArgs = match.Groups[1].Value;
            IList<string> matches = DataFilterParsingHelper.SplitArgs(innerArgs);
            foreach (string item in matches)
            {
                object arg = null;
                Match m = Regex.Match(item, extractFuncRegex);
                if (m != null && string.IsNullOrEmpty(m.Value) == false)
                {
                    arg = new FunctionHelper(m.Value);
                }
                else
                {
                    double d;
                    int v;
                    if (int.TryParse(item, out v) == true)
                    {
                        arg = v;
                    }
                    else if (double.TryParse(item, out d) == true)
                    {
                        arg = d;
                    }
                    else if (DataFilterParsingHelper.IsDateTimeOffset(item) == true)
                    {
                        arg = DateTimeOffset.Parse(item.Replace("'", string.Empty));
                    }
                    else
                    {
                        string str = item.Trim();
                        DateTimeOffset dresult;
                        if (str[0] == '\'')
                        {
                            string sub = str.Substring(1, str.Length - 2);
                            if (DateTimeOffset.TryParseExact(sub, "o", null, DateTimeStyles.None, out dresult) == true)
                            {
                                arg = dresult;
                            }
                            else
                            {
                                arg = sub;
                            }
                        }
                        else if (str[0] == '(')
                        {
                            FunctionHelper list = new FunctionHelper(string.Concat("foo", str));
                            arg = list.Arguments;
                        }
                        else if (DateTimeOffset.TryParseExact(str, "o", null, DateTimeStyles.None, out dresult) == true)
                        {
                            arg = dresult;
                        }
                        else if (str.Contains(" ") == true)
                        {
                            arg = DataFilterParsingHelper.ParseArithmetic(str);
                        }
                        else if (str.StartsWith("Edm.") == true)
                        {
                            arg = str;
                        }
                        else
                        {
                            PropertyNameType pn = new PropertyNameType();
                            arg = pn;
                            int lastslash = str.LastIndexOf('/');
                            if (lastslash > 0)
                            {
                                pn.Prefix = str.Substring(0, lastslash);
                                pn.Value = str.Substring(lastslash + 1);
                            }
                            else
                            {
                                pn.Value = str;
                            }
                        }
                    }
                }

                this.Arguments.Add(arg);
            }
        }

        /// <summary>
        /// Gets the name of the function.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of arguments.
        /// </summary>
        public List<object> Arguments
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the function is negated or not.
        /// </summary>
        public bool Negate
        {
            get;
            private set;
        }
    }
}
