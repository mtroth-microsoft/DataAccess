// -----------------------------------------------------------------------
// <copyright file="InfrastructureController.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.OData;
    using OdataExpressionModel;

    /// <summary>
    /// A base controller for use by any datasource.
    /// </summary>
    public class InfrastructureController : ODataController, IDatasourceAwareController
    {
        /// <summary>
        /// Message for missing required data filters.
        /// </summary>
        private const string RequiredMessage = "{0} is a required data filter in this context.";

        /// <summary>
        /// Message for unsupported data filters.
        /// </summary>
        private const string UnsupportedMessage = "{0} is not a supported data filter in this context.";

        /// <summary>
        /// Gets or sets the datasource for the controller.
        /// </summary>
        public IDatasource Datasource
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the error handler.
        /// </summary>
        public IErrorHandler ErrorHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Build the id list from the $filter string.
        /// </summary>
        /// <param name="propertyName">The name of the property to scrape for ids.</param>
        /// <returns>The list of ids.</returns>
        protected List<int> BuildIdList(string propertyName)
        {
            FilterType filter = DataFilterParsingHelper.ParseFilter(this.Url.Request.RequestUri);

            List<int> ids = null;
            if (filter != null)
            {
                ids = DataFilterParsingHelper.ExtractListOfIds(filter.Item, propertyName);
                ids = ids.Count > 0 ? ids : null;
            }

            return ids;
        }

        /// <summary>
        /// Build the map of parameters for the current request.
        /// </summary>
        /// <returns>The parameter map.</returns>
        protected Dictionary<string, object> BuildMap()
        {
            return this.BuildValidMap(null);
        }

        /// <summary>
        /// Build the map of parameters for the current request and validate its inputs.
        /// </summary>
        /// <param name="edmElementName">The name of the calling element (operation or entity set).</param>
        /// <returns>The parameter map.</returns>
        protected Dictionary<string, object> BuildValidMap(string edmElementName)
        {
            FilterType where = DataFilterParsingHelper.ParseWhere(this.Url.Request.RequestUri);
            Dictionary<string, object> map = DataFilterParsingHelper.ExtractNameValuePairs(
                where == null ? null : where.Item);

            this.ValidateMap(edmElementName, map);

            return map;
        }

        /// <summary>
        /// Build the map of parameters for the current request and validate its inputs.
        /// </summary>
        /// <param name="edmElementName">The name of the calling element (operation or entity set).</param>
        /// <returns>The parameter map.</returns>
        protected Dictionary<string, object> BuildValidPredicateMap(string edmElementName)
        {
            FilterType where = DataFilterParsingHelper.ParseWhere(this.Url.Request.RequestUri);
            List<ParsedPredicate> predicates = DataFilterParsingHelper.Flatten(where);
            Dictionary<string, object> map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (ParsedPredicate pp in predicates)
            {
                IEnumerable<string> names = pp.PropertyNames.Select(p => p.Serialize());
                foreach (string name in names)
                {
                    if (map.ContainsKey(name) == false)
                    {
                        map.Add(name, pp.Value);
                    }
                }
            }

            this.ValidateMap(edmElementName, map);

            return map;
        }

        /// <summary>
        /// Validate the value map to ensure it matches with configured settings.
        /// </summary>
        /// <param name="edmElementName">The edm element to validate.</param>
        /// <param name="valueMap">The value map.</param>
        private void ValidateMap(string edmElementName, Dictionary<string, object> valueMap)
        {
            if (string.IsNullOrEmpty(edmElementName) == false)
            {
                string message;
                if (this.ValidateDataFilters(edmElementName, valueMap, out message) == false)
                {
                    throw new InvalidDataFilterException(message);
                }
            }
        }

        /// <summary>
        /// Validate the data filters for a given edm element.
        /// </summary>
        /// <param name="edmElementName">The entity set or function name to inspect.</param>
        /// <param name="map">The map of parameter values.</param>
        /// <param name="message">The validation message, if applicable.</param>
        /// <returns>True if the parameters are all valid, otherwise false.</returns>
        private bool ValidateDataFilters(
            string edmElementName,
            Dictionary<string, object> map,
            out string message)
        {
            message = null;
            InfrastructureConfigType config = this.Datasource.GetConfig();
            if (config != null &&
                config.DataFilters != null &&
                config.DataFilters.EdmElements != null)
            {
                DataFilterConfigurationItemType item = config.DataFilters.EdmElements
                    .Where(p => p.Name.Equals(edmElementName, StringComparison.OrdinalIgnoreCase) == true)
                    .SingleOrDefault();

                if (item != null)
                {
                    IEnumerable<DataFilterParameterType> parameters = item.Parameters
                        .Where(p => p.Required == true);
                    foreach (DataFilterParameterType parameter in parameters)
                    {
                        if (map.ContainsKey(parameter.Name) == false)
                        {
                            message = string.Format(RequiredMessage, parameter.Name) + CreateElementList(item);
                            return false;
                        }
                    }

                    foreach (string key in map.Keys)
                    {
                        string masked = key;
                        int pos = key.LastIndexOf('/');
                        if (pos > 0)
                        {
                            masked = "*" + key.Substring(pos);
                        }

                        if (item.Parameters.Any(p => p.Name == key || p.Name == masked) == false)
                        {
                            message = string.Format(UnsupportedMessage, key) + CreateElementList(item);
                            return false;
                        }
                    }

                    return true;
                }
            }

            if (map.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Create the element list of supported data filters.
        /// </summary>
        /// <param name="item">The item to inspect.</param>
        /// <returns>The supported list.</returns>
        private string CreateElementList(DataFilterConfigurationItemType item)
        {
            string[] list = item.Parameters
                .Select(p => p.Name + " " + (p.Required == true ? "is Required" : "is Optional"))
                .ToArray();

            return " " + string.Join(" ", list);
        }
    }
}
