// -----------------------------------------------------------------------
// <copyright file="SqlSerializationContext.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Helper class for serialization.
    /// </summary>
    internal class SqlSerializationContext : ISqlSerializationContext
    {
        /// <summary>
        /// Gets or sets the current table.
        /// </summary>
        public Table CurrentTable
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the serialization is for inline execution.
        /// </summary>
        public bool ForInline
        {
            get;
            set;
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <returns>The serialized instance.</returns>
        public string Serialize()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("-----------------------------------");
            builder.AppendLine("-- " + this.CurrentTable.Name);
            builder.AppendLine("-----------------------------------");

            string create = this.SerializeCreate();
            builder.Append(create);

            if (this.ForInline == false)
            {
                string columns = this.SerializeColumns();
                builder.Append(columns);
            }

            if (this.CurrentTable.Name[0] != '#')
            {
                string indexing = this.SerializeIndexing();
                builder.Append(indexing);

                string fkeys = this.SerializeForeignKeys();
                builder.Append(fkeys);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <returns>The serialized instance.</returns>
        private string SerializeCreate()
        {
            string separator = "  ";
            string schema = string.Empty;
            string pk = string.Empty;
            if (string.IsNullOrEmpty(this.CurrentTable.Owner) == false)
            {
                schema = string.Format("[{0}].", this.CurrentTable.Owner);
            }

            if (this.CurrentTable.Name[0] == '#')
            {
                schema = "tempdb..";
                pk = this.SerializeIndexing();
                if (string.IsNullOrEmpty(pk) == false)
                {
                    separator = "  ,";
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(
                "IF OBJECT_ID('{0}[{1}]') IS NULL ",
                schema,
                this.CurrentTable.Name);

            builder.AppendLine();
            builder.AppendFormat(
                "  CREATE TABLE {0}[{1}] (",
                schema,
                this.CurrentTable.Name);

            builder.AppendLine();
            if (this.ForInline == true)
            {
                string columns = this.SerializeColumns();
                builder.Append(columns);
            }
            else
            {
                Table.SchemaCollection collection = this.CurrentTable.Collections.Where(p => p.Name == "Indices").FirstOrDefault();
                foreach (TabularObject to in collection.Objects)
                {
                    Index ix = to as Index;
                    if (ix.IsPrimaryKey == true)
                    {
                        foreach (IndexColumn ixcol in ix.Partition.Columns)
                        {
                            Column c = this.CurrentTable.GetColumn(ixcol.ColumnName);
                            this.ForInline = true;
                            string result = this.SerializeColumn(c);
                            this.ForInline = false;

                            builder.Append(separator);
                            builder.Append(result);
                            builder.Append(",");
                            builder.AppendLine();
                        }

                        break;
                    }
                }
            }

            builder.Append(separator);
            builder.Append(pk);
            builder.Append(")");
            builder.AppendLine();
            builder.Append(";");

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <returns>The serialized instance.</returns>
        private string SerializeColumns()
        {
            string delimitter = "   ";
            StringBuilder builder = new StringBuilder();
            Table.SchemaCollection collection = this.CurrentTable.Collections.Where(p => p.Name == "Columns").First();
            foreach (TabularObject to in collection.Objects)
            {
                Column c = to as Column;
                string result = this.SerializeColumn(c);

                if (string.IsNullOrEmpty(result) == false)
                {
                    if (this.ForInline == true)
                    {
                        builder.Append(delimitter);
                    }

                    builder.Append(result);
                    builder.AppendLine();
                }

                delimitter = "  ,";
            }

            if (builder.Length > 0 && this.ForInline == false)
            {
                builder.Append(";");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <returns>The serialized instance.</returns>
        private string SerializeIndexing()
        {
            StringBuilder builder = new StringBuilder();
            Table.SchemaCollection collection = this.CurrentTable.Collections.Where(p => p.Name == "Indices").FirstOrDefault();
            if (collection == null)
            {
                return null;
            }

            foreach (TabularObject to in collection.Objects)
            {
                Index ix = to as Index;
                string result = this.SerializeIndex(ix);
                if (string.IsNullOrEmpty(result) == false)
                {
                    builder.Append(result);
                    builder.AppendLine();
                }
            }

            if (builder.Length > 0 && this.CurrentTable.Name[0] != '#')
            {
                builder.Append(";");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <returns>The serialized instance.</returns>
        private string SerializeForeignKeys()
        {
            StringBuilder builder = new StringBuilder();
            Table.SchemaCollection collection = this.CurrentTable.Collections.Where(p => p.Name == "ForeignKeys").FirstOrDefault();
            if (collection == null)
            {
                return null;
            }

            foreach (TabularObject to in collection.Objects)
            {
                string result = this.SerializeForeignKey(to as ForeignKey);
                if (string.IsNullOrEmpty(result) == false)
                {
                    builder.Append(result);
                    builder.AppendLine();
                }
            }

            if (builder.Length > 0)
            {
                builder.Append(";");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="column">The column to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeColumn(Column column)
        {
            StringBuilder builder = new StringBuilder();
            if (this.ForInline == false)
            {
                string schema = string.Empty;
                if (string.IsNullOrEmpty(this.CurrentTable.Owner) == false)
                {
                    schema = string.Format("[{0}].", this.CurrentTable.Owner);
                }

                if (this.CurrentTable.Name[0] == '#')
                {
                    schema = "tempdb..";
                }

                builder.AppendFormat(
                    "IF COL_LENGTH('{0}[{1}]', '{2}') IS NULL ",
                    schema,
                    this.CurrentTable.Name,
                    column.Name);

                builder.AppendFormat(
                    "ALTER TABLE {0}[{1}] ADD ",
                    schema,
                    this.CurrentTable.Name);
            }

            if (column.IsComputed == false)
            {
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "[{0}] {1}{2} {3} {4} ",
                    column.Name,
                    column.TypeName,
                    this.PrepareLength(column),
                    column.IsIdentity ? this.PrepareIdentity(column) : string.Empty,
                    column.IsNullable ? "NULL" : "NOT NULL");
            }
            else
            {
                builder.AppendFormat(
                    "[{0}] AS {1} PERSISTED {2} ",
                    column.Name,
                    column.Formula,
                    column.IsNullable ? "NULL" : "NOT NULL");
            }

            if (column.DefaultConstraint != null)
            {
                string result = this.SerializeDefaultConstraint(column.DefaultConstraint);
                builder.AppendFormat("{0} ", result);
            }

            if (column.CheckConstraint != null)
            {
                string result = this.SerializeCheck(column.CheckConstraint);
                builder.AppendFormat("{0} ", result);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="constraint">The constraint to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeDefaultConstraint(DefaultConstraint constraint)
        {
            StringBuilder builder = new StringBuilder();
            if (string.IsNullOrEmpty(constraint.Value) == false)
            {
                builder.AppendFormat("DEFAULT {0}", constraint.Value);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="fk">The key to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeForeignKey(ForeignKey fk)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat(
                "IF OBJECT_ID('[{0}].[{1}]') IS NULL ",
                fk.Owner,
                fk.Name);

            builder.AppendLine();
            builder.AppendFormat(
                "  ALTER TABLE [{0}].[{1}] ADD CONSTRAINT {2} ",
                this.CurrentTable.Owner,
                this.CurrentTable.Name,
                fk.Name);
            builder.AppendLine();
            builder.Append("  FOREIGN KEY ");

            string result = this.SerializeTableReference(fk.TableReference);
            builder.Append(result);

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="tr">The reference to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeTableReference(TableReference tr)
        {
            string comma = string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            foreach (ColumnReference cr in tr.References)
            {
                builder.Append(comma);
                builder.Append(cr.SourceName);
                comma = ", ";
            }

            builder.Append(")");
            builder.AppendLine();
            builder.AppendFormat(
                "  REFERENCES [{0}].[{1}] (",
                tr.TargetOwner,
                tr.TargetName);

            comma = string.Empty;
            foreach (ColumnReference cr in tr.References)
            {
                builder.Append(comma);
                builder.Append(cr.TargetName);
                comma = ", ";
            }

            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="ix">The index to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeIndex(Index ix)
        {
            StringBuilder builder = new StringBuilder();
            string result = null;
            if (ix.IsUniqueConstraint == true)
            {
                result = this.SerializeUniqueConstraint(ix);
            }
            else if (ix.IsPrimaryKey == true)
            {
                result = this.SerializePrimaryKey(ix);
            }
            else
            {
                result = this.SerializeCommonIndex(ix);
            }

            if (string.IsNullOrEmpty(result) == false)
            {
                builder.Append(result);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="ix">The index to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeUniqueConstraint(Index ix)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(
                "IF OBJECT_ID('[{0}].[{1}]') IS NULL",
                ix.Owner,
                ix.Name);

            builder.AppendLine();
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "  ALTER TABLE [{0}].[{1}] ADD CONSTRAINT {2} UNIQUE {3} ",
                this.CurrentTable.Owner,
                this.CurrentTable.Name,
                ix.Name,
                ix.Type);

            builder.Append(this.SerializePartition(ix.Partition));

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="ix">The index to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializePrimaryKey(Index ix)
        {
            StringBuilder builder = new StringBuilder();
            string result = this.SerializePartition(ix.Partition);
            if (string.IsNullOrEmpty(result) == true)
            {
                return null;
            }

            if (this.CurrentTable.Name[0] != '#')
            {
                builder.AppendFormat(
                    "IF OBJECT_ID('[{0}].[{1}]') IS NULL",
                    ix.Owner,
                    ix.Name);

                builder.AppendLine();
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "  ALTER TABLE [{0}].[{1}] ADD CONSTRAINT {2} PRIMARY KEY {3} ",
                    this.CurrentTable.Owner,
                    this.CurrentTable.Name,
                    ix.Name,
                    ix.Type);

                builder.Append(result);
            }
            else
            {
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "PRIMARY KEY CLUSTERED {0}",
                    result);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="ix">The index to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeCommonIndex(Index ix)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('[{0}].[{1}]') AND name = N'{2}')",
                this.CurrentTable.Owner,
                this.CurrentTable.Name,
                ix.Name);

            builder.AppendLine();
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "  CREATE {4}{0} INDEX {1} ON [{2}].[{3}] ",
                ix.Type,
                ix.Name,
                this.CurrentTable.Owner,
                this.CurrentTable.Name,
                ix.IsUnique ? "UNIQUE " : string.Empty);

            builder.Append(this.SerializePartition(ix.Partition));

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="partition">The partition to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializePartition(Partition partition)
        {
            SortedDictionary<int, IndexColumn> keys = new SortedDictionary<int, IndexColumn>();
            foreach (IndexColumn column in partition.Columns)
            {
                if (column.IsIncludedColumn == false)
                {
                    keys.Add(column.KeyOrdinal, column);
                }
            }

            string comma = string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            foreach (int key in keys.Keys)
            {
                IndexColumn column = keys[key];
                Column c = this.CurrentTable.GetColumn(column.ColumnName);
                string result = this.SerializeIndexColumn(column);

                if (column.IsIncludedColumn == false)
                {
                    builder.Append(comma);
                    builder.Append(result);
                    comma = ", ";
                }
            }

            if (builder.Length == 1)
            {
                return null;
            }

            builder.Append(")");
            comma = "\r\n  INCLUDE (";
            foreach (IndexColumn column in partition.Columns)
            {
                if (column.IsIncludedColumn == true)
                {
                    builder.Append(comma);
                    builder.Append(this.SerializeIndexColumn(column));
                    comma = ", ";
                }
            }

            if (builder.ToString().Contains("  INCLUDE (") == true)
            {
                builder.Append(")");
            }

            builder.Append(this.GetPartitionString(partition));

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="column">The column to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeIndexColumn(IndexColumn column)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            builder.Append(column.ColumnName);
            builder.Append("]");

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="check">The constraint to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeCheck(CheckConstraint check)
        {
            StringBuilder builder = new StringBuilder();
            if (string.IsNullOrEmpty(check.Value) == false)
            {
                builder.AppendFormat("CHECK {0}", check.Value);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the instance.
        /// </summary>
        /// <param name="owner">The schema to serialize.</param>
        /// <returns>The serialized instance.</returns>
        private string SerializeSchema(Owner owner)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("-----------------------------------");
            builder.AppendFormat("if not exists (select * from sys.schemas where name = '{0}')\n", owner.Name);
            builder.AppendFormat("   EXEC('CREATE SCHEMA [{0}]')\n", owner.Name);
            builder.Append(";");
            builder.AppendLine("-----------------------------------");
            return builder.ToString();
        }

        /// <summary>
        /// Gets the partition string.
        /// </summary>
        /// <param name="partition">The partition to inspect.</param>
        /// <returns>The partition string.</returns>
        private string GetPartitionString(Partition partition)
        {
            StringBuilder builder = new StringBuilder();
            if (string.IsNullOrEmpty(partition.Name) == false)
            {
                builder.Append(" ON ");
                builder.Append(partition.Name);

                IEnumerable<IndexColumn> cols = partition.Columns.Where(p => p.PartitionOrdinal > 0);
                builder.Append("(");
                string comma = string.Empty;
                foreach (IndexColumn ic in cols)
                {
                    builder.Append(comma);
                    builder.Append(ic.ColumnName);
                    comma = ", ";
                }

                builder.Append(")");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Prepares the identity.
        /// </summary>
        /// <param name="column">The column to inspect.</param>
        /// <returns>The prepared identity.</returns>
        private string PrepareIdentity(Column column)
        {
            if (column.Seed.HasValue == false)
            {
                column.Seed = 1;
            }

            if (column.Increment.HasValue == false)
            {
                column.Increment = 1;
            }

            return string.Format(CultureInfo.InvariantCulture, "IDENTITY({0}, {1})", column.Seed, column.Increment);
        }

        /// <summary>
        /// Prepares the length.
        /// </summary>
        /// <param name="column">The column to inspect.</param>
        /// <returns>The prepared length.</returns>
        private string PrepareLength(Column column)
        {
            switch (column.TypeName)
            {
                case "char":
                case "varchar":
                case "varbinary":
                    return "(" + (column.MaxLength == -1 ? "max" : column.MaxLength.ToString(CultureInfo.InvariantCulture)) + ")";
                case "nchar":
                case "nvarchar":
                    return "(" + (column.MaxLength == -1 ? "max" : column.MaxLength.ToString(CultureInfo.InvariantCulture)) + ")";
                case "numeric":
                case "decimal":
                    return "(" + column.Precision + "," + column.Scale + ")";
                case "datetime2":
                case "datetimeoffset":
                case "time":
                    return "(" + column.Precision + ")";
                default:
                    return string.Empty;
            }
        }
    }
}
