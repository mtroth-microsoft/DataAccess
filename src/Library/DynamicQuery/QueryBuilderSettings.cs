// -----------------------------------------------------------------------
// <copyright file="QueryBuilderSettings.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OdataExpressionModel;

    /// <summary>
    /// Helper class to encapsulate some query builder knobs.
    /// </summary>
    public sealed class QueryBuilderSettings
    {
        /// <summary>
        /// Initializes a new instance of the QueryBuilderSettings class.
        /// </summary>
        public QueryBuilderSettings()
        {
            this.Aggregates = new List<AggregateColumnReference>();
            this.Groupings = new List<GroupByReferenceType>();
            this.Selects = new List<string>();
            this.Orderings = new List<OrderedPropertyType>();
            this.Executor = new TSqlQueryExecution();
        }

        /// <summary>
        /// Gets or sets the table creation template to use.
        /// </summary>
        public string Template
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of aggregate column references.
        /// </summary>
        public List<AggregateColumnReference> Aggregates
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of groupby properties.
        /// </summary>
        public List<GroupByReferenceType> Groupings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the Filter instance.
        /// </summary>
        public FilterType Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Where instance.
        /// </summary>
        public FilterType Where
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the argument filter instance.
        /// </summary>
        public FilterType ArgumentFilter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the top count.
        /// </summary>
        internal long? Top
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the skip count.
        /// </summary>
        internal long? Skip
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the query context.
        /// </summary>
        internal QueryContext QueryContext
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of select properties.
        /// </summary>
        internal List<string> Selects
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of orderby column references.
        /// </summary>
        internal List<OrderedPropertyType> Orderings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the query processing operation.
        /// This is called by the QueryBuilder after it has finished parsing the url.
        /// </summary>
        internal Func<SelectQuery, SelectQuery> QueryProcessor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the url upon which this object is based.
        /// </summary>
        internal Uri Url
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the first entity set type in the url's path.
        /// </summary>
        internal Type EntitySetType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default join type to use.
        /// </summary>
        internal QueryJoinType DefaultJoinType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the settings are for an aggregate query.
        /// </summary>
        internal bool IsAggregateQuery
        {
            get
            {
                return this.Groupings.Count > 0 ||
                    this.Aggregates.Where(p => p.AggregateType != AggregateType.None).Count() > 0;
            }
        }

        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        internal IExecutor Executor
        {
            get;
            set;
        }

        /// <summary>
        /// The list of registered UserDefinedFunctions.
        /// </summary>
        /// <returns>the map of functions.</returns>
        internal static Dictionary<string, UserDefinedFunction> UserDefinedFunctions()
        {
            return udfMap;
        }

        /// <summary>
        /// Private storage of the udf map.
        /// </summary>
        private static Dictionary<string, UserDefinedFunction> udfMap =
            new Dictionary<string, UserDefinedFunction>(StringComparer.OrdinalIgnoreCase)
            {
                { "p.getquantile", new UserDefinedFunction() { Name = "P.GetQuantile", Format = "[dbo].[MergeScenariosDigest]([dbo].[BinaryToScenariosDigest]({0})).GetQuantile({1})" } },
                { "p.variance", new UserDefinedFunction() { Name = "P.Variance", Format = "[dbo].[MergeScenariosDigest]([dbo].[BinaryToScenariosDigest]({0})).Variance" } },
                { "p.average", new UserDefinedFunction() { Name = "P.Average", Format = "[dbo].[MergeScenariosDigest]([dbo].[BinaryToScenariosDigest]({0})).Average" } },
                { "p.minimum", new UserDefinedFunction() { Name = "P.Minimum", Format = "[dbo].[MergeScenariosDigest]([dbo].[BinaryToScenariosDigest]({0})).Minimum" } },
                { "p.maximum", new UserDefinedFunction() { Name = "P.Maximum", Format = "[dbo].[MergeScenariosDigest]([dbo].[BinaryToScenariosDigest]({0})).Maximum" } },
                { "p.cardinality", new UserDefinedFunction() { Name = "P.Cardinality", Format = "[dbo].[MergeScenariosDigest]([dbo].[BinaryToScenariosDigest]({0})).Cardinality" } },
                { "eh.cardinality", new UserDefinedFunction() { Name = "EH.Cardinality", Format = "CAST([dbo].[MergeEnumerationHistogram]([dbo].[UdfBinaryToEnumerationHistogram]({0})).Cardinality AS float)" } },
                { "eh.tostring", new UserDefinedFunction() { Name = "EH.ToString", Format = "[dbo].[MergeEnumerationHistogram]([dbo].[UdfBinaryToEnumerationHistogram]({0})).ToString()" } },
            };
    }
}
