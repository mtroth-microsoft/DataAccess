// -----------------------------------------------------------------------
// <copyright file="OdataQueryType.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Corresponds to OdataQueryType in model.
    /// </summary>
    public partial class ODataQueryType
    {
        /// <summary>
        /// The property name on jobject to parse.
        /// </summary>
        private const string JObjectValue = "value";

        /// <summary>
        /// Creates an instance from a provided url.
        /// </summary>
        /// <param name="url">The url to use.</param>
        /// <param name="endpoint">The endpoint portion of the url. 
        /// Can be null if query is an entityset query or a function/action that is bound to an entityset.</param>
        /// <param name="returnType">The return type of the call. Can be null if query is an entityset query.</param>
        /// <returns>The correlated instance.</returns>
        public static ODataQueryType CreateFromUrl(Uri url, Uri endpoint, Type returnType)
        {
            ODataQueryType query = new ODataQueryType();
            query.Name = null;
            query.Namespace = null;

            query.OrderBy = DataFilterParsingHelper.ParseOrderBy(url);
            query.GroupBy = DataFilterParsingHelper.ParseGroupBy(url);
            query.Where = DataFilterParsingHelper.ParseWhere(url);
            query.Filter = DataFilterParsingHelper.ParseFilter(url);

            Dictionary<string, string> options = DataFilterParsingHelper.ParseQueryOptions(url);
            ParseCount(query, options);
            ParseFormat(query, options);
            ParseSelect(query, options, url);
            ParseTopAndSkip(query, options);
            ParseExpand(query, options, url);
            ParseAggregate(query, url);
            ParseElement(query, url, endpoint, returnType);
            ParseCustom(query, options);

            return query;
        }

        /// <summary>
        /// Get the data for the query.
        /// </summary>
        /// <typeparam name="T">The type of the return collection.</typeparam>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns>The list of results.</returns>
        public virtual IEnumerable<T> Get<T>(
            Uri root,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            List<T> results = new List<T>();
            string json = GetData(odata, "GET", authParameters);

            JObject obj = JObject.Parse(json);
            JArray array = obj[JObjectValue] as JArray;
            foreach (JToken item in array)
            {
                string data = item.ToString();
                T o = DeserializeJson<T>(data);
                results.Add(o);
            }

            JToken count;
            if (obj.TryGetValue("@odata.count", out count) == true && parameters != null)
            {
                parameters["@odata.count"] = count.Value<int>();
            }

            return results;
        }

        /// <summary>
        /// Get the data for the query.
        /// </summary>
        /// <typeparam name="T">The type of the return instance.</typeparam>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns>The list of results.</returns>
        public virtual T GetSingle<T>(
            Uri root,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            string json = GetData(odata, "GET", authParameters);
            T result = DeserializeJson<T>(json);

            return result;
        }

        /// <summary>
        /// Post the data for the query.
        /// </summary>
        /// <typeparam name="T">The type of the return collection.</typeparam>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="payload">The payload for the post.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns></returns>
        public virtual IEnumerable<T> Post<T>(
            Uri root,
            object payload,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            List<T> results = new List<T>();
            string json = PostData(odata, payload, "POST", false, authParameters);
            JArray array = JObject.Parse(json)[JObjectValue] as JArray;
            foreach (JToken item in array)
            {
                string data = item.ToString();
                T o = DeserializeJson<T>(data);
                results.Add(o);
            }

            return results;
        }

        /// <summary>
        /// Post the data for the query.
        /// </summary>
        /// <typeparam name="T">The type of the return collection.</typeparam>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="payload">The payload for the post.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns></returns>
        public virtual T Post<T>(
            Uri root,
            T payload,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            string json = PostData(odata, payload, "POST", false, authParameters);
            T result = DeserializeJson<T>(json);

            return result;
        }

        /// <summary>
        /// Put the data for the query.
        /// </summary>
        /// <typeparam name="T">The type of the return collection.</typeparam>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="payload">The payload for the post.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns></returns>
        public virtual T Put<T>(
            Uri root,
            T payload,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            string json = PostData(odata, payload, "PUT", false, authParameters);
            T result = DeserializeJson<T>(json);

            return result;
        }

        /// <summary>
        /// Patch the data for the query.
        /// </summary>
        /// <typeparam name="T">The type of the return collection.</typeparam>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="payload">The json payload for the post.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns></returns>
        public virtual T Patch<T>(
            Uri root,
            string payload,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            string json = PostData(odata, payload, "PATCH", true, authParameters);
            T result = DeserializeJson<T>(json);

            return result;
        }

        /// <summary>
        /// Post the data for the query as a create ref.
        /// </summary>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="payload">The json payload for the post.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns></returns>
        public virtual string CreateRef(
            Uri root,
            string payload,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            string result = PostData(odata, payload, "POST", true, authParameters);

            return result;
        }

        /// <summary>
        /// Post the data for the query.
        /// </summary>
        /// <typeparam name="T">The type of the return collection.</typeparam>
        /// <param name="root">The uri root of the query.</param>
        /// <param name="parameters">The parameters to set in the query.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns></returns>
        public virtual T Delete<T>(
            Uri root,
            Dictionary<string, object> parameters,
            AuthParameters authParameters = null)
        {
            string odata = root.ToString() + this.Serialize(parameters);
            string json = PostData(odata, null, "DELETE", false, authParameters);
            T result = DeserializeJson<T>(json);

            return result;
        }

        /// <summary>
        /// Execute the request as is.
        /// </summary>
        /// <param name="request">The web request to execute.</param>
        /// <returns>The serialized response to the request.</returns>
        internal static string ExecuteRequest(HttpWebRequest request)
        {
            return GetWebResponse(request);
        }

        /// <summary>
        /// Extract an instance from an xml reader.
        /// </summary>
        /// <param name="reader">The xmlreader to introspect.</param>
        internal virtual void Deserialize(XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.None)
            {
                reader.MoveToContent();
            }

            this.Name = reader.GetAttribute("Name");
            this.Namespace = reader.GetAttribute("Namespace");
            string value = reader.GetAttribute("Format");
            if (string.IsNullOrEmpty(value) == false)
            {
                this.Format = (FormatType)Enum.Parse(typeof(FormatType), value);
            }

            string count = reader.GetAttribute("Count");
            if (string.IsNullOrEmpty(count) == false)
            {
                this.Count = bool.Parse(count);
            }

            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true)
                {
                    switch (reader.LocalName)
                    {
                        case "Filter":
                            this.Filter = new FilterType();
                            this.Filter.Deserialize(reader.ReadSubtree());
                            break;

                        case "Select":
                            this.Select = new List<PropertyType>();
                            this.DeserializeSelects(reader.ReadSubtree());
                            break;

                        case "OrderBy":
                            this.OrderBy = new List<OrderedPropertyType>();
                            this.DeserializeOrderBy(reader.ReadSubtree());
                            break;

                        case "ExpandList":
                            this.Expand = new List<ExpandType>();
                            this.DeserializeExpands(reader.ReadSubtree());
                            break;

                        case "EntitySet":
                            this.EntitySet = new EntitySetType();
                            this.EntitySet.Deserialize(reader.ReadSubtree());
                            break;

                        case "Operation":
                            this.Operation = new ODataOperationType();
                            this.Operation.Deserialize(reader.ReadSubtree());
                            break;

                        case "TopOrSkip":
                            this.TopOrSkip = new TopOrSkipType();
                            this.TopOrSkip.Deserialize(reader.ReadSubtree());
                            break;

                        case "DataFilter":
                            this.DataFilters = new List<EqualType>();
                            this.DeserializeDataFilters(reader.ReadSubtree());
                            break;

                        case "GroupBy":
                            this.GroupBy = new List<GroupByReferenceType>();
                            this.DeserializeGroupBys(reader.ReadSubtree());
                            break;

                        case "AggregateList":
                            this.Aggregate = new List<AggregatePropertyType>();
                            this.DeserializeAggregates(reader.ReadSubtree());
                            break;

                        case "CustomQueryOptions":
                            this.CustomQueryOptions = new List<QueryOptionType>();
                            this.DeserializeCustomOptions(reader.ReadSubtree());
                            break;

                        default:
                            return;
                    }
                }
            }
        }

        /// <summary>
        /// Serialize this instance to an odata uri.
        /// </summary>
        /// <param name="parameters">The populated parameters.</param>
        /// <returns>The serialized string.</returns>
        internal virtual string Serialize(Dictionary<string, object> parameters)
        {
            //if (this.Filter != null)
            //{
            //    this.Filter.Convert(parameters);
            //}

            StringBuilder query = new StringBuilder();
            char token = '?';

            string root = null;
            if (this.EntitySet != null)
            {
                root = this.EntitySet.Serialize();
            }
            else if (this.Operation != null)
            {
                // if the operations already includes a query string, 
                // we need to change the token to an ampersand.
                root = this.Operation.Serialize();
                string prefix = root.StartsWith("http://") == false ? "http://foo" : string.Empty;
                Uri uri = new Uri(prefix + root);
                if (string.IsNullOrEmpty(uri.Query) == false)
                {
                    token = '&';
                }
            }

            query.Append(root);

            List<Func<string>> actions = new List<Func<string>>();
            if (this.Filter != null)
            {
                actions.Add(new Func<string>(this.Filter.Serialize));
            }

            actions.Add(new Func<string>(this.SerializeExpands));
            actions.Add(new Func<string>(this.SerializeSelects));
            actions.Add(new Func<string>(this.SerializeOrderBy));
            actions.Add(new Func<string>(this.TopOrSkip.Serialize));
            actions.Add(new Func<string>(this.SerializeDataFilters));
            actions.Add(new Func<string>(this.SerializeGroupBys));
            actions.Add(new Func<string>(this.SerializeAggregates));
            actions.Add(new Func<string>(this.SerializeCustomOptions));

            foreach (Func<string> action in actions)
            {
                string result = action();
                if (string.IsNullOrEmpty(result) == false)
                {
                    query.Append(token);
                    query.Append(result);
                    token = '&';
                }
            }

            if (this.Count == true)
            {
                query.Append(token);
                query.Append("$count=true");
                token = '&';
            }

            string format = this.SerializeFormat();
            if (string.IsNullOrEmpty(format) == false)
            {
                query.Append(format);
                token = '&';
            }

            return query.ToString();
        }

        /// <summary>
        /// Post data to an http end point
        /// </summary>
        /// <param name="url">data url</param>
        /// <param name="data">object to be serialized into the body of the request</param>
        /// <param name="method">The request method to use.</param>
        /// <param name="jsonData">True if data parameter is already json.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns>The response from the post.</returns>
        private static string PostData(
            string url,
            object data,
            string method,
            bool jsonData,
            AuthParameters authParameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.UseDefaultCredentials = true;
            request.PreAuthenticate = true;
            request.SetAuthentication(authParameters);

            string strData = jsonData == true ? data.ToString() : JsonConvert.SerializeObject(data);
            byte[] byteArray = Encoding.UTF8.GetBytes(strData);
            request.ContentLength = byteArray.Length;
            request.ContentType = "application/json; charset=utf-8";
            if (method == "POST" || method == "PUT" || method == "PATCH" || method == "MERGE")
            {
                request.Headers["Prefer"] = "return=representation";
            }

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            return GetWebResponse(request);
        }

        /// <summary>
        /// Get data from the given url.
        /// </summary>
        /// <param name="odata">The url to query.</param>
        /// <param name="method">The request method to use.</param>
        /// <param name="authParameters">The authentication parameters</param>
        /// <returns>The serialized response.</returns>
        private static string GetData(
            string odata,
            string method,
            AuthParameters authParameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(odata);
            request.Method = method;
            request.UseDefaultCredentials = true;
            request.PreAuthenticate = true;
            request.SetAuthentication(authParameters);
            return GetWebResponse(request);
        }

        /// <summary>
        /// Get the web response.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <returns>The serialized response.</returns>
        private static string GetWebResponse(HttpWebRequest request)
        {
            try
            {
                string responseString;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader readStream = new StreamReader(responseStream))
                        {
                            responseString = readStream.ReadToEnd();
                        }
                    }
                }

                return responseString;
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    using (Stream responseStream = e.Response.GetResponseStream())
                    {
                        using (StreamReader readStream = new StreamReader(responseStream))
                        {
                            string message = readStream.ReadToEnd();
                            throw new WebException(message, e);
                        }
                    }
                }

                throw e;
            }
        }

        /// <summary>
        /// Deserialize json to an instance of an object.
        /// </summary>
        /// <typeparam name="T">The type argument.</typeparam>
        /// <param name="json">The json to deserialize.</param>
        /// <returns>The instance.</returns>
        private static T DeserializeJson<T>(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.Binder = new CustomBinder<T>();

            string data = json.Replace("@odata.type", "$type");
            T result = JsonConvert.DeserializeObject<T>(data, settings);
            return result;
        }

        /// <summary>
        /// Parse the count option.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="options">The options to inspect.</param>
        private static void ParseCount(ODataQueryType query, Dictionary<string, string> options)
        {
            if (options.ContainsKey("count") == true)
            {
                bool count;
                if (bool.TryParse(options["count"], out count) == true)
                {
                    query.Count = count;
                }
            }
        }

        /// <summary>
        /// Parse the format option.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="options">The options to inspect.</param>
        private static void ParseFormat(ODataQueryType query, Dictionary<string, string> options)
        {
            if (options.ContainsKey("format") == true)
            {
                FormatType format;
                if (Enum.TryParse(options["format"], out format) == true)
                {
                    query.Format = format;
                }
            }
        }

        /// <summary>
        /// Parse the select option.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="options">The options to inspect.</param>
        /// <param name="url">The called url.</param>
        private static void ParseSelect(ODataQueryType query, Dictionary<string, string> options, Uri url)
        {
            if (options.ContainsKey("select") == true)
            {
                query.Select = DataFilterParsingHelper.ParseSelect(url)
                    .Select(p => new PropertyType() { Name = p })
                    .ToList();
            }
        }

        /// <summary>
        /// Parse the top and skip options.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="options">The options to inspect.</param>
        private static void ParseTopAndSkip(ODataQueryType query, Dictionary<string, string> options)
        {
            if (options.ContainsKey("top") == true || options.ContainsKey("skip") == true)
            {
                uint top, skip;
                query.TopOrSkip = new TopOrSkipType();
                if (options.ContainsKey("top") == true && uint.TryParse(options["top"], out top) == true)
                {
                    query.TopOrSkip.Top = top;
                }

                if (options.ContainsKey("skip") == true && uint.TryParse(options["skip"], out skip) == true)
                {
                    query.TopOrSkip.Skip = skip;
                }
            }
        }

        /// <summary>
        /// Parse the expand option.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="options">The options to inspect.</param>
        /// <param name="url">The called url.</param>
        private static void ParseExpand(ODataQueryType query, Dictionary<string, string> options, Uri url)
        {
            if (options.ContainsKey("expand") == true)
            {
                QueryContext cxt = new QueryContext(url);
                string[] expand = cxt.ReadExpand().Split(',');
                foreach (string step in expand)
                {
                    ExpandType et = new ExpandType();
                    et.Property = new PropertyType() { Name = step };
                    cxt.Navigate(step);
                    ParseExpand(cxt, et);
                    cxt.ResetToParent();
                    query.Expand.Add(et);
                }
            }
        }

        /// <summary>
        /// Drill into the provided expand.
        /// </summary>
        /// <param name="cxt">The context provider.</param>
        /// <param name="et">The expand to introspect.</param>
        private static void ParseExpand(QueryContext cxt, ExpandType et)
        {
            string selects = cxt.ReadSelects();
            if (string.IsNullOrEmpty(selects) == false)
            {
                et.Select.AddRange(selects.Split(',').Select(p => new PropertyType() { Name = p }));
            }

            string filter = cxt.ReadFilter();
            if (string.IsNullOrEmpty(filter) == false)
            {
                string url = "http://test?$filter=" + filter;
                et.Filter = DataFilterParsingHelper.ParseFilter(new Uri(url));
            }

            string orderby = cxt.ReadOrderBys();
            if (string.IsNullOrEmpty(orderby) == false)
            {
                string url = "http://test?$orderby=" + orderby;
                et.OrderBy = DataFilterParsingHelper.ParseOrderBy(new Uri(url));
            }

            string skip = cxt.ReadSkip();
            string top = cxt.ReadTop();
            if (string.IsNullOrEmpty(skip) == false ||
                string.IsNullOrEmpty(top) == false)
            {
                et.TopOrSkip = new TopOrSkipType();
                uint skipNumber, topNumber;
                if (uint.TryParse(skip, out skipNumber) == true)
                {
                    et.TopOrSkip.Skip = skipNumber;
                }

                if (uint.TryParse(top, out topNumber) == true)
                {
                    et.TopOrSkip.Top = topNumber;
                }
            }

            string expands = cxt.ReadExpand();
            if (string.IsNullOrEmpty(expands) == false)
            {
                string[] steps = expands.Split(',');
                foreach (string step in steps)
                {
                    ExpandType child = new ExpandType();
                    child.Property = new PropertyType() { Name = step };
                    et.Expand.Add(child);
                    cxt.Navigate(step);
                    ParseExpand(cxt, child);
                    cxt.ResetToParent();
                }
            }
        }

        /// <summary>
        /// Parse the argument option.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="url">The called url.</param>
        private static void ParseAggregate(ODataQueryType query, Uri url)
        {
            List<AggregateColumnReference> cols = DataFilterParsingHelper.ParseColumns(url);
            foreach (AggregateColumnReference acr in cols)
            {
                AggregatePropertyType apt = new AggregatePropertyType();
                if (acr.AggregateType != AggregateType.Multiple)
                {
                    apt.Predicate = new WithType() { Predicate = acr.Predicatable, AggregateType = acr.AggregateType };
                }
                else
                {
                    apt.Predicate = acr.Predicatable;
                }

                apt.Alias = acr.Alias;
                if (acr.Froms != null)
                {
                    apt.Froms.AddRange(acr.Froms);
                }

                query.Aggregate.Add(apt);
            }
        }

        /// <summary>
        /// Parse the element path.
        /// </summary>
        /// <param name="query">The query to populate.</param>
        /// <param name="url">The called url.</param>
        /// <param name="endpoint">The service prefix of the url.</param>
        /// <param name="returnType">The return type.</param>
        private static void ParseElement(ODataQueryType query, Uri url, Uri endpoint, Type returnType)
        {
            FilterType args = DataFilterParsingHelper.ParseArguments(url, returnType);
            string address = url.AbsolutePath;
            if (endpoint != null)
            {
                address = address.Replace(endpoint.AbsolutePath, string.Empty);
            }

            // Key based queries will be stored as operations.
            string[] steps = address.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (steps.Last().Contains('(') == true)
            {
                if (returnType == null)
                {
                    throw new ArgumentException("ReturnType must be provided for this query.");
                }

                query.Operation = new ODataOperationType();
                FunctionType ft = new FunctionType() { Value = steps.Last() };
                query.Operation.Name = ft.Name;
                if (steps.Length - 2 >= 0)
                {
                    query.Operation.EntitySet = steps[steps.Length - 2];
                }

                if (args != null)
                {
                    ConditionType condition = args.Item as ConditionType;
                    if (condition != null)
                    {
                        foreach (ExpressionType item in condition.Items)
                        {
                            EqualType arg = item as EqualType;
                            if (arg != null)
                            {
                                query.Operation.Arguments.Add(arg);
                            }
                        }
                    }
                }
            }
            else
            {
                query.EntitySet = new EntitySetType();
                query.EntitySet.Name = steps.Last();
            }
        }

        /// <summary>
        /// Parse custom query options.
        /// </summary>
        /// <param name="query">The query to add the options to.</param>
        /// <param name="options">The full set of options.</param>
        private static void ParseCustom(ODataQueryType query, Dictionary<string, string> options)
        {
            foreach (string key in options.Keys)
            {
                if (IsStandardQueryOption(key) == false)
                {
                    QueryOptionType option = new QueryOptionType();
                    option.Option = key;
                    option.Value = options[key];
                    query.CustomQueryOptions.Add(option);
                }
            }
        }

        /// <summary>
        /// Determines whether the given query option is part of the standard set or not.
        /// </summary>
        /// <param name="key">The query option to test.</param>
        /// <returns>True if it is standard, otherwise false.</returns>
        private static bool IsStandardQueryOption(string key)
        {
            return string.Equals(key, "select", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "expand", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "filter", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "format", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "count", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "apply", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "top", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "skip", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "search", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "orderby", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "aggregate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "where", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "groupby", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Serializes the format string.
        /// </summary>
        /// <returns>The serialized string.</returns>
        private string SerializeFormat()
        {
            switch (this.Format)
            {
                case FormatType.JsonVerbose:
                    return "$format=application/json;odata.metadata=full";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Serialize the expands.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeExpands()
        {
            StringBuilder expands = new StringBuilder();
            string token = string.Empty;
            foreach (ExpandType expand in this.Expand)
            {
                expands.Append(token);
                expands.Append(expand.Serialize());
                token = ",";
            }

            if (this.Expand.Count > 0)
            {
                expands.Insert(0, "$expand=");
            }

            return expands.ToString();
        }

        /// <summary>
        /// Deserialize the expands.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeExpands(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Expand")
                {
                    ExpandType expand = new ExpandType();
                    expand.Deserialize(reader.ReadSubtree());
                    this.Expand.Add(expand);
                }
            }
        }

        /// <summary>
        /// Serialize the selects.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeSelects()
        {
            return this.SerializePropertyList("$select", this.Select);
        }

        /// <summary>
        /// Deserialize the selects.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeSelects(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Property")
                {
                    PropertyType p = new PropertyType();
                    p.Name = reader.GetAttribute("Name");
                    this.Select.Add(p);
                }
            }
        }

        /// <summary>
        /// Serialize the group bys.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeGroupBys()
        {
            StringBuilder output = new StringBuilder();
            string token = string.Empty;
            foreach (GroupByReferenceType item in this.GroupBy)
            {
                output.Append(token);
                if (item.GroupingType == GroupingType.Rollup)
                {
                    output.Append("rollup(");
                    token = string.Empty;
                }

                foreach (PropertyType property in item.Properties)
                {
                    output.Append(token);
                    output.Append(property.Name);
                    token = ",";
                }

                if (item.GroupingType == GroupingType.Rollup)
                {
                    output.Append(")");
                }
            }

            if (this.GroupBy.Count > 0)
            {
                output.Insert(0, "groupby=");
            }

            return output.ToString();
        }

        /// <summary>
        /// Deserialize the groupbys.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeGroupBys(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true && reader.LocalName == "GroupByReference")
                {
                    GroupByReferenceType p = new GroupByReferenceType();
                    p.Deserialize(reader.ReadSubtree());
                    this.GroupBy.Add(p);
                }
            }
        }

        /// <summary>
        /// Serialize the group bys.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeAggregates()
        {
            StringBuilder output = new StringBuilder();
            string token = string.Empty;
            foreach (AggregatePropertyType item in this.Aggregate)
            {
                IPredicatable predicatable = item.Predicate as IPredicatable;
                output.Append(token);
                string data = predicatable.Serialize();
                if (data[0] == '(' && data[data.Length - 1] == ')')
                {
                    data = data.Substring(1, data.Length - 2);
                }

                output.Append(data);
                output.Append(" as ");
                output.Append(item.Alias);
                foreach (FromType from in item.Froms)
                {
                    output.Append(" from ");
                    output.Append(from.Name);
                    output.Append(" with ");
                    output.Append(from.AggregateType.ToString().ToLower());
                }

                token = ",";
            }

            if (this.Aggregate.Count > 0)
            {
                output.Insert(0, "aggregate=");
            }

            return output.ToString();
        }

        /// <summary>
        /// Deserialize the aggregates.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeAggregates(XmlReader reader)
        {
            AggregatePropertyType cxt = null;
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true && reader.LocalName == "Aggregate")
                {
                    AggregatePropertyType apt = new AggregatePropertyType();
                    apt.Alias = reader.GetAttribute("Alias");
                    reader.Read();
                    IPredicatable predicatable = PredicateType.CreatePredicatable(reader.LocalName);
                    predicatable.Deserialize(reader.ReadSubtree());
                    apt.Predicate = predicatable;
                    this.Aggregate.Add(apt);
                    cxt = apt;
                }
                else if (reader.IsStartElement() == true && reader.LocalName == "From")
                {
                    string name = reader.GetAttribute("Name");
                    string type = reader.GetAttribute("AggregateType");
                    FromType ft = new FromType()
                    {
                        Name = name,
                        AggregateType = (AggregateType)Enum.Parse(typeof(AggregateType), type)
                    };
                    cxt.Froms.Add(ft);
                }
            }
        }

        /// <summary>
        /// Serialize the order by.
        /// </summary>
        /// <returns>The resulting string.</returns>
        private string SerializeOrderBy()
        {
            StringBuilder order = new StringBuilder();
            string token = string.Empty;
            foreach (OrderedPropertyType property in this.OrderBy)
            {
                order.Append(token);
                order.Append(property.Serialize());
                token = ",";
            }

            if (this.OrderBy.Count > 0)
            {
                order.Insert(0, "$orderby=");
            }

            return order.ToString();
        }

        /// <summary>
        /// Deserialize the order by.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeOrderBy(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true &&
                    reader.LocalName == "Property")
                {
                    OrderedPropertyType p = new OrderedPropertyType();
                    p.Name = reader.GetAttribute("Name");
                    p.Ascending = XmlConvert.ToBoolean(reader.GetAttribute("Ascending"));
                    this.OrderBy.Add(p);
                }
            }
        }

        /// <summary>
        /// Serialize the data filters.
        /// </summary>
        /// <returns>The serialized string.</returns>
        private string SerializeDataFilters()
        {
            StringBuilder builder = new StringBuilder();
            if (this.Where != null)
            {
                builder.Append("where=");
                string where = this.Where.Serialize();
                string[] parts = where.Split('=');
                builder.Append(parts[1]);
            }
            else
            {
                string separator = string.Empty;
                foreach (EqualType eq in this.DataFilters)
                {
                    string equation = eq.Serialize();
                    builder.Append(separator);
                    builder.Append(equation);
                    separator = " and ";
                }

                if (this.DataFilters.Count > 0)
                {
                    builder.Insert(0, "where=");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Deserialize the data filters.
        /// </summary>
        /// <param name="reader">The reader to consume.</param>
        private void DeserializeDataFilters(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true)
                {
                    if (reader.LocalName == "Filter")
                    {
                        EqualType eq = new EqualType();
                        eq.Deserialize(reader.ReadSubtree());
                        this.DataFilters.Add(eq);
                    }
                    else if (reader.LocalName == "Where")
                    {
                        FilterType where = new FilterType();
                        where.Deserialize(reader.ReadSubtree());
                    }
                }
            }
        }

        /// <summary>
        /// Serialize the custom options.
        /// </summary>
        /// <returns>The serialized custom options.</returns>
        private string SerializeCustomOptions()
        {
            StringBuilder output = new StringBuilder();
            string token = string.Empty;
            foreach (QueryOptionType option in this.CustomQueryOptions)
            {
                output.Append(token);
                output.Append(option.Option);
                output.Append("=");
                output.Append(option.Value);
                token = "&";
            }

            return output.ToString();
        }

        /// <summary>
        /// Deserialize the custom options.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        private void DeserializeCustomOptions(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == true && reader.LocalName == "CustomQueryOption")
                {
                    QueryOptionType option = new QueryOptionType();
                    option.Option = reader.GetAttribute("Option");
                    reader.Read();
                    option.Value = reader.Value;
                    this.CustomQueryOptions.Add(option);
                }
            }
        }

        /// <summary>
        /// Serialize the list of properties.
        /// </summary>
        /// <param name="option">The option for serialization.</param>
        /// <returns>The resulting string.</returns>
        private string SerializePropertyList(string option, List<PropertyType> list)
        {
            StringBuilder output = new StringBuilder();
            string token = string.Empty;
            foreach (PropertyType property in list)
            {
                output.Append(token);
                output.Append(property.Name);
                token = ",";
            }

            if (list.Count > 0)
            {
                output.Insert(0, string.Concat(option, "="));
            }

            return output.ToString();
        }

        /// <summary>
        /// Helper class to resolve bindings during json deserialization.
        /// </summary>
        /// <typeparam name="T">The root type of the deserialization operation.</typeparam>
        private class CustomBinder<T> : SerializationBinder
        {
            /// <summary>
            /// Binds the name of the assembly and type to the actual type.
            /// </summary>
            /// <param name="assemblyName">The full assembly name.</param>
            /// <param name="typeName">The ful type name.</param>
            /// <returns>The corresponding type.</returns>
            public override Type BindToType(string assemblyName, string typeName)
            {
                Assembly assembly = null;
                if (string.IsNullOrEmpty(assemblyName) == true)
                {
                    assembly = typeof(T).Assembly;
                    if (typeName[0] == '#')
                    {
                        typeName = typeName.Substring(1);
                    }
                }
                else
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    assembly = assemblies.Single(p => p.FullName.Contains(assemblyName));
                }

                Type type = assembly.GetType(typeName);
                return type;
            }
        }
    }
}
