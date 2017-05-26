// -----------------------------------------------------------------------
// <copyright file="QueryJoin.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Enumeration of query join types.
    /// </summary>
    public enum QueryJoinType
    {
        Inner,
        Left,
        Right,
        InnerMerge,
    }

    /// <summary>
    /// Class for declaring a join.
    /// </summary>
    public sealed class QueryJoin
    {
        /// <summary>
        /// Initializes a new instance of the QueryJoin class.
        /// </summary>
        public QueryJoin()
        {
            this.Statements = new List<Tuple<QueryColumn, QueryColumn>>();
        }

        /// <summary>
        /// Initializes a new instance of the QueryJoin class.
        /// </summary>
        /// <param name="join">The joint to copy into this new one.</param>
        public QueryJoin(QueryJoin join)
        {
            this.IntermediateTable = join.IntermediateTable;
            this.JoinType = join.JoinType;
            this.Source = join.Source;
            this.SourceNode = join.SourceNode;
            this.Statements = join.Statements;
            this.Target = join.Target;
            this.TargetNode = join.TargetNode;
        }

        /// <summary>
        /// Gets or sets the type of the join.
        /// </summary>
        public QueryJoinType JoinType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source of the join.
        /// </summary>
        public QuerySource Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the target of the join.
        /// </summary>
        public QuerySource Target
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of join statements.
        /// </summary>
        public List<Tuple<QueryColumn, QueryColumn>> Statements
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the source node.
        /// </summary>
        internal CompositeNode SourceNode
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the target node.
        /// </summary>
        internal CompositeNode TargetNode
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the intermediate table.
        /// </summary>
        internal QueryTable IntermediateTable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets any exclusions to be applied to the join.
        /// </summary>
        internal OdataExpressionModel.FilterType Exclusions
        {
            get;
            set;
        }

        

        /// <summary>
        /// Set the statments for the source and target based on their composite nodes.
        /// </summary>
        /// <param name="parent">The source table node.</param>
        /// <param name="node">The target table node.</param>
        internal void SetStatements(CompositeNode parent, CompositeNode node)
        {
            QueryTable intermediateTable = null;
            Dictionary<string, string> preset = TypeCache.LocateJoin(parent, node, out intermediateTable);
            if (intermediateTable != null)
            {
                this.IntermediateTable = new QueryTable()
                {
                    Name = intermediateTable.Name,
                    Schema = intermediateTable.Schema,
                    Alias = string.Concat(this.Source.Alias, "To", this.Target.Alias)
                };
            }

            this.SourceNode = parent;
            this.TargetNode = node;
            foreach (string key in preset.Keys)
            {
                QueryColumn sqc = new QueryColumn() { Source = this.Source, Name = key };
                QueryColumn tqc = new QueryColumn() { Source = this.Target, Name = preset[key] };
                this.Statements.Add(new Tuple<QueryColumn, QueryColumn>(sqc, tqc));
                if (this.IntermediateTable != null)
                {
                    sqc.Source = this.IntermediateTable;
                    tqc.Source = this.IntermediateTable;
                }
            }
        }
    }
}
