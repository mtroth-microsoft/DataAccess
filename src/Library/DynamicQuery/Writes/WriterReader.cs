// -----------------------------------------------------------------------
// <copyright file="WriterReader.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Parse json and expose as IDataReader
    /// </summary>
    internal sealed class WriterReader : IDataReader
    {
        /// <summary>
        /// The originating type for write records.
        /// </summary>
        private Type rootType;

        /// <summary>
        /// Indicates whether the reader is writable still.
        /// A reader is no longer writeable once read iteration has begun.
        /// </summary>
        private bool writeable = true;

        /// <summary>
        /// List of write sets.
        /// </summary>
        private List<WriteSet> writeSets = new List<WriteSet>();

        /// <summary>
        /// List of supplemental data values (primary key values from another instance).
        /// </summary>
        private List<SupplementalValue> supplementalData = new List<SupplementalValue>();

        /// <summary>
        /// The writer set index.
        /// </summary>
        private int writerIndex = 0;

        /// <summary>
        /// Initializes a new instance of the WriterReader class.
        /// </summary>
        /// <param name="typeToWrite">The type of the entity to write.</param>
        public WriterReader(Type typeToWrite)
        {
            this.rootType = typeToWrite;
            IQueryBuilder builder = TypeCache.ReflectCorrectBuilder(typeToWrite, new QueryBuilderSettings());
            this.writeSets = CreateWriteSets(typeToWrite, builder.Query.Source);
        }

        /// <summary>
        /// Initializes a new instance of the WriterReader class.
        /// </summary>
        /// <param name="set">The prepopulated write set.</param>
        private WriterReader(WriteSet set)
        {
            this.rootType = set.WriteType;
            this.writeSets.Add(set);
            this.writeable = false;
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
                this.CheckClosed();
                return this.GetValue(this.GetOrdinal(name));
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
                this.CheckClosed();
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
                this.CheckClosed();
                return 0;
            }
        }

        /// <summary>
        /// Gets the field count for the current item.
        /// </summary>
        public int FieldCount
        {
            get
            {
                this.CheckClosed();
                return this.writeSets[this.writerIndex].FieldCount;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the reader is closed.
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return this.writerIndex >= this.writeSets.Count;
            }
        }

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        public int RecordsAffected
        {
            get
            {
                this.CheckClosed();
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
                this.CheckClosed();
                return this.writeSets[this.writerIndex].RowCount;
            }
        }

        /// <summary>
        /// Gets the current write set.
        /// </summary>
        internal WriteSet Current
        {
            get
            {
                this.InitializeRead();
                if (this.writerIndex < this.writeSets.Count)
                {
                    return this.writeSets[this.writerIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the 
        /// supplement must run before the data it supplements.
        /// </summary>
        internal bool MustRunFirst
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the column being supplemented, if applicable.
        /// </summary>
        internal QueryColumn SupplementedColumn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the collection of supplemental values.
        /// </summary>
        internal ICollection<SupplementalValue> SupplementalValues
        {
            get
            {
                return this.supplementalData.AsReadOnly();
            }
        }

        /// <summary>
        /// Adds a supplemental value to the supplemental reader.
        /// </summary>
        internal void AddSupplementalValue(SupplementalValue value)
        {
            if (this.SupplementedColumn == null)
            {
                throw new InvalidOperationException("This reader is not a supplement.");
            }

            this.supplementalData.Add(value);
        }

        /// <summary>
        /// Preview the next instance in the current set.
        /// </summary>
        /// <returns>The peeked instance.</returns>
        public object PeekNextInstance()
        {
            return this.Current?.PeekNextInstance();
        }

        /// <summary>
        /// Splits the current reader into its component readers.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IDataReader> Split()
        {
            this.CheckClosed();
            this.InitializeRead();

            List<IDataReader> readers = new List<IDataReader>();
            foreach (WriteSet set in this.writeSets)
            {
                readers.Add(new WriterReader(set));
            }

            return readers;
        }

        /// <summary>
        /// Add an instance to the writer reader.
        /// </summary>
        /// <param name="jobject">Json blob representing an instance of the root type.</param>
        /// <returns>The instance.</returns>
        public object Add(JObject jobject)
        {
            if (this.writeable == true)
            {
                Type type = this.rootType;
                foreach (JProperty o in jobject.Properties())
                {
                    if (o.Name.EndsWith("$type", StringComparison.OrdinalIgnoreCase) == true ||
                        o.Name.EndsWith("#type", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        string typeName = o.Value.ToString();
                        int pos = typeName.IndexOf(',');
                        if (pos > 0)
                        {
                            typeName = typeName.Substring(0, pos);
                        }

                        type = TypeCache.LocateType(typeName);
                        break;
                    }
                }

                object instance = Activator.CreateInstance(type);
                Type test = type;
                while (test != typeof(object))
                {
                    WriteSet set = this.writeSets.Where(p => p.WriteType == test).SingleOrDefault();
                    if (set != null)
                    {
                        set.Add(jobject, instance);
                    }

                    test = test.BaseType;
                }

                return instance;
            }
            else
            {
                throw new NotSupportedException("No more data can be added once reading has begun.");
            }
        }

        /// <summary>
        /// Closed the reader.
        /// </summary>
        public void Close()
        {
            this.CheckClosed();
            this.writerIndex = this.writeSets.Count;
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
            this.CheckClosed();
            return (bool)this.GetValue(i);
        }

        /// <summary>
        /// Return the boolen at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The boolean value.</returns>
        public byte GetByte(int i)
        {
            this.CheckClosed();
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
            this.CheckClosed();
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
            this.CheckClosed();
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
            this.CheckClosed();
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
            this.CheckClosed();
            return (IDataReader)this.GetValue(i);
        }

        /// <summary>
        /// Returns the type of the data in the given field ordinal.
        /// </summary>
        /// <param name="i">The field ordinal.</param>
        /// <returns>The type of the data.</returns>
        public string GetDataTypeName(int i)
        {
            this.CheckClosed();
            return this.GetFieldType(i).Name;
        }

        /// <summary>
        /// Return the datetime at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The datetime value.</returns>
        public DateTime GetDateTime(int i)
        {
            this.CheckClosed();
            return (DateTime)this.GetValue(i);
        }

        /// <summary>
        /// Return the decimal at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The decimal value.</returns>
        public decimal GetDecimal(int i)
        {
            this.CheckClosed();
            return (decimal)this.GetValue(i);
        }

        /// <summary>
        /// Return the double at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The double value.</returns>
        public double GetDouble(int i)
        {
            this.CheckClosed();
            return (double)this.GetValue(i);
        }

        /// <summary>
        /// Returns the type of the data in the given field ordinal.
        /// </summary>
        /// <param name="i">The field ordinal.</param>
        /// <returns>The type of the data.</returns>
        public Type GetFieldType(int i)
        {
            this.CheckClosed();
            return this.writeSets[this.writerIndex].GetFieldType(i);
        }

        /// <summary>
        /// Return the float at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The float value.</returns>
        public float GetFloat(int i)
        {
            this.CheckClosed();
            return (float)this.GetValue(i);
        }

        /// <summary>
        /// Return the guid at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The guid value.</returns>
        public Guid GetGuid(int i)
        {
            this.CheckClosed();
            return (Guid)this.GetValue(i);
        }

        /// <summary>
        /// Return the short at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The short value.</returns>
        public short GetInt16(int i)
        {
            this.CheckClosed();
            return (short)this.GetValue(i);
        }

        /// <summary>
        /// Return the int at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The int value.</returns>
        public int GetInt32(int i)
        {
            this.CheckClosed();
            return (int)this.GetValue(i);
        }

        /// <summary>
        /// Return the long at the given position.
        /// </summary>
        /// <param name="i">The long to inspect.</param>
        /// <returns>The boolean value.</returns>
        public long GetInt64(int i)
        {
            this.CheckClosed();
            return (long)this.GetValue(i);
        }

        /// <summary>
        /// Returns the name for a given ordinal.
        /// </summary>
        /// <param name="i">The ordinal to search for.</param>
        /// <returns>The corresponding name.</returns>
        public string GetName(int i)
        {
            this.CheckClosed();
            return this.writeSets[this.writerIndex].GetName(i);
        }

        /// <summary>
        /// Returns the ordinal for a given name.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The corresponding ordinal.</returns>
        public int GetOrdinal(string name)
        {
            this.CheckClosed();
            return this.writeSets[this.writerIndex].GetOrdinal(name);
        }

        /// <summary>
        /// Gets the schema for the current reader.
        /// </summary>
        /// <returns>The schema table.</returns>
        public DataTable GetSchemaTable()
        {
            this.CheckClosed();
            return this.writeSets[this.writerIndex].GetSchemaTable();
        }

        /// <summary>
        /// Return the string at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The string value.</returns>
        public string GetString(int i)
        {
            this.CheckClosed();
            return this.GetValue(i).ToString();
        }

        /// <summary>
        /// Return the object at the given position.
        /// </summary>
        /// <param name="i">The ordinal to inspect.</param>
        /// <returns>The object value.</returns>
        public object GetValue(int i)
        {
            this.CheckClosed();
            return this.writeSets[this.writerIndex].GetValue(i);
        }

        /// <summary>
        /// Populates an array with values from the current item.
        /// </summary>
        /// <param name="values">The array to populate.</param>
        /// <returns>The number of values added to the array.</returns>
        public int GetValues(object[] values)
        {
            this.CheckClosed();
            return this.writeSets[this.writerIndex].GetValues(values);
        }

        /// <summary>
        /// Indicates whether the indexed value is null.
        /// </summary>
        /// <param name="i">The ordinal position to inspect.</param>
        /// <returns>True if null, otherwise false.</returns>
        public bool IsDBNull(int i)
        {
            this.CheckClosed();
            return this.GetValue(i) == null;
        }

        /// <summary>
        /// Advances to the next result.
        /// </summary>
        /// <returns>True if there is a next result, otherwise false.</returns>
        public bool NextResult()
        {
            this.CheckClosed();
            this.InitializeRead();
            if (this.writerIndex < this.writeSets.Count - 1)
            {
                this.writerIndex++;
                return true;
            }
            else
            {
                this.writerIndex = this.writeSets.Count;
                return false;
            }
        }

        /// <summary>
        /// Read the next item in the reader.
        /// </summary>
        /// <returns>True if reader still readable, otherwise false.</returns>
        public bool Read()
        {
            this.CheckClosed();
            this.InitializeRead();
            return this.writeSets[this.writerIndex].Read();
        }

        /// <summary>
        /// Reset the reader for reuse.
        /// </summary>
        public void Reset()
        {
            this.writeSets[this.writerIndex].Reset();
        }

        /// <summary>
        /// Create standard write sets.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="source">The query source to inspect.</param>
        /// <returns>The list of write sets.</returns>
        private static List<WriteSet> CreateWriteSets(Type typeToWrite, QuerySource source)
        {
            UnionQuery union = source as UnionQuery;
            SelectQuery select = source as SelectQuery;
            QueryTable table = source as QueryTable;
            if (union != null)
            {
                // Write base type instances.
                return CreateWriteSetsForBaseType(typeToWrite, union);
            }
            else if (select != null)
            {
                // Write Table per type instances.
                return CreateWriteSetsForTablePerType(typeToWrite, select);
            }
            else if (table != null)
            {
                // Write Table instances.
                return CreateWriteSetsForTable(typeToWrite, table);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Create write sets for a table.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="table">The table to inspect</param>
        /// <returns>The list of write sets.</returns>
        private static List<WriteSet> CreateWriteSetsForTable(Type typeToWrite, QueryTable table)
        {
            table.Hint = HintType.None;
            List<QueryColumn> columns = TypeCache.CreateColumns(table, typeToWrite)
                .Where(p => p.DeclaringType == typeToWrite || p.IsKeyColumn == true).ToList();

            List<WriteSet> writeSets = new List<WriteSet>();
            WriteSet ws = new WriteSet(typeToWrite, columns);
            writeSets.Add(ws);

            return writeSets;
        }

        /// <summary>
        /// Create write sets for table per type data.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="select">The query to inspect.</param>
        /// <returns>The list of write sets.</returns>
        private static List<WriteSet> CreateWriteSetsForTablePerType(Type typeToWrite, SelectQuery select)
        {
            List<WriteSet> writeSets = new List<WriteSet>();
            for (int i = select.Joins.Count - 1; i >= 0; i--)
            {
                QueryJoin join = select.Joins[i];
                List<WriteSet> nested = CreateWriteSets(join.Target.Type, join.Target);
                foreach (WriteSet set in nested)
                {
                    set.TablePerType = set == nested.First() && i < select.Joins.Count - 1;
                    if (writeSets.Any(p => p.WriteType == set.WriteType) == false)
                    {
                        writeSets.Add(set);
                    }
                }
            }

            int last = writeSets.Count;
            writeSets.AddRange(CreateWriteSets(typeToWrite, select.Source));
            writeSets[last].TablePerType = true;

            return writeSets;
        }

        /// <summary>
        /// Create write sets for base type data.
        /// </summary>
        /// <param name="typeToWrite">The type to inspect.</param>
        /// <param name="select">The query to inspect.</param>
        /// <returns>The list of write sets.</returns>
        private static List<WriteSet> CreateWriteSetsForBaseType(Type typeToWrite, UnionQuery union)
        {
            List<WriteSet> writeSets = new List<WriteSet>();
            foreach (SelectQuery query in union.Queries)
            {
                List<WriteSet> nested = CreateWriteSets(query.Type, query);
                foreach (WriteSet set in nested)
                {
                    if (writeSets.Any(p => p.WriteType == set.WriteType) == false)
                    {
                        writeSets.Add(set);
                    }
                }
            }

            return writeSets;
        }

        /// <summary>
        /// Check the closed state, throw if closed.
        /// </summary>
        private void CheckClosed()
        {
            if (this.IsClosed == true)
            {
                throw new InvalidOperationException("Cannot read from closed reader.");
            }
        }

        /// <summary>
        /// Initialize the write sets for read operations.
        /// </summary>
        private void InitializeRead()
        {
            if (this.writeable == true)
            {
                this.writeable = false;
                List<WriteSet> manyToManySets = new List<WriteSet>();
                foreach (WriteSet set in this.writeSets)
                {
                    manyToManySets.AddRange(set.LoadManyToManies());
                }

                this.writeSets.AddRange(manyToManySets);
            }
        }
    }
}
