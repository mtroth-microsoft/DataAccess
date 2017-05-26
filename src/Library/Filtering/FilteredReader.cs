// -----------------------------------------------------------------------
// <copyright file="FilteredReader.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Add a filtering capability to IDataReader
    /// </summary>
    public sealed class FilteredReader : IDataReader
    {
        /// <summary>
        /// A map that associate columns (name) to predicates
        /// </summary>
        private readonly Dictionary<string, Func<object, bool>> filters = new Dictionary<string, Func<object, bool>>();

        /// <summary>
        /// The source data reader that needs filtering
        /// </summary>
        private IDataReader source;

        /// <summary>
        /// Indicates if the object has been disposed
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Indicates if the data record has been closed
        /// </summary>
        private bool isClosed = false;

        /// <summary>
        /// Initializes a new instance of FilteredReader
        /// </summary>
        /// <param name="source">The source data reader</param>
        public FilteredReader(IDataReader source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.source = source;
        }

        /// <summary>
        /// This method is used to add a filter to a column. Filters are cleared when switching to next reader (IDataReader.NextResult).
        /// </summary>
        /// <param name="columnName">The column to add a filter for</param>
        /// <param name="predicate">The predicate that must return true if the row is to be kept</param>
        public void AddFilter<T>(string columnName, Func<T, bool> predicate)
        {
            this.filters.Add(columnName,
                x =>
                {
                    T arg = (T)Convert.ChangeType(x, typeof(T));
                    return predicate(arg);
                });
        }

        /// <summary>
        /// Closes the System.Data.IDataReader Object.
        /// </summary>
        void IDataReader.Close()
        {
            this.CheckNotDisposed();
            this.source.Close();
            this.isClosed = true;
        }

        /// <summary>
        /// Gets a value indicating the depth of nesting for the current row.
        /// </summary>
        int IDataReader.Depth
        {
            get
            {
                this.CheckNotDisposed();
                return this.source.Depth;
            }
        }

        /// <summary>
        /// Returns a System.Data.DataTable that describes the column metadata of the System.Data.IDataReader.
        /// </summary>
        /// <returns>A System.Data.DataTable that describes the column metadata.</returns>
        DataTable IDataReader.GetSchemaTable()
        {
            this.CheckNotDisposed();
            return this.source.GetSchemaTable();
        }

        /// <summary>
        /// Gets a value indicating whether the data reader is closed.
        /// </summary>
        bool IDataReader.IsClosed
        {
            get { return this.isClosed; }
        }

        /// <summary>
        /// Advances the data reader to the next result, when reading the results of batch SQL statements.
        /// This method also clears all filters.
        /// </summary>
        /// <returns>True if there are more rows; otherwise, false.</returns>
        bool IDataReader.NextResult()
        {
            this.CheckNotDisposed();
            this.filters.Clear();
            return this.source.NextResult();
        }

        /// <summary>
        /// Advances the System.Data.IDataReader to the next record.
        /// </summary>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        bool IDataReader.Read()
        {
            this.CheckNotDisposed();
            bool read = this.source.Read();
            while (read && this.SkipCurrentRow())
            {
                read = this.source.Read();
            }

            return read;
        }

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        int IDataReader.RecordsAffected
        {
            get { return -1; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (!this.isDisposed)
            {
                this.source.Dispose();
                this.filters.Clear();
                this.isDisposed = true;
                this.isClosed = true;
                this.source = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        int IDataRecord.FieldCount
        {
            get
            {
                this.CheckNotDisposed();
                return this.source.FieldCount;
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        bool IDataRecord.GetBoolean(int i)
        {
            this.CheckNotDisposed();
            return source.GetBoolean(i);
        }

        /// <summary>
        /// Gets the 8-bit unsigned integer value of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The 8-bit unsigned integer value of the specified column.</returns>
        byte IDataRecord.GetByte(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetByte(i);
        }

        /// <summary>
        /// Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldOffset">The index within the field from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferoffset">The index for buffer to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            this.CheckNotDisposed();
            return this.source.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// Gets the character value of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The character value of the specified column.</returns>
        char IDataRecord.GetChar(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetChar(i);
        }

        /// <summary>
        /// Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldoffset">The index within the row from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferoffset">The index for buffer to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The actual number of characters read.</returns>
        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            this.CheckNotDisposed();
            return this.source.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// Returns an System.Data.IDataReader for the specified column ordinal.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The System.Data.IDataReader for the specified column ordinal.</returns>
        IDataReader IDataRecord.GetData(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetData(i);
        }

        /// <summary>
        /// Gets the data type information for the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The data type information for the specified field.</returns>
        string IDataRecord.GetDataTypeName(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetDataTypeName(i);
        }

        /// <summary>
        /// Gets the date and time data value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The date and time data value of the specified field.</returns>
        DateTime IDataRecord.GetDateTime(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetDateTime(i);
        }

        /// <summary>
        /// Gets the fixed-position numeric value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The fixed-position numeric value of the specified field.</returns>
        decimal IDataRecord.GetDecimal(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetDecimal(i);
        }

        /// <summary>
        /// Gets the double-precision floating point number of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The double-precision floating point number of the specified field.</returns>
        double IDataRecord.GetDouble(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetDouble(i);
        }

        /// <summary>
        /// Gets the System.Type information corresponding to the type of System.Object that would be returned from System.Data.IDataRecord.GetValue(System.Int32).
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The System.Type information corresponding to the type of System.Object that would be returned from System.Data.IDataRecord.GetValue(System.Int32).</returns>
        Type IDataRecord.GetFieldType(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetFieldType(i);
        }

        /// <summary>
        /// Gets the single-precision floating point number of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The single-precision floating point number of the specified field.</returns>
        float IDataRecord.GetFloat(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetFloat(i);
        }

        /// <summary>
        /// Returns the GUID value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The GUID value of the specified field.</returns>
        Guid IDataRecord.GetGuid(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetGuid(i);
        }

        /// <summary>
        /// Gets the 16-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The 16-bit signed integer value of the specified field.</returns>
        short IDataRecord.GetInt16(int i)
        {
            return this.source.GetInt16(i);
        }

        /// <summary>
        /// Gets the 32-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The 32-bit signed integer value of the specified field.</returns>
        int IDataRecord.GetInt32(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetInt32(i);
        }

        /// <summary>
        /// Gets the 64-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The 64-bit signed integer value of the specified field.</returns>
        long IDataRecord.GetInt64(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetInt64(i);
        }

        /// <summary>
        /// Gets the name for the field to find.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The name of the field or the empty string (""), if there is no value to return.</returns>
        string IDataRecord.GetName(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetName(i);
        }

        /// <summary>
        /// Return the index of the named field.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        /// <returns>The index of the named field.</returns>
        int IDataRecord.GetOrdinal(string name)
        {
            this.CheckNotDisposed();
            return this.source.GetOrdinal(name);
        }

        /// <summary>
        /// Gets the string value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The string value of the specified field.</returns>
        string IDataRecord.GetString(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetString(i);
        }

        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The System.Object which will contain the field value upon return.</returns>
        object IDataRecord.GetValue(int i)
        {
            this.CheckNotDisposed();
            return this.source.GetValue(i);
        }

        /// <summary>
        /// Populates an array of objects with the column values of the current record.
        /// </summary>
        /// <param name="values">An array of System.Object to copy the attribute fields into.</param>
        /// <returns>The number of instances of System.Object in the array.</returns>
        int IDataRecord.GetValues(object[] values)
        {
            this.CheckNotDisposed();
            return this.source.GetValues(values);
        }

        /// <summary>
        /// Return whether the specified field is set to null.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>true if the specified field is set to null; otherwise, false.</returns>
        bool IDataRecord.IsDBNull(int i)
        {
            this.CheckNotDisposed();
            return this.source.IsDBNull(i);
        }

        /// <summary>
        /// Gets the column with the specified name.
        /// </summary>
        /// <param name="name">The name of the column to find.</param>
        /// <returns>The column with the specified name as an System.Object.</returns>
        object IDataRecord.this[string name]
        {
            get
            {
                this.CheckNotDisposed();
                return this.source[name];
            }
        }

        /// <summary>
        /// Gets the column located at the specified index.
        /// </summary>
        /// <param name="i">The zero-based index of the column to get.</param>
        /// <returns>The column located at the specified index as an System.Object.</returns>
        object IDataRecord.this[int i]
        {
            get
            {
                this.CheckNotDisposed();
                return this.source[i];
            }
        }

        /// <summary>
        /// Indicate whether the current row should be skipped.
        /// </summary>
        /// <returns>True if the row should be skipped, false otherwise.</returns>
        private bool SkipCurrentRow()
        {
            foreach (string colName in this.filters.Keys)
            {
                Func<object, bool> predicate = this.filters[colName];
                if (!predicate(this.source[colName]))
                {
                    return true;
                }
            }

            return false;
        }

        private void CheckNotDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("source");
            }
        }
    }
}
