// -----------------------------------------------------------------------
// <copyright file="WriteSet.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Config = Configuration;

    /// <summary>
    /// The data set to write.
    /// </summary>
    internal sealed class WriteSet
    {
        /// <summary>
        /// Name of the property holding the reader instance for each data.
        /// </summary>
        private const string InstanceName = "$instance";

        /// <summary>
        /// The columns for the write type.
        /// </summary>
        private List<QueryColumn> columns;

        /// <summary>
        /// The property map.
        /// </summary>
        private Dictionary<int, string> properties = new Dictionary<int, string>();

        /// <summary>
        /// The collection of data for the current set.
        /// </summary>
        private List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();

        /// <summary>
        /// Map of many to many tables.
        /// </summary>
        private Dictionary<string, WriteSet> manyToManySets = new Dictionary<string, WriteSet>();

        /// <summary>
        /// Map of supplemental tables.
        /// </summary>
        private Dictionary<string, WriterReader> supplementalSets = new Dictionary<string, WriterReader>();

        /// <summary>
        /// The left columns, if applicable.
        /// </summary>
        private List<QueryColumn> leftColumns;

        /// <summary>
        /// The right columns, if applicable.
        /// </summary>
        private List<QueryColumn> rightColumns;

        /// <summary>
        /// The left key names, if applicable.
        /// </summary>
        private List<string> left;

        /// <summary>
        /// The right key names, if applicable.
        /// </summary>
        private List<string> right;

        /// <summary>
        /// The current index of the reader.
        /// </summary>
        int index = -1;

        /// <summary>
        /// Initializes a new instance of the WriteSet class.
        /// </summary>
        /// <param name="writeType">The type of the write set entities.</param>
        /// <param name="columns">The columns associated with the type.</param>
        public WriteSet(Type writeType, List<QueryColumn> columns)
        {
            this.columns = columns;
            this.WriteType = writeType;
            this.QueryTable = columns.First().Source as QueryTable;
        }

        /// <summary>
        /// Gets or sets a value indicating whether write set is part of a table per type hierarchy.
        /// </summary>
        public bool TablePerType
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the table data.
        /// </summary>
        public QueryTable QueryTable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of columns.
        /// </summary>
        public ICollection<QueryColumn> Columns
        {
            get
            {
                return this.columns
                    .Where(p => TypeCache.IsNavigationalType(p.ElementType) == false)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the data for the primary write set.
        /// </summary>
        public IDictionary<string, object> Data
        {
            get
            {
                ReadOnlyDictionary<string, object> readOnly = new ReadOnlyDictionary<string, object>(this.data[this.index]);
                return readOnly;
            }
        }

        /// <summary>
        /// Gets the FieldCount.
        /// </summary>
        public int FieldCount
        {
            get
            {
                return this.properties.Count;
            }
        }

        /// <summary>
        /// Gets the RowCount.
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.data.Count;
            }
        }

        /// <summary>
        /// Gets the type of the write set.
        /// </summary>
        public Type WriteType
        {
            get;
            private set;
        }

        /// <summary>
        /// Preview the next instance in the set.
        /// </summary>
        /// <returns>The peeked instance.</returns>
        public object PeekNextInstance()
        {
            if (this.index < this.data.Count - 1)
            {
                return this.data[this.index + 1][InstanceName];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get all supplemented column names.
        /// </summary>
        public IEnumerable<string> LocateSupplementColumnNames()
        {
            return this.supplementalSets.Keys;
        }

        /// <summary>
        /// Convert the write set structure into a configured feed.
        /// </summary>
        /// <returns>The configured feed.</returns>
        public Config.Feed Convert()
        {
            Config.Feed feed = new Config.EntityType();
            feed.Name = this.QueryTable.Name;
            feed.Namespace = this.QueryTable.Schema;
            foreach (QueryColumn column in this.Columns)
            {
                Config.Property property = new Config.Property();
                property.Computed = column.Computed == DatabaseGeneratedOption.Computed;
                property.AutoIncrement = column.Computed == DatabaseGeneratedOption.Identity;
                property.Name = column.Name;
                property.Nullable = column.Nullable;
                property.Size = column.Size;
                property.Type = column.ConcurrencyCheck ? Config.DataType.rowversion : Convert(column.ElementType);
                feed.Properties.Add(property);
                if (column.IsKeyColumn == true)
                {
                    feed.Keys.Add(new Config.PropertyRef() { Name = property.Name });
                }
            }

            return feed;
        }

        /// <summary>
        /// Update the value on the current write set.
        /// </summary>
        /// <param name="property">The property to update.</param>
        /// <param name="value">The value to update it to.</param>
        public void UpdateValue(string property, object value)
        {
            if (this.data[this.index].ContainsKey(property) == false)
            {
                string message = "The property {0} has not been defined on the current write set.";
                throw new IndexOutOfRangeException(string.Format(message, property));
            }

            this.data[this.index][property] = value;
        }

        /// <summary>
        /// Add an instance to the write set.
        /// </summary>
        /// <param name="jobject">The token to add.</param>
        /// <param name="instance">The instance.</param>
        public void Add(JObject jobject, object instance)
        {
            Dictionary<string, object> propertyBag = new Dictionary<string, object>();
            propertyBag[InstanceName] = instance;
            int counter = 0;
            foreach (QueryColumn column in this.columns.OrderBy(p => TypeCache.IsNavigationalType(p.ElementType)))
            {
                if (TypeCache.IsNavigationalType(column.ElementType) == false)
                {
                    JToken value;
                    if (jobject.TryGetValue(column.Alias, out value) == false && column.IsKeyColumn == false)
                    {
                        continue;
                    }
                    else if (this.WriteType != null && this.WriteType.GetProperties().Any(p => p.Name == column.Alias) == true)
                    {
                        this.properties[counter++] = column.Alias;
                        if (value != null && string.IsNullOrEmpty(value.ToString()) == false)
                        {
                            propertyBag[column.Alias] = value.ToObject(column.ElementType);
                        }
                        else
                        {
                            propertyBag[column.Alias] = column.DefaultValue;
                        }
                    }
                }
                else if (column.ElementType == typeof(IDictionary<string, object>) == true ||
                    typeof(IDictionary<string, object>).IsAssignableFrom(column.ElementType) == true)
                {
                    continue;
                }
                else
                {
                    QueryTable intermediateTable;
                    Dictionary<string, string> cols = TypeCache.GetOverride(
                        TypeCache.NormalizeType(this.WriteType) ?? typeof(object),
                        column.Alias,
                        out intermediateTable);

                    if (intermediateTable != null)
                    {
                        JArray array = jobject[column.Alias] as JArray;
                        if (array != null && array.HasValues == true)
                        {
                            WriteSet mtm = GetOrAddManyToManySet(column, cols, intermediateTable);
                            mtm.AddManyToMany(column, jobject);
                        }
                    }
                    else if (TypeCache.IsNavigationalType(column.ElementType) == true)
                    {
                        JToken obj;
                        bool valueSetByCaller = jobject.TryGetValue(column.Alias, out obj);
                        bool hasValue = false;
                        if (obj != null && obj.HasValues == true)
                        {
                            hasValue = true;
                        }

                        foreach (string key in cols.Keys)
                        {
                            Type propertyType = column.ElementType.GetProperty(cols[key])?.PropertyType;
                            if (propertyType != null && hasValue == true)
                            {
                                this.properties[counter++] = key;
                                propertyBag[key] = obj[cols[key]].ToObject(propertyType);
                            }
                            else if (propertyType != null && hasValue == false && valueSetByCaller == true && propertyBag.ContainsKey(key) == false)
                            {
                                propertyBag[key] = null;
                            }
                        }

                        if (hasValue == true)
                        {
                            this.UpdateNavigation(column, obj, cols, propertyBag);
                        }
                    }
                }
            }

            this.data.Add(propertyBag);
        }

        /// <summary>
        /// Returns the type of the data in the given field ordinal.
        /// </summary>
        /// <param name="i">The field ordinal.</param>
        /// <returns>The type of the data.</returns>
        public Type GetFieldType(int i)
        {
            if (this.GetValue(i) == null)
            {
                return typeof(DBNull);
            }

            return this.GetValue(i).GetType();
        }

        /// <summary>
        /// Returns the name for a given ordinal.
        /// </summary>
        /// <param name="i">The ordinal to search for.</param>
        /// <returns>The corresponding name.</returns>
        public string GetName(int i)
        {
            if (this.properties.ContainsKey(i) == true)
            {
                return this.properties[i];
            }

            return null;
        }

        /// <summary>
        /// Returns the ordinal for a given name.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The corresponding ordinal.</returns>
        public int GetOrdinal(string name)
        {
            foreach (int key in this.properties.Keys)
            {
                if (this.properties[key] == name)
                {
                    return key;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the schema for the current reader.
        /// </summary>
        /// <returns>The schema table.</returns>
        public DataTable GetSchemaTable()
        {
            DataTable table = new DataTable();
            table.Locale = CultureInfo.CurrentCulture;

            if (this.columns.Count > 0)
            {
                for (int i = 0; i < this.properties.Count; i++)
                {
                    string name = this.GetName(i);
                    Type type = this.GetFieldType(i);
                    DataColumn c = new DataColumn(name, type);
                    table.Columns.Add(c);
                }
            }

            return table;
        }

        /// <summary>
        /// Return the object at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The object value.</returns>
        public object GetValue(int i)
        {
            string name = this.GetName(i);
            return this.data[this.index][name];
        }

        /// <summary>
        /// Populates an array with values from the current item.
        /// </summary>
        /// <param name="values">The array to populate.</param>
        /// <returns>The number of values added to the array.</returns>
        public int GetValues(object[] values)
        {
            for (int i = 0; i < this.properties.Count; i++)
            {
                values[i] = this.GetValue(i);
            }

            return this.properties.Count;
        }

        /// <summary>
        /// Read the next item in the reader.
        /// </summary>
        /// <returns>True if reader still readable, otherwise false.</returns>
        public bool Read()
        {
            if (this.index < this.data.Count - 1)
            {
                this.index++;
                return true;
            }
            else
            {
                this.index = this.data.Count;
                return false;
            }
        }

        /// <summary>
        /// Reset the reader for reuse.
        /// </summary>
        public void Reset()
        {
            this.index = -1;
        }

        /// <summary>
        /// Gets the list of many to manies.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WriteSet> LoadManyToManies()
        {
            return this.manyToManySets.Values;
        }

        /// <summary>
        /// Get the list of supplements for this set.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WriterReader> LoadSupplements()
        {
            return this.supplementalSets.Values;
        }

        /// <summary>
        /// Convert the system type into a configuration data type.
        /// </summary>
        /// <param name="elementType">The system type to inspect.</param>
        /// <returns>The correlative configuration data type.</returns>
        private static Config.DataType Convert(Type elementType)
        {
            switch (elementType.Name)
            {
                case "String":
                    return Config.DataType.@string;
                case "Int32":
                    return Config.DataType.@int;
                case "DateTimeOffset":
                    return Config.DataType.datetimeoffset;
                case "DateTime":
                    return Config.DataType.datetime;
                case "Int64":
                    return Config.DataType.@long;
                case "Boolean":
                    return Config.DataType.@bool;
                case "Byte":
                    return Config.DataType.@byte;
                case "Char":
                    return Config.DataType.@char;
                case "Double":
                    return Config.DataType.@double;
                case "Guid":
                    return Config.DataType.guid;
                case "Byte[]":
                    return Config.DataType.varbinary;
                case "Decimal":
                    return Config.DataType.@decimal;
                case "Single":
                    return Config.DataType.@float;
                case "Int16":
                    return Config.DataType.@short;
                default:
                    if (elementType.IsEnum == true)
                    {
                        return Convert(Enum.GetUnderlyingType(elementType));
                    }

                    throw new NotSupportedException(elementType.Name);
            }
        }

        /// <summary>
        /// Create a one supplemental set.
        /// </summary>
        /// <param name="column">The column indicating the many to many.</param>
        /// <returns>The write set for the many to many.</returns>
        private WriterReader GetOrAddSupplementalSet(QueryColumn column)
        {
            if (this.supplementalSets.ContainsKey(column.Alias) == false)
            {
                WriterReader reader = new WriterReader(TypeCache.NormalizeType(column.ElementType));
                reader.SupplementedColumn = column;
                this.supplementalSets[column.Alias] = reader;
            }

            return this.supplementalSets[column.Alias];
        }

        /// <summary>
        /// Create a many to many set.
        /// </summary>
        /// <param name="column">The column indicating the many to many.</param>
        /// <param name="cols">The column list of the many to many.</param>
        /// <param name="table">The intermediate table.</param>
        /// <returns>The write set for the many to many.</returns>
        private WriteSet GetOrAddManyToManySet(QueryColumn column, Dictionary<string, string> cols, QueryTable table)
        {
            if (this.manyToManySets.ContainsKey(column.Alias) == false)
            {
                Type rightType = column.ElementType.GetGenericArguments()[0];
                List<QueryColumn> columns = new List<QueryColumn>();
                foreach (string key in cols.Keys)
                {
                    QueryColumn left = new QueryColumn()
                    {
                        Name = key,
                        Alias = key,
                        Source = table,
                        ElementType = typeof(long),
                        IsKeyColumn = true
                    };
                    QueryColumn right = new QueryColumn()
                    {
                        Name = cols[key],
                        Alias = cols[key],
                        Source = table,
                        ElementType = typeof(long),
                        IsKeyColumn = true
                    };
                    columns.Add(left);
                    columns.Add(right);
                }

                WriteSet set = new WriteSet(null, columns);
                set.leftColumns = TypeCache.CreateColumns(new QueryTable(), column.DeclaringType);
                set.rightColumns = TypeCache.CreateColumns(new QueryTable(), rightType);
                set.left = TypeCache.GetKeys(column.DeclaringType);
                set.right = TypeCache.GetKeys(rightType);

                this.manyToManySets[column.Alias] = set;
            }

            return this.manyToManySets[column.Alias];
        }

        /// <summary>
        /// Adds data to the write set for many to many data.
        /// </summary>
        /// <param name="column">The column indicating the many to many relationship.</param>
        /// <param name="jobject">The current row in the loading side of the data.</param>
        private void AddManyToMany(QueryColumn column, JObject jobject)
        {
            JArray array = jobject[column.Alias] as JArray;
            if (array != null && array.HasValues == true)
            {
                int counter = 0;
                foreach (QueryColumn c in this.columns)
                {
                    this.properties[counter++] = c.Alias;
                }

                int keyIndex = 0;
                Dictionary<string, object> keyBag = new Dictionary<string, object>();
                foreach (string leftKey in this.left)
                {
                    QueryColumn leftCol = this.leftColumns.Single(p => p.Alias == leftKey);
                    object leftValue = jobject[leftKey].ToObject(leftCol.ElementType);
                    string name = this.properties[keyIndex++];
                    keyBag[name] = leftValue;
                }

                foreach (JObject item in array)
                {
                    Dictionary<string, object> propertyBag = new Dictionary<string, object>();
                    foreach (string key in keyBag.Keys)
                    {
                        propertyBag[key] = keyBag[key];
                    }

                    int rightKeyIndex = keyIndex;
                    foreach (string rightKey in this.right)
                    {
                        QueryColumn rightCol = this.rightColumns.Single(p => p.Alias == rightKey);
                        object rightValue = item[rightKey].ToObject(rightCol.ElementType);
                        string name = this.properties[rightKeyIndex++];
                        propertyBag[name] = rightValue;
                    }

                    this.data.Add(propertyBag);
                }
            }
        }

        /// <summary>
        /// Update the navigation property.
        /// </summary>
        /// <param name="column">The column containing the navigation instance.</param>
        /// <param name="token">The value contained in the column.</param>
        /// <param name="keys">The key names associating the types.</param>
        /// <param name="propertyBag">The current collected data.</param>
        private void UpdateNavigation(
            QueryColumn column, 
            JToken token, 
            Dictionary<string, string> keys, 
            Dictionary<string, object> propertyBag)
        {
            bool keysOnly = false;
            JArray array = token as JArray;
            if (array == null)
            {
                IEnumerable<JValue> values = token.Values().OfType<JValue>().Where(p => p.Value != null && IsDefault(p.Value) == false);
                IEnumerable<string> keyNames = TypeCache.GetKeys(column.ElementType);
                IEnumerable<string> names = values.Where(p => p.Parent is JProperty).Select(p => ((JProperty)p.Parent).Name);
                if (names.Except(keyNames).Count() == 0)
                {
                    keysOnly = true;
                }
            }

            if (keysOnly == false)
            {
                propertyBag[column.Alias] = HandleNavigation(column, token);
            }
        }

        /// <summary>
        /// Handle the navigation property.
        /// </summary>
        /// <param name="column">The column containing the navigation instance.</param>
        /// <param name="token">The value contained in the column.</param>
        /// <returns>The navigation data.</returns>
        private object HandleNavigation(QueryColumn column, JToken token)
        {
            WriterReader wr = GetOrAddSupplementalSet(column);
            JObject ex = token as JObject;
            JArray ar = token as JArray;
            if (ex != null)
            {
                wr.MustRunFirst = true;
                return wr.Add(ex);
            }
            else if (ar != null)
            {
                wr.MustRunFirst = false;
                List<object> data = new List<object>();
                foreach (JObject item in ar)
                {
                    data.Add(wr.Add(item));
                }

                return data;
            }

            throw new InvalidOperationException("Unknown token provided.");
        }

        /// <summary>
        /// Check whether the given value is a default value of the type.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if it is the default value, otherwise false.</returns>
        private bool IsDefault(object value)
        {
            switch (value.GetType().Name)
            {
                case "Int32":
                    return (int)value == default(int);
                case "Int64":
                    return (long)value == default(long);
                case "Guid":
                    return (Guid)value == default(Guid);
                case "DateTimeOffset":
                    return (DateTimeOffset)value == default(DateTimeOffset);
                case "Int16":
                    return (short)value == default(short);
                case "Decimal":
                    return (decimal)value == default(decimal);
                case "Single":
                    return (float)value == default(float);
                case "Double":
                    return (double)value == default(double);
                default:
                    return false;
            }
        }
    }
}
