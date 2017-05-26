// -----------------------------------------------------------------------
// <copyright file="AggregateColumnReference.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using OdataExpressionModel;

    /// <summary>
    /// Helper class to declare an aggregate column.
    /// </summary>
    public sealed class AggregateColumnReference
    {
        /// <summary>
        /// Initializes an instance of the AggregateColumnReference class.
        /// </summary>
        /// <param name="propertyName">The property name. Should be the full name, using odata semantics.</param>
        /// <param name="aggregate">The aggregate type of the property.</param>
        /// <param name="alias">The alias for the query, defaults to null.</param>
        public AggregateColumnReference(string propertyName, AggregateType aggregate, string alias = null)
        {
            PropertyNameType pnt = new PropertyNameType() { Value = propertyName, Alias = alias };
            int pos = propertyName.LastIndexOf('/');
            if (pos > 0)
            {
                pnt.Value = propertyName.Substring(pos + 1);
                pnt.Prefix = propertyName.Substring(0, pos);
            }

            IEnumerable<FromType> froms;
            this.Alias = ApplyAlias(alias ?? pnt.Value, out froms);
            this.AggregateType = aggregate;
            this.Predicatable = pnt;
            this.Froms = froms;
        }

        /// <summary>
        /// Initializes an instance of the AggregateColumnReference class.
        /// </summary>
        /// <param name="predicatable">The predicatable for the reference.</param>
        /// <param name="aggregate">The aggregate type of the property.</param>
        /// <param name="alias">The alias for the query, defaults to null.</param>
        internal AggregateColumnReference(IPredicatable predicatable, AggregateType aggregate, string alias = null)
        {
            IEnumerable<FromType> froms;
            this.Alias = ApplyAlias(alias ?? string.Empty, out froms);
            this.AggregateType = aggregate;
            this.Predicatable = predicatable;
            this.Froms = froms;
            if (string.IsNullOrEmpty(alias) == true && this.Predicatable.LocatePropertyNames().Count > 1)
            {
                throw new ArgumentNullException("alias", "An alias must be provided when multiple columns are involved in the aggregate.");
            }
        }

        /// <summary>
        /// Gets the aggregate type of the property.
        /// </summary>
        public AggregateType AggregateType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the alias for the aggregate.
        /// </summary>
        internal string Alias
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the predicatable.
        /// </summary>
        internal IPredicatable Predicatable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of froms for the current aggregate column reference.
        /// </summary>
        internal IEnumerable<FromType> Froms
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the column to which the Aggregate refers.
        /// </summary>
        internal QueryColumn Column
        {
            get;
            set;
        }

        /// <summary>
        /// Remove any from references from the alias.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <param name="froms">The discovered from list.</param>
        /// <returns>The normalized alias.</returns>
        private static string ApplyAlias(string value, out IEnumerable<FromType> froms)
        {
            int pos = value.IndexOf(" from ");
            if (pos > 0)
            {
                froms = ExtractFroms(value.Substring(pos));
                return value.Substring(0, pos);
            }

            froms = null;
            return value;
        }

        /// <summary>
        /// Extract each of the from statments from the value.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>The list of with types.</returns>
        private static IEnumerable<FromType> ExtractFroms(string value)
        {
            List<FromType> froms = new List<FromType>();
            int pos = 0;
            while (pos >= 0)
            {
                int startpos = pos + 6;
                pos = value.IndexOf(" from ", startpos);
                int endpos = pos < 0 ? value.Length - startpos : pos - startpos;
                string data = value.Substring(startpos, endpos);

                string[] tokens = data.Split(' ');
                AggregateType agg;
                if (Enum.TryParse<AggregateType>(tokens[2], true, out agg) == true)
                {
                    FromType ft = new FromType();
                    ft.AggregateType = agg;
                    ft.Name = tokens[0];
                    froms.Add(ft);
                }
            }

            return froms;
        }
    }
}
