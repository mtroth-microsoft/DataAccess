// -----------------------------------------------------------------------
// <copyright file="ApplyParsingHelper.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// Parser for $apply.
    /// </summary>
    internal class ApplyParsingHelper
    {
        /// <summary>
        /// Parse the apply query option.
        /// </summary>
        /// <param name="apply">The apply value.</param>
        public static void ParseApply(string apply)
        {
            string[] parts = Split(apply, '/', new List<char>());
            foreach (string part in parts)
            {
                List<Token> tokens = CreateTokenList(part);
            }
        }

        /// <summary>
        /// Creates a list of tokens from the given filter statement.
        /// </summary>
        /// <param name="filter">The filter to inspect.</param>
        /// <returns>The resulting list of tokens.</returns>
        private static List<Token> CreateTokenList(string filter)
        {
            bool quoted = false;
            bool function = false;
            int depth = 0;

            List<Token> tokens = new List<Token>();
            StringBuilder token = new StringBuilder();
            foreach (char c in filter)
            {
                if (c == '\'' && quoted == false)
                {
                    token.Append(c);
                    quoted = true;
                }
                else if (c == '\'' && quoted == true)
                {
                    token.Append(c);
                    quoted = false;
                }
                else if (c == '(' && quoted == false && function == true)
                {
                    depth++;
                    token.Append(c);
                }
                else if (c == '(' && quoted == false)
                {
                    function = IsFunction(token.ToString());
                    if (function == false)
                    {
                        if (token.Length > 0)
                        {
                            tokens.Add(new SemanticToken(token.ToString()));
                            token.Clear();
                        }

                        tokens.Add(new Token(c.ToString()));
                        continue;
                    }
                    else
                    {
                        token.Append(c);
                    }
                }
                else if (c == ')' && quoted == false && depth > 0)
                {
                    depth--;
                    token.Append(c);
                }
                else if (c == ')' && quoted == false)
                {
                    if (function == false)
                    {
                        if (token.Length > 0)
                        {
                            tokens.Add(new SemanticToken(token.ToString()));
                            token.Clear();
                        }

                        tokens.Add(new Token(c.ToString()));
                        continue;
                    }
                    else
                    {
                        function = false;
                        token.Append(c);
                        tokens.Add(new SemanticToken(token.ToString()));
                        token.Clear();
                    }
                }
                else if (c == ' ' && quoted == false && function == false)
                {
                    if (token.Length > 0)
                    {
                        tokens.Add(new SemanticToken(token.ToString()));
                        token.Clear();
                    }
                }
                else
                {
                    token.Append(c);
                }
            }

            if (token.Length > 0)
            {
                tokens.Add(new SemanticToken(token.ToString()));
            }

            return tokens;
        }

        /// <summary>
        /// Split the given string using the provided char. 
        /// Omitting characters inside single quotes or inside parens.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="split">The split character.</param>
        /// <param name="ignores">The list of chars to ignore at the root level of the string.</param>
        /// <returns>The list of tokens.</returns>
        private static string[] Split(string input, char split, List<char> ignores)
        {
            List<string> tokens = new List<string>();
            bool quoted = false;
            int level = 0;
            StringBuilder token = new StringBuilder();

            foreach (char c in input)
            {
                // Causes problems if there is an apostrophe inside the quoted string.
                if (c == '\'')
                {
                    quoted = quoted == false ? true : false;
                }
                else if (c == '(')
                {
                    level++;
                }
                else if (c == ')')
                {
                    level--;
                }

                if (ignores.Contains(c) == true && quoted == false && level == 0)
                {
                    continue;
                }
                else if (c == split && quoted == false && level == 0)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }
                else
                {
                    token.Append(c);
                }
            }

            if (token.Length > 0)
            {
                tokens.Add(token.ToString());
            }

            return tokens.ToArray();
        }

        /// <summary>
        /// Tests string against list of supported functions.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if function, otherwise false.</returns>
        private static bool IsFunction(string value)
        {
            return value.Equals("aggregate", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("topcount", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("topsum", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("toppercent", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("bottomcount", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("bottomsum", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("bottompercent", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("identity", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("concat", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("groupby", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("filter", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("expand", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("search", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("rollup", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Helper class for storing token value and level.
        /// </summary>
        private class Token
        {
            /// <summary>
            /// Initializes an instance of the Token class.
            /// </summary>
            /// <param name="value">The value of the token.</param>
            public Token(string value)
            {
                this.Children = new List<Token>();
                this.Value = value;
            }

            /// <summary>
            /// Gets the token value.
            /// </summary>
            public string Value
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets teh list of children nodes.
            /// </summary>
            public List<Token> Children
            {
                get;
                private set;
            }

            /// <summary>
            /// Override of the tostring method.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Value;
            }
        }

        /// <summary>
        /// Helper class for storing token value and level for semantic elements.
        /// </summary>
        private class SemanticToken : Token
        {
            /// <summary>
            /// Initializes an instance of the SemanticToken class.
            /// </summary>
            /// <param name="value">The value of the token.</param>
            public SemanticToken(string value)
                : base(value)
            {
                this.Function = new FunctionHelper(value);
                Stack<FunctionHelper> helpers = new Stack<FunctionHelper>();
                helpers.Push(this.Function);
                while (helpers.Count > 0)
                {
                    FunctionHelper function = helpers.Pop();
                    foreach (object item in function.Arguments)
                    {
                        FunctionHelper f = item as FunctionHelper;
                        PropertyNameType pnt = item as PropertyNameType;
                        if (f != null)
                        {
                            helpers.Push(f);
                        }
                        else if (pnt != null)
                        {
                            string pname = pnt.Value;
                            FilterType argFilter = DataFilterParsingHelper.ParseArguments(new Uri("http://foo?aggregate=" + pname), null);
                        }
                    }
                }
            }

            /// <summary>
            /// Gets the parsed function data.
            /// </summary>
            public FunctionHelper Function
            {
                get;
                private set;
            }
        }
    }
}
