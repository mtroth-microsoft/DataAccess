// -----------------------------------------------------------------------
// <copyright company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// A wrapper for IDataReader that allows to map columns
    /// </summary>
    public sealed class MappedDataReader : IDataReader
    {
        /// <summary>
        /// The orginal data reader
        /// </summary>
        private readonly IDataReader source;

        /// <summary>
        /// The mappings
        /// </summary>
        private readonly IEnumerable<MapBase> mappings;

        /// <summary>
        /// Map of ordinals to names.
        /// </summary>
        private readonly Dictionary<int, string> ordinalMap = new Dictionary<int, string>();

        /// <summary>
        /// Map of names to ordinals.
        /// </summary>
        private readonly Dictionary<string, int> nameMap = new Dictionary<string, int>();

        /// <summary>
        /// Map of target properties to source properties
        /// </summary>
        private readonly Dictionary<string, string> propertyMap = new Dictionary<string, string>();

        /// <summary>
        /// Map of ordinals to value mappings.
        /// </summary>
        private readonly Dictionary<int, ValueMappings> valueMap = new Dictionary<int, ValueMappings>();

        /// <summary>
        /// Value extractor
        /// </summary>
        private readonly ValueExtractor valueExtractor;

        /// <summary>
        /// Initialize a new instance of MappedDataReader
        /// </summary>
        /// <param name="source">The source data reader</param>
        /// <param name="map">The load map</param>
        public MappedDataReader(IDataReader source, LoadMap map)
            : this(source, map.Mappings)
        {
            EntityTypeReference loadFrom = map.LoadFrom as EntityTypeReference;
            if (loadFrom != null && map.LoadTo is DimensionReference)
            {
                this.valueExtractor = new EntityToDimensionValueExtractor(loadFrom, this);
            }
        }

        /// <summary>
        /// Initialize a new instance of MappedDataReader
        /// </summary>
        /// <param name="source">The source data reader</param>
        /// <param name="mappings">The mappings</param>
        private MappedDataReader(IDataReader source, IEnumerable<MapBase> mappings)
        {
            this.source = source;
            this.mappings = mappings;
            this.valueExtractor = new DefaultValueExtractor(this);
            this.Initialize();
        }

        /// <summary>
        /// Indexer into the current item by name.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The value for the given name.</returns>
        object IDataRecord.this[string name]
        {
            get
            {
                int ordinal = this.nameMap[name];
                return this.GetValue(ordinal);
            }
        }

        /// <summary>
        /// Indexer into the current item by ordinal.
        /// </summary>
        /// <param name="i">The ordinal.</param>
        /// <returns>The value for the given ordinal.</returns>
        object IDataRecord.this[int i]
        {
            get { return this.GetValue(i); }
        }

        /// <summary>
        /// Gets the depth of the reader.
        /// </summary>
        int IDataReader.Depth
        {
            get { return this.source.Depth; }
        }

        /// <summary>
        /// Gets a count of items in the array.
        /// </summary>
        int IDataReader.RecordsAffected
        {
            get { return this.source.RecordsAffected; }
        }

        /// <summary>
        /// Gets a value indicating whether the reader is closed.
        /// </summary>
        bool IDataReader.IsClosed
        {
            get { return this.source.IsClosed; }
        }

        /// <summary>
        /// Gets the field count for the current item.
        /// </summary>
        int IDataRecord.FieldCount
        {
            get { return this.nameMap.Keys.Count(); }
        }

        /// <summary>
        /// Gets the schema for the current reader.
        /// </summary>
        /// <returns>The schema table.</returns>
        DataTable IDataReader.GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Closed the reader.
        /// </summary>
        void IDataReader.Close()
        {
            this.source.Close();
        }

        /// <summary>
        /// Advances to the next result.
        /// </summary>
        /// <returns>True if there is a next result, otherwise false.</returns>
        bool IDataReader.NextResult()
        {
            return this.source.NextResult();
        }

        /// <summary>
        /// Read the next item in the reader.
        /// </summary>
        /// <returns>True if reader still readable, otherwise false.</returns>
        bool IDataReader.Read()
        {
            return this.source.Read();
        }

        /// <summary>
        /// Disposes the reader.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.source.Dispose();
        }

        /// <summary>
        /// Return the boolen at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The boolean value.</returns>
        bool IDataRecord.GetBoolean(int i)
        {
            return (bool)this.GetValue(i);
        }

        /// <summary>
        /// Return the boolen at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The boolean value.</returns>
        byte IDataRecord.GetByte(int i)
        {
            return (byte)this.GetValue(i);
        }

        /// <summary>
        /// Popualte an array of bytes.
        /// </summary>
        /// <param name="i">The ordinal position.</param>
        /// <param name="fieldOffset">The offset.</param>
        /// <param name="buffer">The buffer to populate.</param>
        /// <param name="bufferoffset">The buffer offset.</param>
        /// <param name="length">The length of data to write.</param>
        /// <returns>Total number of bytes in source array.</returns>
        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
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
        char IDataRecord.GetChar(int i)
        {
            return (char)this.GetValue(i);
        }

        /// <summary>
        /// Popualte an array of characters.
        /// </summary>
        /// <param name="i">The ordinal position.</param>
        /// <param name="fieldOffset">The offset.</param>
        /// <param name="buffer">The buffer to populate.</param>
        /// <param name="bufferoffset">The buffer offset.</param>
        /// <param name="length">The length of data to write.</param>
        /// <returns>Total number of characters in source array.</returns>
        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
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
        IDataReader IDataRecord.GetData(int i)
        {
            IDataReader reader = null;
            string name;
            if (this.ordinalMap.TryGetValue(i, out name) == false)
            {
                throw new KeyNotFoundException(string.Format("Cannot find data at position {0}", i));
            }

            string sourceName;
            if (this.propertyMap.TryGetValue(name, out sourceName) == false)
            {
                throw new KeyNotFoundException(string.Format("Cannot find key {0} in property map", name));
            }

            IEnumerable<DimensionMap> mappings = this.mappings.OfType<DimensionMap>().Where(x => x.To == name && x.From == sourceName);
            if (mappings.Any())
            {
                if (mappings.Count() != 1)
                {
                    throw new NotSupportedException();
                }

                IDataReader childReader = this.source.GetData(this.source.GetOrdinal(sourceName));
                if (childReader != null)
                {
                    reader = new MappedDataReader(childReader, mappings.Single().Properties);
                }
            }

            return reader;
        }

        /// <summary>
        /// Returns the type of the data in the given field ordinal.
        /// </summary>
        /// <param name="i">The field ordinal.</param>
        /// <returns>The type of the data.</returns>
        string IDataRecord.GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the datetime at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The datetime value.</returns>
        DateTime IDataRecord.GetDateTime(int i)
        {
            return (DateTime)this.GetValue(i);
        }

        /// <summary>
        /// Return the decimal at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The decimal value.</returns>
        decimal IDataRecord.GetDecimal(int i)
        {
            return (decimal)this.GetValue(i);
        }

        /// <summary>
        /// Return the double at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The double value.</returns>
        double IDataRecord.GetDouble(int i)
        {
            return (double)this.GetValue(i);
        }

        /// <summary>
        /// Returns the type of the data in the given field ordinal.
        /// </summary>
        /// <param name="i">The field ordinal.</param>
        /// <returns>The type of the data.</returns>
        Type IDataRecord.GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the float at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The float value.</returns>
        float IDataRecord.GetFloat(int i)
        {
            return (float)this.GetValue(i);
        }

        /// <summary>
        /// Return the guid at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The guid value.</returns>
        Guid IDataRecord.GetGuid(int i)
        {
            return (Guid)this.GetValue(i);
        }

        /// <summary>
        /// Return the short at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The short value.</returns>
        short IDataRecord.GetInt16(int i)
        {
            return (short)this.GetValue(i);
        }

        /// <summary>
        /// Return the int at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The int value.</returns>
        int IDataRecord.GetInt32(int i)
        {
            return (int)this.GetValue(i);
        }

        /// <summary>
        /// Return the long at the given position.
        /// </summary>
        /// <param name="i">The long to inspect.</param>
        /// <returns>The boolean value.</returns>
        long IDataRecord.GetInt64(int i)
        {
            return (long)this.GetValue(i);
        }

        /// <summary>
        /// Returns the name for a given ordinal.
        /// </summary>
        /// <param name="i">The ordinal to search for.</param>
        /// <returns>The corresponding name.</returns>
        string IDataRecord.GetName(int i)
        {
            if (this.ordinalMap.ContainsKey(i) == true)
            {
                return this.ordinalMap[i];
            }

            return null;
        }

        /// <summary>
        /// Returns the ordinal for a given name.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The corresponding ordinal.</returns>
        int IDataRecord.GetOrdinal(string name)
        {
            if (this.nameMap.ContainsKey(name) == true)
            {
                return this.nameMap[name];
            }

            return -1;
        }

        /// <summary>
        /// Return the string at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The string value.</returns>
        string IDataRecord.GetString(int i)
        {
            return this.GetValue(i).ToString();
        }

        /// <summary>
        /// Return the object at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The object value.</returns>
        object IDataRecord.GetValue(int i)
        {
            return this.GetValue(i);
        }

        /// <summary>
        /// Populates an array with values from the current item.
        /// </summary>
        /// <param name="values">The array to populate.</param>
        /// <returns>The number of values added to the array.</returns>
        int IDataRecord.GetValues(object[] values)
        {
            for (int i = 0; i < this.nameMap.Keys.Count(); i++)
            {
                values[i] = this.GetValue(i);
            }

            return this.nameMap.Keys.Count();
        }

        /// <summary>
        /// Indicates whether the indexed value is null.
        /// </summary>
        /// <param name="i">The ordinal position to inspect.</param>
        /// <returns>True if null, otherwise false.</returns>
        bool IDataRecord.IsDBNull(int i)
        {
            return this.GetValue(i) == null;
        }

        /// <summary>
        /// Cast the serialized value into its correct value type.
        /// </summary>
        /// <param name="dataType">The data type of the target value.</param>
        /// <param name="value">The serialized value.</param>
        /// <returns>The cast value.</returns>
        private static object Cast(DataType dataType, string value)
        {
            switch (dataType)
            {
                case DataType.@string:
                    return value;

                case DataType.@int:
                    return int.Parse(value);

                case DataType.@bool:
                    return bool.Parse(value);

                case DataType.@long:
                    return long.Parse(value);

                case DataType.datetimeoffset:
                    return DateTimeOffset.Parse(value);

                case DataType.datetime:
                    return DateTime.Parse(value);

                case DataType.@decimal:
                    return decimal.Parse(value);

                case DataType.@double:
                    return double.Parse(value);

                case DataType.@float:
                    return float.Parse(value);

                case DataType.@short:
                    return short.Parse(value);

                case DataType.@byte:
                    return byte.Parse(value);

                case DataType.guid:
                    return Guid.Parse(value);

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Initialize the object state
        /// </summary>
        private void Initialize()
        {
            int ordinal = 0;
            foreach (SimpleMap mapping in this.mappings.OfType<SimpleMap>())
            {
                this.nameMap[mapping.To] = ordinal;
                this.ordinalMap[ordinal] = mapping.To;
                this.propertyMap[mapping.To] = mapping.From;
                if (mapping.ValueMappings != null && mapping.ValueMappings.ValueMaps.Count > 0)
                {
                    this.valueMap[ordinal] = mapping.ValueMappings;
                }

                ++ordinal;
            }

            foreach (DimensionMap mapping in this.mappings.OfType<DimensionMap>())
            {
                this.nameMap[mapping.To] = ordinal;
                this.ordinalMap[ordinal] = mapping.To;
                this.propertyMap[mapping.To] = mapping.From;
                ++ordinal;
            }
        }

        /// <summary>
        /// Return the object at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The object value.</returns>
        private object GetValue(int i)
        {
            return this.valueExtractor.GetValue(i);
        }

        /// <summary>
        /// Base class to extract value
        /// </summary>
        private abstract class ValueExtractor
        {
            /// <summary>
            /// The MappedDataReader instance
            /// </summary>
            protected readonly MappedDataReader mappedDataReader;

            /// <summary>
            /// Initializes a new instance of ValueExtractor
            /// </summary>
            /// <param name="mappedDataReader">The instance of MappedDataReader</param>
            protected ValueExtractor(MappedDataReader mappedDataReader)
            {
                this.mappedDataReader = mappedDataReader;
            }

            /// <summary>
            /// Return the value at the given ordinal
            /// </summary>
            /// <param name="ordinal">The ordinal</param>
            /// <returns>The value</returns>
            public virtual object GetValue(int ordinal)
            {
                string name = this.mappedDataReader.ordinalMap[ordinal];
                string sourceName = this.mappedDataReader.propertyMap[name];
                object value = this.mappedDataReader.source[sourceName];
                if (this.mappedDataReader.valueMap.ContainsKey(ordinal) == true)
                {
                    ValueMappings mappings = this.mappedDataReader.valueMap[ordinal];
                    foreach (ValueMap map in mappings.ValueMaps)
                    {
                        if (value.ToString() == map.From)
                        {
                            return MappedDataReader.Cast(mappings.ToType, map.To);
                        }
                    }
                }

                return value;
            }
        }

        /// <summary>
        /// Value extractor that uses a direct column to column mapping
        /// </summary>
        private sealed class DefaultValueExtractor : ValueExtractor
        {
            /// <summary>
            /// Initializes a new instance of DefaultValueExtractor
            /// </summary>
            /// <param name="mappedDataReader">The instance of MappedDataReader</param>
            public DefaultValueExtractor(MappedDataReader mappedDataReader)
                : base(mappedDataReader)
            {
            }
        }

        /// <summary>
        /// Value extractor that extract the key from IDataReader if the type is IDataReader, otherwise returns the raw value
        /// </summary>
        private sealed class EntityToDimensionValueExtractor : ValueExtractor
        {
            /// <summary>
            /// Feed reference to load from
            /// </summary>
            private readonly EntityTypeReference loadFrom;

            /// <summary>
            /// Initializes a new instance of EntityToDimensionValueExtractor
            /// </summary>
            /// <param name="loadFrom">The entity type reference to load from</param>
            /// <param name="mappedDataReader">The instance of MappedDataReader</param>
            public EntityToDimensionValueExtractor(EntityTypeReference loadFrom, MappedDataReader mappedDataReader)
                : base(mappedDataReader)
            {
                this.loadFrom = loadFrom;
            }

            /// <summary>
            /// Return the value at the given ordinal
            /// </summary>
            /// <param name="ordinal">The ordinal</param>
            /// <returns>The value</returns>
            public override object GetValue(int ordinal)
            {
                string name = this.mappedDataReader.ordinalMap[ordinal];
                string sourceName = this.mappedDataReader.propertyMap[name];
                object value = base.GetValue(ordinal);

                using (IDataReader reader = value as IDataReader)
                {
                    if (reader != null)
                    {
                        EntityType entityType = ConfigurationCache.Instance.GetWellKnownInstance<EntityType>(this.loadFrom);
                        NavigationProperty navProp = entityType.NavigationProperties.SingleOrDefault(x => x.Name == sourceName);
                        if (navProp != null)
                        {
                            if (navProp.Association == AssociationType.ManyToOne || navProp.Association == AssociationType.OneToOne)
                            {
                                EntityType targetType = ConfigurationCache.Instance.GetWellKnownInstance<EntityType>(navProp.TargetType);
                                if (reader.Read())
                                {
                                    value = reader[targetType.Keys.Single().Name];
                                }
                            }
                        }
                    }
                }

                return value;
            }
        }
    }
}
