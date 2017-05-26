// -----------------------------------------------------------------------
// <copyright file="QueryContext.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper class for working with uri's.
    /// </summary>
    public class QueryContext
    {
        /// <summary>
        /// Regex for locating expand list in url.
        /// </summary>
        private static Regex expandRegex = new Regex(@"(\$expand=)[^&]+");

        /// <summary>
        /// The current uri being executed.
        /// </summary>
        private Uri query;

        /// <summary>
        /// The expand segment root node.
        /// </summary>
        private Segment expand;

        /// <summary>
        /// Initializes a new instance of the QueryContext class.
        /// </summary>
        public QueryContext(Uri query)
        {
            this.query = query;
            this.expand = InitializeExpand();
        }

        /// <summary>
        /// Gets a value indicating whether the context is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return this.expand == null;
            }
        }

        /// <summary>
        /// Switch the expand context to one of the child properties.
        /// </summary>
        /// <param name="propertyName">The name of the child property.</param>
        public void Navigate(string propertyName)
        {
            if (this.expand != null)
            {
                this.expand = this.expand.Children
                    .Where(p => p.Name == propertyName)
                    .OrderByDescending(p => p.Children.Count())
                    .First();
            }
        }

        /// <summary>
        /// Resets the expand context to the parent of the current property.
        /// </summary>
        public void ResetToParent()
        {
            if (this.expand != null)
            {
                this.expand = this.expand.Parent;
            }
        }

        /// <summary>
        /// Determine whether current query requests an expand for a given property.
        /// </summary>
        /// <param name="propertyName">The name of the property to be tested.</param>
        /// <returns>True to expand, otherwise false.</returns>
        public bool Expand(string propertyName)
        {
            if (this.expand == null)
            {
                return false;
            }

            if (this.expand.Children.Any(p => p.Name == propertyName && p.GetType() == typeof(ExpandSegment)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determine whether current query can navigate to the named property.
        /// </summary>
        /// <param name="propertyName">The name of the property to be tested.</param>
        /// <returns>True to indicate navigation supported, otherwise false.</returns>
        public bool CanNavigate(string propertyName)
        {
            if (this.expand == null)
            {
                return false;
            }

            if (this.expand.Children.Any(p => p.Name == propertyName))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Read the expands for the node currently in scope.
        /// </summary>
        /// <returns>The expands text.</returns>
        internal string ReadExpand()
        {
            StringBuilder builder = new StringBuilder();
            string separator = string.Empty;
            foreach (Segment item in this.expand.Children)
            {
                ExpandSegment select = item as ExpandSegment;
                if (select != null)
                {
                    builder.Append(separator);
                    builder.AppendFormat(select.Name);
                    separator = ",";
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Read the filter for the node currently in scope.
        /// </summary>
        /// <returns>The filter text.</returns>
        internal string ReadFilter()
        {
            foreach (Segment item in this.expand.Children)
            {
                FilterSegment filter = item as FilterSegment;
                if (filter != null)
                {
                    return filter.Name;
                }
            }

            return null;
        }

        /// <summary>
        /// Read the selects for the node currently in scope.
        /// </summary>
        /// <returns>The selects text.</returns>
        internal string ReadSelects()
        {
            StringBuilder builder = new StringBuilder();
            string separator = string.Empty;
            foreach (Segment item in this.expand.Children)
            {
                SelectSegment select = item as SelectSegment;
                if (select != null)
                {
                    builder.Append(separator);
                    builder.AppendFormat(select.Name);
                    separator = ",";
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Read the top for the node currently in scope.
        /// </summary>
        /// <returns>The top text.</returns>
        internal string ReadTop()
        {
            foreach (Segment item in this.expand.Children)
            {
                TopSegment top = item as TopSegment;
                if (top != null)
                {
                    return top.Name;
                }
            }

            return null;
        }

        /// <summary>
        /// Read the skip for the node currently in scope.
        /// </summary>
        /// <returns>The skip text.</returns>
        internal string ReadSkip()
        {
            foreach (Segment item in this.expand.Children)
            {
                SkipSegment skip = item as SkipSegment;
                if (skip != null)
                {
                    return skip.Name;
                }
            }

            return null;
        }

        /// <summary>
        /// Read the orderbys for the node currently in scope.
        /// </summary>
        /// <returns>The selects text.</returns>
        internal string ReadOrderBys()
        {
            StringBuilder builder = new StringBuilder();
            string separator = string.Empty;
            foreach (Segment item in this.expand.Children)
            {
                OrderBySegment orderby = item as OrderBySegment;
                if (orderby != null)
                {
                    builder.Append(separator);
                    builder.AppendFormat(orderby.Name);
                    separator = ",";
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Transfer the context structure to the composite node.
        /// </summary>
        /// <param name="node">The node to traverse.</param>
        /// <param name="tableCreator">The opertion to create discovered tables.</param>
        internal void PopulateCompositeNodes(
            CompositeNode node, 
            Func<Type, string, QuerySource> tableCreator)
        {
            if (this.expand == null)
            {
                return;
            }

            Stack<Segment> stack = new Stack<Segment>(this.expand.Children);
            while (stack.Count > 0)
            {
                Segment segment = stack.Pop();
                if (segment is ExpandSegment)
                {
                    StringBuilder builder = new StringBuilder(segment.Name);
                    Segment test = segment.Parent;
                    while (test != null)
                    {
                        if (test.Name != "Root")
                        {
                            builder.Insert(0, "/");
                            builder.Insert(0, test.Name);
                        }

                        test = test.Parent;
                    }

                    CompositeNode target = node.Align(builder.ToString());
                    this.AssignExpandsConfiguration(target, segment);
                    QuerySource table = tableCreator(target.ElementType, target.GetFullPath());
                }

                foreach (Segment child in segment.Children)
                {
                    stack.Push(child);
                }
            }
        }

        /// <summary>
        /// Assign the expands configuration to the corresponding node.
        /// </summary>
        /// <param name="target">The node.</param>
        /// <param name="segment">The expand segment.</param>
        private void AssignExpandsConfiguration(CompositeNode target, Segment segment)
        {
            TopSegment topSegment = segment.Children.OfType<TopSegment>().SingleOrDefault();
            SkipSegment skipSegment = segment.Children.OfType<SkipSegment>().SingleOrDefault();
            IEnumerable<OrderBySegment> orderbys = segment.Children.OfType<OrderBySegment>();
            string top = topSegment == null ? null : topSegment.Name;
            string skip = skipSegment == null ? null : skipSegment.Name;
            target.SetConfiguration(top, skip, orderbys.Select(p => p.Name).ToList());
        }

        /// <summary>
        /// Set the initial expand value.
        /// </summary>
        /// <returns>The root expand property.</returns>
        private ExpandSegment InitializeExpand()
        {
            ExpandSegment segment = null;
            Match match = expandRegex.Match(Uri.UnescapeDataString(this.query.Query));
            if (match.Length > 0)
            {
                IEnumerable<Segment> results = Parse(match.Value);
                segment = new ExpandSegment();
                segment.Name = "Root";
                segment.AddChildren(results);
            }

            return segment;
        }

        /// <summary>
        /// Parse the data in a segment.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static IEnumerable<Segment> Parse(string options)
        {
            Stack<Segment> stack = new Stack<Segment>();
            Segment context = null;
            bool keyword = false;
            bool inquotes = false;

            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == '$' && inquotes == false)
                {
                    buffer.Clear();
                    keyword = true;
                }
                else if (keyword == true && options[i] == '=')
                {
                    string name = buffer.ToString();
                    context = CreateSegment(name);
                    stack.Push(context);
                    buffer.Clear();
                    keyword = false;
                    i++;
                }
                else if (keyword == true)
                {
                    buffer.Append(options[i]);
                }

                if (keyword == false)
                {
                    if (inquotes == false)
                    {
                        if (options[i] == ';')
                        {
                            context.Assign(buffer.ToString());
                            if (string.IsNullOrEmpty(context.Name) == true)
                            {
                                context.Name = buffer.ToString();
                            }

                            buffer.Clear();
                        }
                        else if (options[i] == '&')
                        {
                            context.Assign(buffer.ToString());
                            if (string.IsNullOrEmpty(context.Name) == true)
                            {
                                context.Name = buffer.ToString();
                            }

                            buffer.Clear();
                        }
                        else if (options[i] == '(')
                        {
                            string sub = options.Substring(i);
                            int pos = FindEndParens(sub);
                            string toparse = sub.Substring(0, pos);
                            i += pos;
                            if (context.Assign(toparse.Substring(1)) == true)
                            {
                                context.Name = buffer.ToString();
                                buffer.Clear();
                            }
                            else
                            {
                                buffer.Append(toparse);
                            }
                        }
                        else if (options[i] == ',')
                        {
                            if (string.IsNullOrEmpty(context.Name) == true)
                            {
                                context.Name = buffer.ToString();
                            }

                            context = Activator.CreateInstance(context.GetType()) as Segment;
                            stack.Push(context);
                            buffer.Clear();
                            i++;
                        }
                        else if (i == options.Length - 1)
                        {
                            buffer.Append(options[i]);
                            if (string.IsNullOrEmpty(context.Name) == true)
                            {
                                context.Name = buffer.ToString();
                            }

                            context.Assign(buffer.ToString());
                            buffer.Clear();
                        }
                    }

                    if (i < options.Length)
                    {
                        if (options[i] == '\'')
                        {
                            inquotes = !inquotes;
                        }

                        buffer.Append(options[i]);
                    }
                }
            }

            return stack.Reverse();
        }

        /// <summary>
        /// Finds the matching close parens for the current opens parens.
        /// </summary>
        /// <param name="test">The string test.</param>
        /// <returns>The position in the test string of the end parens.</returns>
        private static int FindEndParens(string test)
        {
            if (test[0] != '(')
            {
                throw new ArgumentException("Test string does not start with a parenthesis");
            }

            int level = 0;
            for (int i = 1; i < test.Length; i++)
            {
                if (test[i] == ')' && level == 0)
                {
                    return i;
                }
                else if (test[i] == ')')
                {
                    level--;
                }
                else if (test[i] == '(')
                {
                    level++;
                }
            }

            return -1;
        }

        /// <summary>
        /// Factory method to create segments.
        /// </summary>
        /// <param name="name">The keyword name to use for creating the segment.</param>
        /// <returns>The correlative segment.</returns>
        private static Segment CreateSegment(string name)
        {
            Segment result = null;
            switch (name)
            {
                case "expand":
                    result = new ExpandSegment();
                    break;

                case "select":
                    result = new SelectSegment();
                    break;

                case "search":
                    result = new SearchSegment();
                    break;

                case "top":
                    result = new TopSegment();
                    break;

                case "orderby":
                    result = new OrderBySegment();
                    break;

                case "skip":
                    result = new SkipSegment();
                    break;

                case "filter":
                    result = new FilterSegment();
                    break;

                case "levels":
                    result = new LevelsSegment();
                    break;

                case "count":
                    result = new CountSegment();
                    break;
            }

            return result;
        }

        /// <summary>
        /// Base class for all segments.
        /// </summary>
        private abstract class Segment
        {
            /// <summary>
            /// The stored children of the current segment.
            /// </summary>
            private List<Segment> children = new List<Segment>();

            /// <summary>
            /// Gets the list of children.
            /// </summary>
            public IEnumerable<Segment> Children
            {
                get
                {
                    return this.children;
                }
            }

            /// <summary>
            /// Gets or sets the name of the segement.
            /// </summary>
            public virtual string Name
            {
                get;
                set;
            }

            /// <summary>
            /// Gets the parent of the current segment.
            /// </summary>
            public Segment Parent
            {
                get;
                private set;
            }

            /// <summary>
            /// Assign data to the segment.
            /// </summary>
            /// <param name="data"></param>
            /// <returns>True if assignment made, otherwise false.</returns>
            public bool Assign(string data)
            {
                if (data.Contains('$') == true)
                {
                    IEnumerable<Segment> children = Parse(data);
                    this.AddChildren(children);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Add the list of nodes as children to the current segment.
            /// </summary>
            /// <param name="nodes">The list of nodes.</param>
            public void AddChildren(IEnumerable<Segment> nodes)
            {
                foreach (Segment child in nodes)
                {
                    child.Parent = this;
                    this.children.Add(child);
                }
            }
        }

        /// <summary>
        /// Class for ExpandSegments.
        /// </summary>
        private class ExpandSegment : Segment
        {
            /// <summary>
            /// The name of the expanded property.
            /// </summary>
            private string propertyName;

            /// <summary>
            /// Gets or sets the Name of the segment.
            /// </summary>
            public override string Name
            {
                get
                {
                    return this.propertyName;
                }

                set
                {
                    if (value != null)
                    {
                        int pos = value.IndexOf('/');
                        if (pos > 0)
                        {
                            this.propertyName = value.Substring(0, pos);
                            this.PropertyType = TypeCache.LocateType(value.Substring(pos + 1));
                        }
                        else
                        {
                            this.propertyName = value;
                        }
                    }
                }
            }

            /// <summary>
            /// Gets the derived type of the expanded property.
            /// </summary>
            public Type PropertyType
            {
                get;
                private set;
            }
        }

        /// <summary>
        /// Class for SelectSegments.
        /// </summary>
        private class SelectSegment : Segment
        {
        }

        /// <summary>
        /// Class for FilterSegments.
        /// </summary>
        private class FilterSegment : Segment
        {
        }

        /// <summary>
        /// Class for OrderBySegments.
        /// </summary>
        private class OrderBySegment : Segment
        {
        }

        /// <summary>
        /// Class for TopSegments.
        /// </summary>
        private class TopSegment : Segment
        {
        }

        /// <summary>
        /// Class for LevelsSegments.
        /// </summary>
        private class LevelsSegment : Segment
        {
        }

        /// <summary>
        /// Class for SkipSegments.
        /// </summary>
        private class SkipSegment : Segment
        {
        }

        /// <summary>
        /// Class for CountSegments.
        /// </summary>
        private class CountSegment : Segment
        {
        }

        /// <summary>
        /// Class for SearchSegments.
        /// </summary>
        private class SearchSegment : Segment
        {
        }
    }
}
