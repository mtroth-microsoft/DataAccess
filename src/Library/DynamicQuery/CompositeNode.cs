// -----------------------------------------------------------------------
// <copyright file="CompositeNode.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// CompositeNode class declaration.
    /// </summary>
    internal sealed class CompositeNode
    {
        /// <summary>
        /// The list of nodes.
        /// </summary>
        private List<CompositeNode> nodes = new List<CompositeNode>();

        /// <summary>
        /// Initializes a new instance of the CompositeNode class.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="path">The path for the node.</param>
        /// <param name="type">The type of the node.</param>
        /// <param name="subselect">True if the compositenode represents a subselect.</param>
        public CompositeNode(CompositeNode parent, string path, Type type, bool subselect)
        {
            this.IsSubSelect = subselect;
            this.Path = path;
            this.Parent = parent;

            if (parent == null)
            {
                this.ComponentId = "0";
            }
            else
            {
                this.ComponentId = string.Concat(parent.ComponentId, ".", parent.Nodes.Count);
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) == true && type != typeof(string))
            {
                this.ElementType = type.GenericTypeArguments[0];
                this.IsCollection = true;
            }
            else
            {
                this.ElementType = type;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the node data is a collection.
        /// </summary>
        public bool IsCollection
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the node describes a sub select.
        /// </summary>
        public bool IsSubSelect
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the path of the node.
        /// </summary>
        public string Path
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the component id.
        /// </summary>
        public string ComponentId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the element type.
        /// </summary>
        public Type ElementType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the parent node.
        /// </summary>
        /// <value>The return value.</value>
        public CompositeNode Parent
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of nodes.
        /// </summary>
        /// <value>The return value.</value>
        public List<CompositeNode> Nodes
        {
            get
            {
                return this.nodes;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the node is a reverse relation to its parent.
        /// </summary>
        public bool Reverse
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the top value, if applicable.
        /// </summary>
        public int? Top
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the skip value, if applicable.
        /// </summary>
        public int? Skip
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of orderbys.
        /// </summary>
        public ICollection<string> OrderBys
        {
            get;
            private set;
        }

        /// <summary>
        /// Set the expands configuration for the current node.
        /// </summary>
        /// <param name="top">The top.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="orderbys">The list of orderbys.</param>
        internal void SetConfiguration(string top, string skip, List<string> orderbys)
        {
            this.Top = top == null ? (int?)null : int.Parse(top);
            this.Skip = skip == null ? (int?)null : int.Parse(skip);
            this.OrderBys = orderbys.AsReadOnly();
        }

        /// <summary>
        /// Align the given prefix with the current tree structure.
        /// Use this method if you are doing normal filter evaluation
        /// where paths are all constructed forward from the query's entitytype.
        /// </summary>
        /// <param name="prefix">The prefix to align.</param>
        /// <param name="subselect">True if aligning a subselect operation.</param>
        /// <returns>The aligned node.</returns>
        internal CompositeNode Align(string prefix, bool subselect = false)
        {
            return this.Align(null, prefix, subselect);
        }

        /// <summary>
        /// Align the given prefix with the current tree structure.
        /// Use this method if you are doing key filter based evaluation
        /// where paths are all constructed in reverse from the query's entitytype
        /// back to the root entity set on the url's path.
        /// </summary>
        /// <param name="type">The type of the node to align. This value must be null if the prefix is forward.</param>
        /// <param name="prefix">The prefix to align.</param>
        /// <param name="subselect">True if aligning a subselect operation.</param>
        /// <returns>The aligned node.</returns>
        internal CompositeNode Align(Type type, string prefix, bool subselect)
        {
            if (string.IsNullOrEmpty(prefix) == false)
            {
                prefix = string.Concat("/", prefix);
            }

            string[] levels = prefix.Split('/');
            CompositeNode cxt = this;

            for (int i = 0; i < levels.Length; i++)
            {
                if (cxt.Path.Equals(levels[i], StringComparison.OrdinalIgnoreCase) == true && levels.Length > i + 1)
                {
                    CompositeNode test = cxt
                        .Nodes
                        .SingleOrDefault(p => p.Path.Equals(levels[i + 1], StringComparison.OrdinalIgnoreCase) == true);

                    if (test == null)
                    {
                        TypeCache.CheckIsLegalColumn(levels[i + 1], type ?? cxt.ElementType);
                        Type propertyType = TypeCache.LocatePropertyType(type ?? cxt.ElementType, levels[i + 1]);
                        if (propertyType != null)
                        {
                            test = new CompositeNode(cxt, levels[i + 1], type ?? propertyType, subselect);
                            cxt.Nodes.Add(test);
                            cxt = test;
                            if (type != null)
                            {
                                test.Reverse = true;
                            }
                        }
                    }
                    else
                    {
                        cxt = test;
                    }
                }
            }

            if (cxt != null && cxt.IsSubSelect == true && subselect == false)
            {
                cxt.IsSubSelect = false;
            }

            return cxt;
        }

        /// <summary>
        /// Gets the full path for the current node.
        /// </summary>
        /// <returns>The full path.</returns>
        internal string GetFullPath()
        {
            StringBuilder builder = new StringBuilder(this.Path);
            CompositeNode node = this.Parent;
            while (node != null)
            {
                if (string.IsNullOrEmpty(node.Path) == false)
                {
                    builder.Insert(0, "/");
                    builder.Insert(0, node.Path);
                }

                node = node.Parent;
            }

            return builder.ToString();
        }
    }
}
