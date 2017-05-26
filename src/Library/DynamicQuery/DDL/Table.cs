// -----------------------------------------------------------------------
// <copyright file="Table.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Config = Configuration;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class Table
    {
        /// <summary>
        /// The list of collections.
        /// </summary>
        private List<SchemaCollection> collections = new List<SchemaCollection>();

        /// <summary>
        /// Initializes a new instance of the Table class.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="ns">The namespace for the table.</param>
        public Table(string name, string ns)
        {
            this.Name = name;
            this.Owner = ns;
            this.collections.Add(new Columns());
            this.collections.Add(new Indices());
            this.collections.Add(new ForeignKeys());
        }

        /// <summary>
        /// Initializes a new instance of the Table class.
        /// </summary>
        /// <param name="feed">The feed to use to create the class.</param>
        public Table(Config.Feed feed)
        {
            this.Owner = feed.Namespace;
            this.Name = feed.Name;
            this.collections.Add(new Columns(feed.Properties));

            if (feed.Keys.Count > 0)
            {
                Indices indices = new Indices();
                List<Config.Property> keys = feed.Properties
                    .Where(p => (feed.Keys.Select(q => q.Name)).Contains(p.Name))
                    .ToList();
                indices.AddPrimaryKey(keys, "PK_" + this.Name, this.Owner, "CLUSTERED", null);
                this.collections.Add(indices);
            }
        }

        /// <summary>
        /// Initializes a new instance of the Table class.
        /// </summary>
        /// <param name="dimension">The dimension to use to create the class.</param>
        public Table(Config.Dimension dimension)
        {
            this.Owner = dimension.Namespace;
            this.Name = dimension.Name;
            this.collections.Add(new Columns(dimension.Properties));

            if (dimension.Keys.Count > 0)
            {
                Indices indices = new Indices();
                List<Config.Property> keys = dimension.Properties
                    .Where(p => (dimension.Keys.Select(q => q.Name)).Contains(p.Name))
                    .ToList();
                indices.AddPrimaryKey(keys, "PK_" + this.Name, this.Owner, "CLUSTERED", null);
                this.collections.Add(indices);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Owner
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the table's schema collections.
        /// </summary>
        public ICollection<SchemaCollection> Collections
        {
            get
            {
                return this.collections.AsReadOnly();
            }
        }

        /// <summary>
        /// Add a column to the table.
        /// </summary>
        /// <param name="column">The column to add.</param>
        internal void AddColumn(Column column)
        {
            SchemaCollection collection = this.Collections.Where(p => p.Name == "Columns").FirstOrDefault();
            if (collection.Objects.Any(p => ((Column)p).Name == column.Name) == false)
            {
                collection.Objects.Add(column);
            }
        }

        /// <summary>
        /// Add a foreign key to the table.
        /// </summary>
        /// <param name="fk">The foreign key to add.</param>
        internal void AddForeignKey(ForeignKey fk)
        {
            SchemaCollection collection = this.Collections.Where(p => p.Name == "ForeignKeys").FirstOrDefault();
            if (collection.Objects.Any(p => ((ForeignKey)p).Name == fk.Name) == false)
            {
                collection.Objects.Add(fk);
            }
        }

        /// <summary>
        /// Add an index to the table.
        /// </summary>
        /// <param name="ix">The index to add.</param>
        internal void AddIndex(Index ix)
        {
            SchemaCollection collection = this.Collections.Where(p => p.Name == "Indices").FirstOrDefault();
            if (collection.Objects.Any(p => ((Index)p).Name == ix.Name) == false)
            {
                collection.Objects.Add(ix);
            }
        }

        /// <summary>
        /// Get the references for the table.
        /// </summary>
        /// <returns>The list of references.</returns>
        internal List<TableReference> GetReferences()
        {
            List<TableReference> references = new List<TableReference>();
            SchemaCollection collection = this.Collections.Where(p => p.Name == "ForeignKeys").FirstOrDefault();
            if (collection == null)
            {
                return references;
            }

            foreach (TabularObject to in collection.Objects)
            {
                ForeignKey fk = to as ForeignKey;
                if (fk.TableReference.TargetOwner == this.Owner)
                {
                    references.Add(fk.TableReference);
                }
            }

            return references;
        }

        /// <summary>
        /// Get the column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The matching column.</returns>
        internal Column GetColumn(string name)
        {
            SchemaCollection collection = this.Collections.Where(p => p.Name == "Columns").FirstOrDefault();
            if (collection == null)
            {
                return null;
            }

            foreach (TabularObject to in collection.Objects)
            {
                Column c = to as Column;
                if (c.Name == name)
                {
                    return c;
                }
            }

            return null;
        }

        /// <summary>
        /// Determine whether a column is a key column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>True if it is a key, otherwise false.</returns>
        internal bool IsKeyColumn(string name)
        {
            SchemaCollection collection = this.Collections.Where(p => p.Name == "Indices").FirstOrDefault();
            if (collection == null)
            {
                return false;
            }

            foreach (TabularObject to in collection.Objects)
            {
                Index ix = to as Index;
                if (ix.IsPrimaryKey == false)
                {
                    continue;
                }

                foreach (IndexColumn ixcol in ix.Partition.Columns)
                {
                    if (ixcol.ColumnName == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The class declaration.
        /// </summary>
        internal abstract class SchemaCollection
        {
            /// <summary>
            /// Initializes a new instance of the SchemaCollection class.
            /// </summary>
            /// <param name="name">The name of the collection.</param>
            protected SchemaCollection(string name)
            {
                this.Name = name;
                this.Objects = new List<TabularObject>();
            }

            /// <summary>
            /// Gets the name.
            /// </summary>
            public string Name
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the table.
            /// </summary>
            public Table Table
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the list of objects.
            /// </summary>
            public List<TabularObject> Objects
            {
                get;
                private set;
            }
        }

        /// <summary>
        /// The class declaration.
        /// </summary>
        internal class ForeignKeys : SchemaCollection
        {
            /// <summary>
            /// Initializes a new instance of the ForeignKeys class.
            /// </summary>
            public ForeignKeys()
                : base("ForeignKeys")
            {
            }
        }

        /// <summary>
        /// Class declaration.
        /// </summary>
        internal class Indices : SchemaCollection
        {
            /// <summary>
            /// Initializes a new instance of the Indices class.
            /// </summary>
            public Indices()
                : base("Indices")
            {
            }

            /// <summary>
            /// Add a primary key to the index collection.
            /// </summary>
            /// <param name="columns">The columns to include in the pk.</param>
            /// <param name="name">The name of the key.</param>
            /// <param name="schema">The schema of the key.</param>
            /// <param name="type">The type of the index.</param>
            /// <param name="partition">The name of the partition.</param>
            public void AddPrimaryKey(
                List<Config.Property> columns, 
                string name, 
                string schema, 
                string type, 
                string partition)
            {
                Index pk = new Index();
                pk.Name = name;
                pk.Owner = schema;
                pk.IsPrimaryKey = true;
                pk.Type = type;
                pk.Partition = new Partition();
                pk.Partition.Name = partition;
                int index = 0;
                foreach (Config.Property column in columns)
                {
                    pk.Partition.Columns.Add(new IndexColumn() { ColumnName = column.Name, KeyOrdinal = index++ });
                }

                this.Objects.Add(pk);
            }
        }

        /// <summary>
        /// Class declaration.
        /// </summary>
        internal class Columns : SchemaCollection
        {
            /// <summary>
            /// Initializes a new instance of the Columns class.
            /// </summary>
            public Columns()
                : base ("Columns")
            {
            }

            /// <summary>
            /// Initializes a new instance of the Columns class.
            /// </summary>
            /// <param name="properties">The properties to wrap.</param>
            public Columns(List<Config.Property> properties)
                : base("Columns")
            {
                foreach (Config.Property child in properties)
                {
                    Column col = new Column(child);
                    this.Objects.Add(col);
                }
            }
        }
    }
}
