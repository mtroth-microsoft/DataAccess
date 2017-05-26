// -----------------------------------------------------------------------
// <copyright file="SchemaGenerator.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.OData.Edm;
    using Config = Configuration;

    /// <summary>
    /// Class to generate schema from a metadata document.
    /// </summary>
    public sealed class SchemaGenerator
    {
        /// <summary>
        /// Gets the list of tables.
        /// </summary>
        internal List<Table> Tables
        {
            get;
            private set;
        }

        /// <summary>
        /// Generate the tables, given a metadata document.
        /// </summary>
        /// <param name="metadata">The metadata document.</param>
        public void Generate(XElement metadata)
        {
            IEdmModel model = Microsoft.OData.Edm.Csdl.CsdlReader.Parse(metadata.CreateReader());
            this.Generate(model);
        }

        /// <summary>
        /// Generate the tables, given a model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Generate(IEdmModel model)
        {
            this.Tables = new List<Table>();
            foreach (IEdmSchemaElement element in model.SchemaElements)
            {
                IEdmEntityType entityType = element as IEdmEntityType;
                if (entityType != null)
                {
                    Table table = new Table(element.Name, element.Namespace);
                    ConfigureTypeHierarchy(entityType, table);
                    Unpack(entityType, table);
                    this.Tables.Add(table);
                }
            }
        }

        /// <summary>
        /// Configure the type hierarchy, using TPT method.
        /// </summary>
        /// <param name="entityType">The type to inspect.</param>
        /// <param name="table">The table to append.</param>
        private static void ConfigureTypeHierarchy(IEdmEntityType entityType, Table table)
        {
            List<IEdmEntityType> types = new List<IEdmEntityType>();
            IEnumerable<IEdmStructuralProperty> keys = null;
            IEdmEntityType type = entityType;
            while (type != null)
            {
                keys = type.DeclaredKey;
                if (keys != null && keys.Count() > 0)
                {
                    Index pk = new Index() { IsPrimaryKey = true, Name = string.Format("PK_{0}", table.Name) };
                    pk.Partition = new Partition();
                    pk.Owner = table.Owner;
                    foreach (IEdmStructuralProperty key in keys)
                    {
                        Unpack(key, table);
                        IndexColumn ic = new IndexColumn();
                        ic.ColumnName = key.Name;
                        pk.Partition.Columns.Add(ic);
                    }

                    table.AddIndex(pk);
                }

                types.Add(type);
                type = type.BaseEntityType();
            }

            foreach (IEdmEntityType tableType in types)
            {
                if (tableType.FullName() != entityType.FullName())
                {
                    ForeignKey fk = new ForeignKey();
                    fk.Name = string.Format("FK_{0}_{1}", table.Name, tableType.Name);
                    fk.Owner = table.Owner;
                    fk.TableReference = new TableReference()
                    {
                        TargetName = tableType.Name,
                        TargetOwner = tableType.Namespace
                    };
                    for (int i = 0; i < keys.Count(); i++)
                    {
                        ColumnReference cr = new ColumnReference();
                        cr.SourceName = keys.ElementAt(i).Name;
                        cr.TargetName = keys.ElementAt(i).Name;
                        fk.TableReference.References.Add(cr);
                    }

                    table.AddForeignKey(fk);
                }
            }
        }

        /// <summary>
        /// Unpack the objects in the given entity type.
        /// </summary>
        /// <param name="entityType">The entity type to inspect.</param>
        /// <param name="table">The table to populate.</param>
        private static void Unpack(IEdmEntityType entityType, Table table)
        {
            foreach (IEdmProperty p in entityType.DeclaredProperties)
            {
                if (p.PropertyKind != EdmPropertyKind.Navigation)
                {
                    if (p.Type.IsComplex() == true)
                    {
                        UnpackComplexType(p, table);
                    }
                    else if (p.Type.IsEnum() == true)
                    {
                        UnpackEnumType(p, table);
                    }
                    else if (p.Type.IsCollection() == true)
                    {
                        UnpackCollection(p, table);
                    }
                    else
                    {
                        Unpack(p, table);
                    }
                }
                else
                {
                    UnpackNavigation(p as IEdmNavigationProperty, table);
                }
            }
        }

        /// <summary>
        /// Unpack an enumeration property.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <param name="table">The table to populate.</param>
        private static void UnpackEnumType(IEdmProperty property, Table table)
        {
            Config.Property p = new Config.Property();
            p.Name = property.Name;
            p.Nullable = property.Type.IsNullable;
            p.Type = Config.DataType.@int;
            Column column = new Column(p);
            table.AddColumn(column);
        }

        /// <summary>
        /// Unpack a complex type.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <param name="table">The table to populate.</param>
        private static void UnpackComplexType(IEdmProperty property, Table table)
        {
            IEdmComplexType complexType = property.Type.ToStructuredType() as IEdmComplexType;
            foreach (IEdmProperty p in complexType.DeclaredProperties)
            {
                if (p.Type.IsComplex() == true)
                {
                    UnpackComplexType(p, table);
                }
                else if (p.Type.IsEnum() == true)
                {
                    UnpackEnumType(p, table);
                }
                else if (p.Type.IsCollection() == true)
                {
                    UnpackCollection(p, table);
                }
                else
                {
                    Unpack(p, table);
                }
            }
        }

        /// <summary>
        /// Unpack a collection property.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <param name="table">The table to populate.</param>
        private static void UnpackCollection(IEdmProperty property, Table table)
        {
            Config.Property p = new Config.Property();
            p.Name = property.Name;
            p.Nullable = true;
            p.Type = Config.DataType.@string;
            p.Size = -1;
            Column column = new Column(p);
            table.AddColumn(column);
        }

        /// <summary>
        /// Unpack a navigation property.
        /// </summary>
        /// <param name="np">The property to inspect.</param>
        /// <param name="table">The table to populate.</param>
        private static void UnpackNavigation(IEdmNavigationProperty np, Table table)
        {
            ForeignKey fk = new ForeignKey();
            fk.Name = string.Format("FK_{0}_{1}", table.Name, np.ToEntityType().Name);
            fk.Owner = table.Owner;
            fk.TableReference = new TableReference()
            {
                TargetName = np.ToEntityType().Name,
                TargetOwner = np.ToEntityType().Namespace
            };

            IEnumerable<IEdmStructuralProperty> deps = np.DependentProperties();
            IEnumerable<IEdmStructuralProperty> pris = np.PrincipalProperties();
            if (deps != null && pris != null)
            {
                foreach (IEdmStructuralProperty sp in deps)
                {
                    Unpack(sp, table);
                }

                for (int i = 0; i < deps.Count(); i++)
                {
                    ColumnReference cr = new ColumnReference();
                    cr.SourceName = deps.ElementAt(i).Name;
                    cr.TargetName = pris.ElementAt(i).Name;
                    fk.TableReference.References.Add(cr);
                }

                table.AddForeignKey(fk);
            }
            else
            {
                if (np.TargetMultiplicity() != EdmMultiplicity.Many)
                {
                    IEnumerable<IEdmStructuralProperty> keys = np.ToEntityType().Key();
                    foreach (IEdmStructuralProperty key in keys)
                    {
                        Config.Property p = new Config.Property();
                        p.Name = np.Name + "_" + key.Name;
                        p.Nullable = true;
                        p.Type = SwitchType(key.Type);
                        p.Size = p.Type == Config.DataType.@string ? GetStringLength(key) : (int?)null;
                        Column column = new Column(p);
                        table.AddColumn(column);

                        ColumnReference cr = new ColumnReference();
                        cr.SourceName = p.Name;
                        cr.TargetName = key.Name;
                        fk.TableReference.References.Add(cr);
                    }

                    table.AddForeignKey(fk);
                }
            }
        }

        /// <summary>
        /// Unpack a property.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <param name="table">The table to populate.</param>
        private static void Unpack(IEdmProperty property, Table table)
        {
            Config.Property p = new Config.Property();
            p.Name = property.Name;
            p.Nullable = property.Type.IsNullable;
            p.Type = SwitchType(property.Type);
            p.Size = p.Type == Config.DataType.@string ? GetStringLength(property) : (int?)null;
            Column column = new Column(p);
            table.AddColumn(column);
        }

        /// <summary>
        /// Gets the length to associate with the provided property.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <returns>The string length.</returns>
        private static int? GetStringLength(IEdmProperty property)
        {
            return ((IEdmStringTypeReference)property.Type).MaxLength ?? 128;
        }

        /// <summary>
        /// Switch the type from edm type reference to data type.
        /// </summary>
        /// <param name="type">The edm type reference to switch.</param>
        /// <returns>The corresponding data type.</returns>
        private static Config.DataType SwitchType(IEdmTypeReference type)
        {
            switch (type.FullName())
            {
                case "Edm.String": return Config.DataType.@string;
                case "Edm.Int32": return Config.DataType.@int;
                case "Edm.Boolean": return Config.DataType.@bool;
                case "Edm.DateTimeOffset": return Config.DataType.datetimeoffset;
                case "Edm.Int64": return Config.DataType.@long;
                default:
                    throw new IndexOutOfRangeException(type.FullName());
            }
        }
    }
}
