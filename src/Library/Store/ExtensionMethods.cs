// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Data;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Class for extension methods.
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Dispose a connection and swallow any already disposed exception that it might throw.
        /// </summary>
        /// <param name="connection">The connection to dispose.</param>
        public static void SafeDispose(this IDbConnection connection)
        {
            try
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }

                connection.Dispose();
            }
            catch (ObjectDisposedException) { }
        }

        /// <summary>
        /// Write the given reader to a csv file.
        /// </summary>
        /// <param name="dataReader">The reader to write.</param>
        /// <param name="includeHeaderAsFirstRow">True to include headers as the first row, otherwise false.</param>
        /// <param name="separator">The field separator.</param>
        /// <param name="fileName">The file to write the data to.</param>
        /// <returns></returns>
        public static void ToCSV(
            this IDataReader dataReader, 
            bool includeHeaderAsFirstRow, 
            string separator,
            string fileName)
        {
            if (File.Exists(fileName) == true)
            {
                throw new InvalidOperationException("File already exists.");
            }

            StringBuilder sb = null;
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                if (includeHeaderAsFirstRow)
                {
                    sb = new StringBuilder();
                    for (int index = 0; index < dataReader.FieldCount; index++)
                    {
                        if (dataReader.GetName(index) != null)
                        {
                            sb.Append(dataReader.GetName(index));
                        }

                        if (index < dataReader.FieldCount - 1)
                        {
                            sb.Append(separator);
                        }
                    }

                    writer.WriteLine(sb.ToString());
                }

                while (dataReader.Read())
                {
                    sb = new StringBuilder();
                    for (int index = 0; index < dataReader.FieldCount - 1; index++)
                    {
                        if (dataReader.IsDBNull(index) == false)
                        {
                            object raw = dataReader.GetValue(index);
                            string value = raw.ToString();
                            Type type = dataReader.GetFieldType(index);
                            if (type == typeof(string))
                            {
                                if (value.IndexOf("\"") >= 0)
                                {
                                    value = value.Replace("\"", "\\\"");
                                }

                                if (value.IndexOf(separator) >= 0)
                                {
                                    value = "\"" + value + "\"";
                                }
                            }
                            else if (type == typeof(DateTime))
                            {
                                value = "\"" + value + "\"";
                            }
                            else if (type == typeof(DateTimeOffset))
                            {
                                DateTimeOffset offset = (DateTimeOffset)raw;
                                DateTime dt = offset.UtcDateTime;
                                value = "\"" + dt.ToString("o") + "\"";
                            }
                            else if (type.IsEnum)
                            {
                                Type under = Enum.GetUnderlyingType(type);
                                if (under == typeof(byte))
                                {
                                    byte number = (byte)raw;
                                    value = number.ToString();
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }

                            sb.Append(value);
                        }

                        if (index < dataReader.FieldCount - 1)
                        {
                            sb.Append(separator);
                        }
                    }

                    if (dataReader.IsDBNull(dataReader.FieldCount - 1) == false)
                    {
                        sb.Append(dataReader.GetValue(dataReader.FieldCount - 1).ToString().Replace(separator, " "));
                    }

                    writer.WriteLine(sb.ToString());
                }
            }
        }
    }
}
