// -----------------------------------------------------------------------
// <copyright file="BatchResponseReader.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Config = Configuration;

    /// <summary>
    /// Parse json and expose as IDataReader
    /// </summary>
    public sealed class BatchResponseReader : IDataReader
    {
        /// <summary>
        /// The parsed payload.
        /// </summary>
        private JArray array;

        /// <summary>
        /// The metadata for the payload.
        /// </summary>
        private Config.Feed feed;

        /// <summary>
        /// The current member of the payload.
        /// </summary>
        private JObject current;

        /// <summary>
        /// The current index of the reader.
        /// </summary>
        private int index;

        /// <summary>
        /// The depth of the reader.
        /// </summary>
        private int depth;

        /// <summary>
        /// Map of ordinals to names.
        /// </summary>
        private Dictionary<int, string> ordinalMap = new Dictionary<int, string>();

        /// <summary>
        /// Map of names to ordinals.
        /// </summary>
        private Dictionary<string, int> nameMap = new Dictionary<string, int>();

        /// <summary>
        /// The metadata map.
        /// </summary>
        private Dictionary<int, Config.NamedObject> properties = new Dictionary<int, Config.NamedObject>();

        /// <summary>
        /// Initializes a new instance of teh BatchResponseReader class.
        /// </summary>
        /// <param name="response">The response to read.</param>
        /// <param name="feed">The metadata for the payload.</param>
        public BatchResponseReader(BatchResponse response, Config.Feed feed)
            : this(response.Payload, feed, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of teh BatchResponseReader class.
        /// </summary>
        /// <param name="response">The response to read.</param>
        /// <param name="feed">The metadata for the payload.</param>
        /// <param name="depth">The depth of the reader.</param>
        private BatchResponseReader(string json, Config.Feed feed, int depth)
        {
            JToken token = JToken.Parse(json);
            if (token is JArray)
            {
                this.array = token as JArray;
            }
            else
            {
                JToken value;
                JObject obj = token as JObject;
                if (obj.TryGetValue("value", out value) == true)
                {
                    this.array = value as JArray;
                }
                else
                {
                    this.array = new JArray();
                    this.array.Add(obj);
                }
            }

            this.feed = feed;
            this.depth = depth;

            this.InitializeMap();
        }

        /// <summary>
        /// Indexer into the current item by name.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The value for the given name.</returns>
        public object this[string name]
        {
            get
            {
                return this.GetValue(this.GetPublicOrdinal(name));
            }
        }

        /// <summary>
        /// Indexer into the current item by ordinal.
        /// </summary>
        /// <param name="i">The ordinal.</param>
        /// <returns>The value for the given ordinal.</returns>
        public object this[int i]
        {
            get
            {
                return this.GetValue(i);
            }
        }

        /// <summary>
        /// Gets the depth of the reader.
        /// </summary>
        public int Depth
        {
            get
            {
                return this.depth;
            }
        }

        /// <summary>
        /// Gets the field count for the current item.
        /// </summary>
        public int FieldCount
        {
            get
            {
                return this.properties.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the reader is closed.
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return this.array.Count <= this.index;
            }
        }

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        public int RecordsAffected
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Gets a count of items in the array.
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.array.Count;
            }
        }

        /// <summary>
        /// Closed the reader.
        /// </summary>
        public void Close()
        {
            this.index = this.array.Count + 1;
            this.current = null;
        }

        /// <summary>
        /// Disposes the reader.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Return the boolen at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The boolean value.</returns>
        public bool GetBoolean(int i)
        {
            return (bool)this.GetValue(i);
        }

        /// <summary>
        /// Return the boolen at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The boolean value.</returns>
        public byte GetByte(int i)
        {
            return (byte)this.GetValue(i);
        }

        /// <summary>
        /// Populate an array of bytes.
        /// </summary>
        /// <param name="i">The ordinal position.</param>
        /// <param name="fieldOffset">The offset.</param>
        /// <param name="buffer">The buffer to populate.</param>
        /// <param name="bufferoffset">The buffer offset.</param>
        /// <param name="length">The length of data to write.</param>
        /// <returns>Total number of bytes in source array.</returns>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            byte[] bytes = (byte[])this.GetValue(i);
            Array.Copy(bytes, fieldOffset, buffer, bufferoffset, length);

            return bytes.LongLength;
        }

        /// <summary>
        /// Return the char at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The char value.</returns>
        public char GetChar(int i)
        {
            return (char)this.GetValue(i);
        }

        /// <summary>
        /// Populate an array of characters.
        /// </summary>
        /// <param name="i">The ordinal position.</param>
        /// <param name="fieldOffset">The offset.</param>
        /// <param name="buffer">The buffer to populate.</param>
        /// <param name="bufferoffset">The buffer offset.</param>
        /// <param name="length">The length of data to write.</param>
        /// <returns>Total number of characters in source array.</returns>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            char[] chars = (char[])this.GetValue(i);
            Array.Copy(chars, fieldoffset, buffer, bufferoffset, length);

            return chars.LongLength;
        }

        /// <summary>
        /// Returns a data reader for the element at the ordinal position.
        /// </summary>
        /// <param name="i">The position.</param>
        /// <returns>The corresponding data reader.</returns>
        public IDataReader GetData(int i)
        {
            return (IDataReader)this.GetValue(i);
        }

        /// <summary>
        /// Returns the type of the data in the given field ordinal.
        /// </summary>
        /// <param name="i">The field ordinal.</param>
        /// <returns>The type of the data.</returns>
        public string GetDataTypeName(int i)
        {
            return this.GetFieldType(i).Name;
        }

        /// <summary>
        /// Return the datetime at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The datetime value.</returns>
        public DateTime GetDateTime(int i)
        {
            return (DateTime)this.GetValue(i);
        }

        /// <summary>
        /// Return the decimal at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The decimal value.</returns>
        public decimal GetDecimal(int i)
        {
            return (decimal)this.GetValue(i);
        }

        /// <summary>
        /// Return the double at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The double value.</returns>
        public double GetDouble(int i)
        {
            return (double)this.GetValue(i);
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
        /// Return the float at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The float value.</returns>
        public float GetFloat(int i)
        {
            return (float)this.GetValue(i);
        }

        /// <summary>
        /// Return the guid at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The guid value.</returns>
        public Guid GetGuid(int i)
        {
            return (Guid)this.GetValue(i);
        }

        /// <summary>
        /// Return the short at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The short value.</returns>
        public short GetInt16(int i)
        {
            return (short)this.GetValue(i);
        }

        /// <summary>
        /// Return the int at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The int value.</returns>
        public int GetInt32(int i)
        {
            return (int)this.GetValue(i);
        }

        /// <summary>
        /// Return the long at the given position.
        /// </summary>
        /// <param name="i">The long to inspect.</param>
        /// <returns>The boolean value.</returns>
        public long GetInt64(int i)
        {
            return (long)this.GetValue(i);
        }

        /// <summary>
        /// Returns the name for a given ordinal.
        /// </summary>
        /// <param name="i">The ordinal to search for.</param>
        /// <returns>The corresponding name.</returns>
        public string GetName(int i)
        {
            int ordinal = this.GetPrivateOrdinal(i);
            if (this.ordinalMap.ContainsKey(ordinal) == true)
            {
                return this.ordinalMap[ordinal];
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
            if (this.nameMap.ContainsKey(name) == true)
            {
                return this.GetPublicOrdinal(name);
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

            if (this.array.Count > 0)
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
        /// Return the string at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The string value.</returns>
        public string GetString(int i)
        {
            return this.GetValue(i).ToString();
        }

        /// <summary>
        /// Return the object at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The object value.</returns>
        public object GetValue(int i)
        {
            JToken token = this.current[this.properties[i].Name];
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return (bool)token;

                case JTokenType.Bytes:
                    return (byte[])token;

                case JTokenType.Float:
                    return (float)token;

                case JTokenType.Guid:
                    return (Guid)token;

                case JTokenType.Integer:
                    return (int)token;

                case JTokenType.Date:
                    return (DateTime)token;

                case JTokenType.String:
                    return token.ToString();

                case JTokenType.Null:
                    return null;

                default:
                    Config.Feed subFeed = null;
                    Config.NavigationProperty np = this.properties[i] as Config.NavigationProperty;
                    if (np != null)
                    {
                        subFeed = Config.ConfigurationCache.Instance.GetWellKnownInstance<Config.EntityType>(np.TargetType);
                        BatchResponseReader reader = new BatchResponseReader(token.ToString(), subFeed, this.depth + 1);
                        return reader;
                    }

                    return token;
            }
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
        /// Indicates whether the indexed value is null.
        /// </summary>
        /// <param name="i">The ordinal position to inspect.</param>
        /// <returns>True if null, otherwise false.</returns>
        public bool IsDBNull(int i)
        {
            return this.GetValue(i) == null;
        }

        /// <summary>
        /// Advances to the next result.
        /// </summary>
        /// <returns>True if there is a next result, otherwise false.</returns>
        public bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// Read the next item in the reader.
        /// </summary>
        /// <returns>True if reader still readable, otherwise false.</returns>
        public bool Read()
        {
            if (array.Count > this.index)
            {
                this.current = this.array[this.index] as JObject;
                this.index++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset the reader for reuse.
        /// </summary>
        public void Reset()
        {
            this.index = 0;
        }

        /// <summary>
        /// Initializes the property map.
        /// </summary>
        private void InitializeMap()
        {
            if (this.array.Count > 0)
            {
                int ordinal = 0;
                JObject value = this.array[0] as JObject;
                foreach (KeyValuePair<string, JToken> item in value)
                {
                    this.nameMap[item.Key] = ordinal;
                    this.ordinalMap[ordinal++] = item.Key;
                }

                if (this.feed != null)
                {
                    for (int i = 0; i < this.feed.Properties.Count; i++)
                    {
                        this.properties[i] = this.feed.Properties[i];
                    }

                    Config.EntityType et = this.feed as Config.EntityType;
                    if (et != null)
                    {
                        for (int i = 0; i < et.NavigationProperties.Count; i++)
                        {
                            this.properties[i + this.feed.Properties.Count] = et.NavigationProperties[i];
                        }
                    }
                }
                else
                {
                    foreach (int key in this.ordinalMap.Keys)
                    {
                        this.properties[key] = new Config.Property() { Name = this.ordinalMap[key] };
                    }
                }
            }
        }

        /// <summary>
        /// Retruns the public ordinal for the reader.
        /// </summary>
        /// <param name="name">The name to use to locate the public ordinal.</param>
        /// <returns>The public ordinal.</returns>
        private int GetPublicOrdinal(string name)
        {
            if (this.nameMap.ContainsKey(name) == true)
            {
                KeyValuePair<int, Config.NamedObject> pair = this.properties
                    .Where(p => p.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .Single();

                return pair.Key;
            }

            return -1;
        }

        /// <summary>
        /// Returns the private ordinal for the reader.
        /// </summary>
        /// <param name="i">The public ordinal to look up.</param>
        /// <returns>The private ordinal.</returns>
        private int GetPrivateOrdinal(int i)
        {
            if (this.properties.ContainsKey(i) == true)
            {
                string name = this.properties[i].Name;
                return this.nameMap[name];
            }

            return -1;
        }
    }
}
