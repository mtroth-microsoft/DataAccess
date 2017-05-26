// -----------------------------------------------------------------------
// <copyright file="InfrastructureQueryable.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// The infrastructure queryable class.
    /// </summary>
    internal class InfrastructureQueryable<T> :
        IQueryable<T>,
        IQueryable,
        IEnumerable<T>,
        IEnumerable,
        IOrderedQueryable<T>,
        IOrderedQueryable
    {
        /// <summary>
        /// The list of internal dbsets.
        /// </summary>
        private List<DbQuery<T>> dbsets = new List<DbQuery<T>>();

        /// <summary>
        /// The list of internal object queries.
        /// </summary>
        private List<ObjectQuery<T>> objqueries = new List<ObjectQuery<T>>();

        /// <summary>
        /// Initializes a new instance of the InfrastructureQueryable class.
        /// </summary>
        /// <param name="dbfirst">True if uses dbcontext, otherwise false.</param>
        public InfrastructureQueryable(bool dbcontext)
        {
            this.Expression = Expression.Constant(this);
            if (dbcontext == true)
            {
                this.Provider = new InfrastructureQueryProvider(this.dbsets, typeof(T));
            }
            else
            {
                this.Provider = new InfrastructureQueryProvider(this.objqueries, typeof(T));
            }
        }

        /// <summary>
        /// Initializes a new instance of the InfrastructureQueryable class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="expression">The expression.</param>
        public InfrastructureQueryable(
            InfrastructureQueryProvider provider, 
            Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }

            this.Provider = provider;
            this.Expression = expression;
            this.dbsets = provider.State as List<DbQuery<T>>;
            this.objqueries = provider.State as List<ObjectQuery<T>>;
        }

        /// <summary>
        /// Gets the element type.
        /// </summary>
        public Type ElementType
        {
            get
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression Expression
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        public IQueryProvider Provider
        {
            get;
            private set;
        }

        /// <summary>
        /// Add a dbset to the queryble.
        /// </summary>
        /// <param name="query">The query to add.</param>
        public void Add(DbQuery<T> query)
        {
            this.dbsets.Add(query);
        }

        /// <summary>
        /// Add an object query to the queryble.
        /// </summary>
        /// <param name="query">The query to add.</param>
        public void Add(ObjectQuery<T> query)
        {
            this.objqueries.Add(query);
        }

        /// <summary>
        /// Get the enumerator for the current query.
        /// </summary>
        /// <returns>The relevant enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            List<T> results = new List<T>();
            if (this.dbsets != null || this.objqueries != null)
            {
                if (this.dbsets.Count > 0)
                {
                    results = this.ExecuteDbSets();
                }
                else
                {
                    results = this.ExecuteObjectQueries();
                }
            }
            else
            {
                // in this block we are dealing with some special case
                // resulting from particular characteristics in the linq query.
                // The location of the proxies, therefore, has been altered.
                results = ExecuteAlteredSets();
            }

            return results.GetEnumerator();
        }

        /// <summary>
        /// Gets the untyped enumerator for the current query.
        /// </summary>
        /// <returns>The untyped enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Run queries for sets that have been altered.
        /// This happens during the resolution of $select.
        /// </summary>
        /// <returns>The list of select generated instances.</returns>
        private List<T> ExecuteAlteredSets()
        {
            List<T> results = new List<T>();

            // The proxies are not local, but they will be on the Provider passed to the constructor.
            // Since we are handling the $select case, they will not be of the original generic type.
            InfrastructureQueryProvider qp = this.Provider as InfrastructureQueryProvider;
            Dictionary<int, object> map = new Dictionary<int, object>();
            IEnumerable queries = qp.State as IEnumerable;
            IEnumerator iterator = queries.GetEnumerator();
            int maxcount = 0;
            while (iterator.MoveNext() == true)
            {
                map[maxcount] = iterator.Current;
                maxcount++;
            }

            System.Threading.Tasks.Parallel.For(
                0,
                maxcount,
                p =>
                {
                    object item = map[p];
                    QueryTranslator translator = new QueryTranslator(item);
                    Expression translated = translator.Translate(this.Expression);
                    IQueryable<T> nested = (item as IQueryable).Provider.CreateQuery(translated) as IQueryable<T>;
                    results.AddRange(nested);
                });

            return results;
        }

        /// <summary>
        /// Run queries when they have defined via ObjectContext.
        /// </summary>
        /// <returns>The list of results.</returns>
        private List<T> ExecuteObjectQueries()
        {
            List<T> results = new List<T>();
            System.Threading.Tasks.Parallel.ForEach(
                this.objqueries,
                p =>
                {
                    QueryTranslator translator = new QueryTranslator(p);
                    Expression translated = translator.Translate(this.Expression);
                    IQueryable<T> nested = p.AsQueryable<T>().Provider.CreateQuery<T>(translated);
                    results.AddRange(nested);
                });

            return results;
        }

        /// <summary>
        /// Run queries when they have defined via DbContext.
        /// </summary>
        /// <returns>The list of results.</returns>
        private List<T> ExecuteDbSets()
        {
            List<T> results = new List<T>();
            System.Threading.Tasks.Parallel.ForEach(
                this.dbsets,
                p =>
                {
                    QueryTranslator translator = new QueryTranslator(p);
                    Expression translated = translator.Translate(this.Expression);
                    IQueryable<T> nested = p.AsQueryable<T>().Provider.CreateQuery<T>(translated);
                    results.AddRange(nested);
                });

            return results;
        }
    }
}
