// -----------------------------------------------------------------------
// <copyright file="TypeCache.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Class for storing type related data.
    /// </summary>
    public static class TypeCache
    {
        /// <summary>
        /// The list of endpoints.
        /// </summary>
        private static HashSet<string> endpoints = new HashSet<string>();

        /// <summary>
        /// Cache of property attributes.
        /// </summary>
        private static ConcurrentDictionary<PropertyInfo, List<Attribute>> attributes =
            new ConcurrentDictionary<PropertyInfo, List<Attribute>>();

        /// <summary>
        /// Cache of property names.
        /// </summary>
        private static ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> propertyNames =
            new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();

        /// <summary>
        /// Cache of type accessors.
        /// </summary>
        private static ConcurrentDictionary<Type, FastMember.TypeAccessor> typeAccessors =
            new ConcurrentDictionary<Type, FastMember.TypeAccessor>();

        /// <summary>
        /// List of overrides.
        /// </summary>
        private static Dictionary<PropertyInfo, Dictionary<string, string>> overrides = 
            new Dictionary<PropertyInfo, Dictionary<string, string>>();

        /// <summary>
        /// List of supplements.
        /// </summary>
        private static Dictionary<Type, HashSet<string>> supplements =
            new Dictionary<Type, HashSet<string>>();

        /// <summary>
        /// List of many to many properties.
        /// </summary>
        private static Dictionary<PropertyInfo, QueryTable> manyToManies =
            new Dictionary<PropertyInfo, QueryTable>();

        /// <summary>
        /// All modeled types in the current app domain.
        /// </summary>
        private static Dictionary<Type, IEnumerable<PropertyInfo>> typeMap = InventoryTypes();

        /// <summary>
        /// Initial configuration for a given type.
        /// </summary>
        /// <typeparam name="T">The type to configure.</typeparam>
        /// <returns>The type configuration.</returns>
        public static TypeConfiguration<T> Type<T>()
        {
            return new TypeConfiguration<T>();
        }

        /// <summary>
        /// Returns a value indicating whether the provided type is a navigational type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>Truf if the type is navigational, otherwise false.</returns>
        internal static bool IsNavigationalType(Type type)
        {
            return (type.IsClass && type != typeof(string) && type != typeof(byte[])) ||
                (type.IsInterface == true) ||
                (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string) && type != typeof(byte[]));
        }

        /// <summary>
        /// Set an endpoint url for the current process.
        /// </summary>
        /// <param name="url">The path to the endpoint.</param>
        internal static void SetEndpoint(string model)
        {
            if (endpoints.Contains(model) == false)
            {
                endpoints.Add(model);
            }
        }

        /// <summary>
        /// Set an override for a given property.
        /// </summary>
        /// <param name="property">The property to override.</param>
        /// <param name="left">The list of left hand columns in the container type of the property.</param>
        /// <param name="right">The list of right hand columns in the contained type of the property.</param>
        internal static void SetOverride(
            PropertyInfo property, 
            IEnumerable<string> left, 
            IEnumerable<string> right)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            for (int i = 0; i < left.Count(); i++)
            {
                map[left.ElementAt(i)] = right.ElementAt(i);
            }

            overrides[property] = map;
            Supplement(property.DeclaringType, left);
            Supplement(property.PropertyType, right);
        }

        /// <summary>
        /// Set an override for a given property.
        /// </summary>
        /// <param name="property">The property to override.</param>
        /// <param name="left">The list of left hand columns in the container type of the property.</param>
        /// <param name="right">The list of right hand columns in the contained type of the property.</param>
        internal static void SetOverride(
            PropertyInfo property,
            QueryTable table,
            IEnumerable<string> left,
            IEnumerable<string> right)
        {
            if (left.Count() > 1 || right.Count() > 1)
            {
                throw new InvalidOperationException("Only single column joins supported for many to many relations.");
            }

            Dictionary<string, string> map = new Dictionary<string, string>();
            for (int i = 0; i < left.Count(); i++)
            {
                map[left.ElementAt(i)] = right.ElementAt(i);
            }

            overrides[property] = map;
            manyToManies[property] = table;
        }

        /// <summary>
        /// Helper function to locate ambiguous references for foreign key discovery on one-many joins.
        /// </summary>
        /// <param name="containerType">The hosting type.</param>
        /// <param name="propertyName">The name of the collection property.</param>
        /// <returns>The column name mappings.</returns>
        internal static Dictionary<string, string> GetOverride(
            Type containerType,
            string propertyName,
            out QueryTable intermediateTable)
        {
            Dictionary<string, string> statements = null;
            statements = LocateOverride(containerType, propertyName, out intermediateTable);
            if (statements == null || statements.Count == 0)
            {
                Type propertyType = LocatePropertyType(containerType, propertyName);
                CompositeNode parent = new CompositeNode(null, string.Empty, containerType, false);
                CompositeNode child = new CompositeNode(parent, propertyName, propertyType, false);
                statements = AttemptResolveJoin(parent, child);
            }

            return statements;
        }

        /// <summary>
        /// Helper function to locate ambiguous references for foreign key discovery on one-many joins.
        /// </summary>
        /// <param name="parent">The hosting node.</param>
        /// <param name="child">The hosted node.</param>
        /// <param name="intermediateTable">The intermediate table, if the join is a manytomany join.</param>
        /// <returns>The column name mappings.</returns>
        internal static Dictionary<string, string> LocateJoin(
            CompositeNode parent,
            CompositeNode child,
            out QueryTable intermediateTable)
        {
            Dictionary<string, string> statements = null;
            if (child.Reverse == true)
            {
                Dictionary<string, string> reverse = LocateOverride(child.ElementType, child.Path, out intermediateTable);
                if (reverse != null)
                {
                    statements = new Dictionary<string, string>();
                    foreach (string key in reverse.Keys)
                    {
                        statements[reverse[key]] = key;
                    }
                }
            }
            else
            {
                statements = LocateOverride(parent.ElementType, child.Path, out intermediateTable);
            }

            if (statements == null || statements.Count == 0)
            {
                statements = AttemptResolveJoin(parent, child);
            }

            return statements;
        }

        /// <summary>
        /// Extract the inner type from a Nullable or ICollection type.
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        internal static Type NormalizeType(Type propertyType)
        {
            Type[] types = propertyType.GetGenericArguments();
            if (types.Length == 1)
            {
                return types.Single();
            }

            return propertyType;
        }

        /// <summary>
        /// Locate the property for the given type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="propertyName">The full path of the property to locate.</param>
        /// <returns>The property info for the provided path.</returns>
        internal static PropertyInfo LocateProperty(Type type, string propertyName)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (propertyName.Contains('/') == false)
            {
                PropertyInfo pi = type.GetProperty(propertyName);
                if (pi != null)
                {
                    return pi;
                }
            }
            else
            {
                PropertyInfo pi = null;
                Type test = type;
                string[] steps = propertyName.Split('/');
                foreach (string step in steps)
                {
                    pi = test.GetProperty(step);
                    if (pi != null)
                    {
                        test = NormalizeType(pi.PropertyType);
                    }
                    else
                    {
                        return null;
                    }
                }

                return pi;
            }

            return null;
        }

        /// <summary>
        /// Locate the type of the given property name.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="propertyName">The name of the property to search for.</param>
        /// <returns>The property's discovered type.</returns>
        internal static Type LocatePropertyType(Type type, string propertyName)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (propertyName.Contains('/') == false)
            {
                PropertyInfo pi = type.GetProperty(propertyName);
                if (pi != null)
                {
                    return pi.PropertyType;
                }
            }
            else
            {
                Type test = type;
                string[] steps = propertyName.Split('/');
                foreach (string step in steps)
                {
                    PropertyInfo pi = test.GetProperty(step);
                    if (pi != null)
                    {
                        test = pi.PropertyType;
                    }
                    else
                    {
                        return null;
                    }
                }

                return test;
            }

            return null;
        }

        /// <summary>
        /// Extract the names of the table and its schema.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="name">The table name.</param>
        /// <param name="schema">The schema name.</param>
        internal static void ExtractTableName(Type type, out string name, out string schema)
        {
            schema = "dbo";
            name = null;

            TableAttribute attribute = type.GetCustomAttribute<TableAttribute>(false);
            if (attribute != null)
            {
                if (string.IsNullOrEmpty(attribute.Schema) == false)
                {
                    schema = attribute.Schema;
                    name = attribute.Name;
                }
                else if (attribute.Name.Contains('.') == true)
                {
                    schema = attribute.Name.Substring(0, attribute.Name.IndexOf('.'));
                    name = attribute.Name.Substring(attribute.Name.IndexOf('.') + 1);
                }
                else
                {
                    name = attribute.Name;
                }
            }
        }

        /// <summary>
        /// Check to see if the grouping is a legal column in the data.
        /// </summary>
        /// <param name="group">The grouping to check.</param>
        /// <param name="type">The type to check against.</param>
        internal static void CheckIsLegalColumn(string group, Type type)
        {
            if (group != "*" && group != "$it" && group != "$TypeName")
            {
                List<PropertyInfo> properties = GetProperties(type);
                if (properties.Any(p => p.Name.Equals(group, StringComparison.OrdinalIgnoreCase)) == false &&
                    (supplements.ContainsKey(type) == false || supplements[type].Contains(group) == false))
                {
                    throw new ArgumentException(group + " is not a legal property on type " + type.Name);
                }
            }
        }

        /// <summary>
        /// Make sure all keys are present in the query.
        /// </summary>
        /// <param name="source">The query source with which to associate the columns.</param>
        /// <param name="type">The type of the underlying object.</param>
        /// <returns>The list of columns.</returns>
        internal static List<QueryColumn> CreateColumns(QuerySource source, Type type)
        {
            IEnumerable<PropertyInfo> properties = null;
            if (source is UnionQuery)
            {
                IEnumerable<Type> derived = (source as UnionQuery).Queries.Select(p => p.Type);
                properties = GetProperties(derived.ToArray());
            }
            else
            {
                properties = GetProperties(type);
            }

            List<QueryColumn> columns = new List<QueryColumn>();
            foreach (PropertyInfo p in properties)
            {
                KeyAttribute attribute = GetAttribute<KeyAttribute>(p);
                ColumnAttribute column = GetAttribute<ColumnAttribute>(p);
                IgnoreAttribute ignore = GetAttribute<IgnoreAttribute>(p);
                MaxLengthAttribute max = GetAttribute<MaxLengthAttribute>(p);
                RequiredAttribute required = GetAttribute<RequiredAttribute>(p);
                DatabaseGeneratedAttribute computed = GetAttribute<DatabaseGeneratedAttribute>(p);
                ConcurrencyCheckAttribute cc = GetAttribute<ConcurrencyCheckAttribute>(p);
                TimestampAttribute ts = GetAttribute<TimestampAttribute>(p);
                InsertedTimeAttribute it = GetAttribute<InsertedTimeAttribute>(p);
                UpdatedTimeAttribute ut = GetAttribute<UpdatedTimeAttribute>(p);
                ChangedByAttribute cb = GetAttribute<ChangedByAttribute>(p);
                if (ignore != null)
                {
                    continue;
                }

                bool nullable = false;
                Type propertyType = p.PropertyType;
                if (p.PropertyType.Name.StartsWith("Nullable") == true)
                {
                    propertyType = p.PropertyType.GetGenericArguments()[0];
                    nullable = true;
                }
                else if (required == null && (propertyType == typeof(string) || 
                    propertyType == typeof(byte[]) || 
                    IsNavigationalType(propertyType)))
                {
                    nullable = true;
                }

                QueryColumn c = new QueryColumn();
                c.Source = source;
                c.Alias = p.Name;
                c.Name = p.Name;
                c.ElementType = propertyType;
                c.DeclaringType = GetDeclaringType(type, p.DeclaringType);
                c.Nullable = nullable;
                c.Computed = computed != null ? computed.DatabaseGeneratedOption : DatabaseGeneratedOption.None;
                c.IsInsertedTime = it != null;
                c.IsUpdatedTime = ut != null;
                c.IsChangedBy = cb != null;
                c.ConcurrencyCheck = cc != null || ts != null;
                c.Size = max == null ? -1 : max.Length;
                columns.Add(c);
                if (column != null && string.IsNullOrEmpty(column.Name) == false)
                {
                    c.Name = column.Name;
                }

                if (attribute != null)
                {
                    c.IsKeyColumn = true;
                    c.DefaultValue = CreateDefaultValue(p.PropertyType);
                }
            }

            AppendSupplementalColumns(columns, source, type);

            return columns;
        }

        /// <summary>
        /// Set a value for a given property.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="property">The property name to set.</param>
        /// <param name="instance">The instance to set.</param>
        /// <param name="value">The value to set.</param>
        internal static void SetValue(Type type, string property, object instance, object value)
        {
            FastMember.TypeAccessor ta = GetTypeAccessor(type);
            FastMember.Member member = ta.GetMembers().Where(p => p.Name == property).SingleOrDefault();
            if (value != null && member.Type != value.GetType())
            {
                if (member.Type == typeof(DateTimeOffset) && value.GetType() == typeof(DateTime))
                {
                    DateTime v = (DateTime)value;
                    ta[instance, property] = (DateTimeOffset)v;
                }
                else if (member.Type == typeof(byte[]) && value.GetType() == typeof(DateTime))
                {
                    ta[instance, property] = null;
                }
                else if (member.Type.IsEnum == true)
                {
                    ta[instance, property] = Enum.ToObject(member.Type, value);
                }
                else if (IsCollection(member.Type) == true)
                {
                    object collection = HandleEnumeration((IEnumerable)value, type.GetProperty(property).PropertyType, null);
                    ta[instance, property] = collection;
                }
                else
                {
                    ta[instance, property] = value;
                }
            }
            else
            {
                ta[instance, property] = value;
            }
        }

        /// <summary>
        /// Get a value for a given property.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="property">The property name to get.</param>
        /// <param name="instance">The instance to get from.</param>
        /// <returns>The retrieved value.</returns>
        internal static object GetValue(Type type, string property, object instance)
        {
            FastMember.TypeAccessor ta = GetTypeAccessor(type);
            return ta[instance, property];
        }

        /// <summary>
        /// Maps the rows in a datable to a list of objects
        /// </summary>
        /// <param name="type">The type of the object</param>
        /// <param name="row">The row containing the instance data.</param>
        /// <returns>The populated object.</returns>
        internal static object ToObject(Type type, DataRow row)
        {
            Type local = type;
            if (row.Table.Columns.Contains("$TypeName") == true)
            {
                string localName = row["$TypeName"].ToString();
                local = LocateType(localName);
            }

            // collect the columns
            HashSet<string> columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn col in row.Table.Columns)
            {
                columns.Add(col.ColumnName);
            }

            Dictionary<string, PropertyInfo> properties = GetColumnNameToPropertyMap(local, columns);
            object instance = Activator.CreateInstance(local);
            foreach (string prop in properties.Keys)
            {
                object val = row[prop];
                if (val != DBNull.Value && val != null)
                {
                    SetValue(local, properties[prop].Name, instance, val);
                }
            }

            PropertyInfo dynamic = local.GetProperty("DynamicProperties");
            if (dynamic != null)
            {
                Dictionary<string, object> propertyBag = new Dictionary<string, object>();
                IEnumerable<string> dcols = columns.Where(p => properties.ContainsKey(p) == false && p[0] != '$');
                foreach (string dcol in dcols)
                {
                    object val = row[dcol];
                    if (val != DBNull.Value && val != null)
                    {
                        propertyBag[dcol] = val;
                    }
                }

                SetValue(local, dynamic.Name, instance, propertyBag);
            }

            return instance;
        }

        /// <summary>
        /// Populate the data for the given results.
        /// </summary>
        /// <param name="propertyName">The name of the property to populate.</param>
        /// <param name="context">The composite node for the containing type.</param>
        /// <param name="result">The result with raw data to move into the instance.</param>
        internal static void PopulateData(string propertyName, CompositeNode context, Result result)
        {
            object instance = result.Member;
            PropertyInfo pi = context.ElementType.GetProperty(propertyName);
            CompositeNode current = context.Nodes.Single(p => p.Path == propertyName);
            if (current.IsCollection)
            {
                IEnumerable<object> values = result.Nodes.Where(p => p.Path == propertyName).Select(p => p.Member);
                object retval = HandleEnumeration(values, pi.PropertyType, current);
                SetValue(context.ElementType, propertyName, instance, retval);
            }
            else
            {
                object value = result.Nodes.Single(p => p.Path == propertyName).Member;
                SetValue(context.ElementType, propertyName, instance, value);
            }
        }

        /// <summary>
        /// Extract the type sequence.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="entitySetType"></param>
        /// <returns>The sequence of types/steps.</returns>
        internal static List<PathStep> ExtractTypeSequence(Uri uri, Type entitySetType)
        {
            List<PathStep> map = new List<PathStep>();
            string path = Uri.UnescapeDataString(uri.AbsolutePath.Substring(1));
            foreach (string endpoint in endpoints)
            {
                int pos = Uri.UnescapeDataString(uri.AbsolutePath).IndexOf(endpoint);
                if (pos > 0)
                {
                    path = Uri.UnescapeDataString(uri.AbsolutePath).Substring(pos + endpoint.Length + 1);
                    break;
                }
            }

            Type test = entitySetType;
            string[] steps = path.Split('/');
            for (int i = 0; i < steps.Length; i++)
            {
                string args = string.Empty;
                string step = steps[i];
                int parenpos = step.IndexOf('(') + 1; // Regex regex = new Regex(@"\(([^\)]+)\)");
                if (parenpos > 0)
                {
                    int endpos = step.LastIndexOf(')');
                    args = endpos > 0 ? step.Substring(parenpos, endpos - parenpos) : string.Empty;
                    step = step.Substring(0, parenpos - 1);
                }

                bool isTypeConstraint = false;
                test = i == 0 ? entitySetType : LocatePropertyType(test, step);
                if (typeof(IEnumerable).IsAssignableFrom(test) == true && test != typeof(string) && test != typeof(byte[]))
                {
                    test = test.GenericTypeArguments[0];
                }
                else if (test == null)
                {
                    test = LocateType(step);
                    isTypeConstraint = true;
                }

                if (test != null && test.IsClass == true && test != typeof(string) && test != typeof(byte[]))
                {
                    map.Add(new PathStep() { Name = step, Type = test, IsTypeConstraint = isTypeConstraint, Args = args });
                }
            }

            return map;
        }

        /// <summary>
        /// Use reflection to generate the correct builder.
        /// </summary>
        /// <param name="rootType">The root type for the builder.</param>
        /// <param name="settings">The settings to pass to the constructor.</param>
        /// <returns>The generated query buidler.</returns>
        internal static IQueryBuilder ReflectCorrectBuilder(Type rootType, QueryBuilderSettings settings)
        {
            object[] parameters = new object[] { settings };
            Type[] types = new Type[] { typeof(QueryBuilderSettings) };
            Type queryBuilderRaw = typeof(QueryBuilder<>);
            Type queryBuilderOfT = queryBuilderRaw.MakeGenericType(new Type[] { rootType });
            ConstructorInfo ci = queryBuilderOfT.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                null,
                types,
                null);

            IQueryBuilder qbuilder = ci.Invoke(parameters) as IQueryBuilder;

            return qbuilder;
        }

        /// <summary>
        /// Get the attribute of the specified type for the given property.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type.</typeparam>
        /// <param name="property">The specified property.</param>
        /// <returns>The correlative attribute.</returns>
        internal static TAttribute GetAttribute<TAttribute>(PropertyInfo property)
        {
            List<Attribute> value;
            if (attributes.TryGetValue(property, out value) == false)
            {
                value = attributes.GetOrAdd(
                    property,
                    property.GetCustomAttributes(true).Cast<Attribute>().ToList());
            }

            TAttribute attribute = value.OfType<TAttribute>().SingleOrDefault();

            return attribute;
        }

        /// <summary>
        /// Get the column names to property map for a given type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="columnNames">The list of column names in the data.</param>
        /// <returns>The map of column names to properties.</returns>
        internal static Dictionary<string, PropertyInfo> GetColumnNameToPropertyMap(Type type, HashSet<string> columnNames)
        {
            Dictionary<string, PropertyInfo> properties;
            if (propertyNames.TryGetValue(type, out properties) == false)
            {
                properties = propertyNames.GetOrAdd(
                    type,
                    (p) =>
                    {
                        Dictionary<string, PropertyInfo> map = new Dictionary<string, PropertyInfo>(columnNames.Count);
                        foreach (PropertyInfo prop in GetProperties(p))
                        {
                            ColumnAttribute ca = GetAttribute<ColumnAttribute>(prop);
                            if (columnNames.Contains(prop.Name) == true)
                            {
                                map.Add(prop.Name, prop);
                            }
                            else if (ca != null && string.IsNullOrEmpty(ca.Name) == false)
                            {
                                map.Add(ca.Name, prop);
                            }
                            else
                            {
                                map.Add(prop.Name, prop);
                            }
                        }

                        return map;
                    });
            }

            Dictionary<string, PropertyInfo> results = new Dictionary<string, PropertyInfo>();
            foreach (string key in properties.Keys)
            {
                if (columnNames.Contains(key) == true)
                {
                    results.Add(key, properties[key]);
                }
            }

            return results;
        }

        /// <summary>
        /// Get the key columns properties for a given type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>The list of key names.</returns>
        internal static List<string> GetKeys(Type type)
        {
            List<string> keyNames = new List<string>();
            IEnumerable<PropertyInfo> cols = GetProperties(type);
            foreach (PropertyInfo pi in cols)
            {
                KeyAttribute ka = GetAttribute<KeyAttribute>(pi);
                if (ka != null)
                {
                    keyNames.Add(pi.Name);
                }
            }

            return keyNames;
        }

        /// <summary>
        /// Locate the named type.
        /// </summary>
        /// <param name="type">The type name to locate.</param>
        /// <returns>The discovered type.</returns>
        internal static Type LocateType(string type)
        {
            IEnumerable<Type> types = typeMap.Keys;
            Type discovered = types.FirstOrDefault(p => p.Name.Equals(type, StringComparison.OrdinalIgnoreCase) ||
                                                        p.FullName.Equals(type, StringComparison.OrdinalIgnoreCase));

            return discovered;
        }

        /// <summary>
        /// Returns the collection of derived types.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>The collection of derived types.</returns>
        internal static IEnumerable<Type> ReadDerivedTypes(Type type)
        {
            return typeMap.Keys.Where(p => p.Assembly == type.Assembly && type.IsAssignableFrom(p) && p != type);
        }

        /// <summary>
        /// Handle a select expand object.
        /// </summary>
        /// <param name="item">The select expand wrapped object.</param>
        /// <returns>The unwrapped object.</returns>
        internal static object HandleSelectExpand(object item)
        {
            object instance = item;
            if (item != null && item.GetType().Name.Contains("SelectAll") == true)
            {
                instance = HandleSelectAll(item);
            }
            else if (item != null && item.GetType().Name.Contains("SelectSome") == true)
            {
                Type entityType = item.GetType().GetGenericArguments()[0];
                instance = HandleSelectSome(item, entityType);
            }

            return instance;
        }

        /// <summary>
        /// Handle a select all object.
        /// </summary>
        /// <param name="item">The select wrapped object.</param>
        /// <returns>The unwrapped object.</returns>
        private static object HandleSelectAll(object item)
        {
            object instance = TypeCache.GetValue(item.GetType(), "Instance", item);

            return instance;
        }

        /// <summary>
        /// Handle a select some object.
        /// </summary>
        /// <param name="item">The select wrapped object.</param>
        /// <param name="entityType">The type of the wrapped object.</param>
        /// <returns>The unwrapped object.</returns>
        private static object HandleSelectSome(object item, Type entityType)
        {
            object p = Activator.CreateInstance(entityType);
            object container = GetValue(item.GetType(), "Container", item);
            while (container != null)
            {
                object name = GetValue(container.GetType(), "Name", container);
                object value = GetValue(container.GetType(), "Value", container);
                if (value != null)
                {
                    value = HandleSelectExpand(value);
                    SetValue(entityType, name.ToString(), p, value);
                }
                else
                {
                    object collection = GetValue(container.GetType(), "Collection", container);
                    collection = HandleEnumeration(collection as IEnumerable, entityType.GetProperty(name.ToString()).PropertyType, null);
                    SetValue(entityType, name.ToString(), p, collection);
                }

                if (container.GetType().Name.Contains("WithNext") == true)
                {
                    container = GetValue(container.GetType(), "Next", container);
                }
                else
                {
                    container = null;
                }
            }

            return p;
        }

        /// <summary>
        /// Handle an enumeration of members.
        /// </summary>
        /// <param name="collection">The collection to enumerate.</param>
        /// <param name="collectionType">The type of the collection.</param>
        /// <param name="node">The context node, if applicable.</param>
        /// <returns>The collection of type collectiontype wrapped as object.</returns>
        private static object HandleEnumeration(IEnumerable collection, Type collectionType, CompositeNode node)
        {
            List<object> values = new List<object>();
            foreach (object item in collection)
            {
                object member = HandleSelectExpand(item);
                values.Add(member);
            }

            MethodInfo method = typeof(TypeCache).GetMethod("CreateAndFillCollection", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo generic = method.MakeGenericMethod(collectionType, collectionType.GetGenericArguments()[0]);
            object retval = generic.Invoke(null, new object[] { values, node });

            return retval;
        }

        /// <summary>
        /// Check whether the given type is a collection.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if it is a collection, otherwise false.</returns>
        private static bool IsCollection(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) == true &&
                type != typeof(string) &&
                type != typeof(byte[]) &&
                type.Name.Contains("Dictionary`2") == false;
        }

        /// <summary>
        /// Provides a value indicating whether to include the type name in the query columns.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>True to include the column, otherwise false.</returns>
        private static bool IncludeTypeName(Type type)
        {
            return type.IsAbstract == true || type.BaseType != typeof(object);
        }

        /// <summary>
        /// Determine the declaring type of the column.
        /// </summary>
        /// <param name="type">The type being inspected.</param>
        /// <param name="declaringType">The actual declaring type of the property.</param>
        /// <returns>The declaring type of the column.</returns>
        private static Type GetDeclaringType(Type type, Type declaringType)
        {
            if (declaringType == type)
            {
                return type;
            }
            else
            {
                TableAttribute ta = declaringType.GetCustomAttribute<TableAttribute>(false);
                if (ta != null)
                {
                    return declaringType;
                }
                else
                {
                    Type test = type;
                    Type thisType = null;
                    while (test != typeof(object))
                    {
                        ta = test.GetCustomAttribute<TableAttribute>(false);
                        if (ta != null)
                        {
                            thisType = test;
                        }
                        else if (test == declaringType)
                        {
                            return thisType;
                        }

                        test = test.BaseType;
                    }

                    return null;
                }
            }
        }

        /// <summary>
        /// Add system maintained columns to the set.
        /// </summary>
        /// <param name="columns">The set to append.</param>
        /// <param name="source">The query source.</param>
        /// <param name="type">The type to inspect.</param>
        private static void AppendSupplementalColumns(List<QueryColumn> columns, QuerySource source, Type type)
        {
            IEnumerable<KeyValuePair<Type, HashSet<string>>> supplementalColumns = supplements
                .Where(p => p.Key.IsAssignableFrom(type) == true);

            foreach (KeyValuePair<Type, HashSet<string>> item in supplementalColumns)
            {
                foreach (string col in item.Value)
                {
                    if (columns.Any(p => p.Alias == col) == false)
                    {
                        columns.Add(new QueryColumn()
                        {
                            Name = col,
                            Alias = col,
                            Source = source,
                            DeclaringType = item.Key,
                            ElementType = typeof(string)
                        });
                    }
                }
            }

            if (IncludeTypeName(type) == true)
            {
                QueryColumn typeColumn = new QueryColumn();
                if (source is QueryTable)
                {
                    typeColumn.Expression = string.Concat("'", type.FullName, "'");
                }
                else
                {
                    typeColumn.Name = "$TypeName";
                    typeColumn.Source = source;
                }

                typeColumn.Alias = "$TypeName";
                typeColumn.ElementType = typeof(string);
                columns.Add(typeColumn);
            }
        }

        /// <summary>
        /// Supplement the type with any override columns provided.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="items">The items to add.</param>
        private static void Supplement(Type type, IEnumerable<string> items)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type) == true && type != typeof(string))
            {
                type = type.GenericTypeArguments[0];
            }

            if (supplements.ContainsKey(type) == false)
            {
                supplements[type] = new HashSet<string>();
            }

            foreach (string item in items)
            {
                if (supplements[type].Contains(item) == false)
                {
                    supplements[type].Add(item);
                }
            }
        }

        /// <summary>
        /// Get the type accessor for a given type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>The correlative type accessor.</returns>
        private static FastMember.TypeAccessor GetTypeAccessor(Type type)
        {
            FastMember.TypeAccessor ta;
            if (typeAccessors.TryGetValue(type, out ta) == false)
            {
                ta = typeAccessors.GetOrAdd(type, FastMember.TypeAccessor.Create(type));
            }

            return ta;
        }

        /// <summary>
        /// Helper function to locate ambiguous references for foreign key discovery on one-many joins.
        /// </summary>
        /// <param name="containerType">The hosting type.</param>
        /// <param name="propertyName">The name of the collection property.</param>
        /// <returns>The column name mappings.</returns>
        private static Dictionary<string, string> LocateOverride(
            Type containerType,
            string propertyName,
            out QueryTable intermediateTable)
        {
            intermediateTable = null;
            PropertyInfo property = containerType.GetProperty(propertyName);
            if (property != null && manyToManies.ContainsKey(property) == true)
            {
                intermediateTable = new QueryTable()
                {
                    Name = manyToManies[property].Name,
                    Schema = manyToManies[property].Schema,
                    Alias = manyToManies[property].Alias,
                    Hint = manyToManies[property].Hint,
                    Path = manyToManies[property].Path
                };
            }

            if (property != null && overrides.Any(p => property.DeclaringType == p.Key.DeclaringType && property.Name == p.Key.Name) == true)
            {
                Dictionary<string, string> columns = new Dictionary<string, string>();
                foreach (PropertyInfo pi in overrides.Keys)
                {
                    if (pi.DeclaringType == property.DeclaringType && pi.Name == property.Name)
                    {
                        foreach (string key in overrides[pi].Keys)
                        {
                            columns.Add(key, overrides[pi][key]);
                        }

                        return columns;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// A guess based on class attribution when no join has been explicitly set in the override cache.
        /// </summary>
        /// <param name="parent">The parent container node.</param>
        /// <param name="node">The child contained node.</param>
        private static Dictionary<string, string> AttemptResolveJoin(CompositeNode parent, CompositeNode node)
        {
            Dictionary<string, string> statements = new Dictionary<string, string>();
            List<KeyMatch> sourcekeys = ExtractKeys(parent, node);
            List<KeyMatch> targetkeys = ExtractKeys(node, parent);

            List<string> sk = null;
            List<string> tk = null;

            if (sourcekeys.Any(p => p.IsPrimaryKey == true) && targetkeys.Any(p => p.IsPrimaryKey == false) && parent.ElementType != node.ElementType)
            {
                sk = sourcekeys.Where(p => p.IsPrimaryKey == true).Select(p => p.Name).ToList();
                tk = targetkeys.Where(p => p.IsPrimaryKey == false && p.IsExactMatch == true).Select(p => p.Name).ToList();
                if (tk.Count == 0)
                {
                    tk = targetkeys.Where(p => p.IsPrimaryKey == false).Select(p => p.Name).ToList();
                }
            }
            else if (targetkeys.Any(p => p.IsPrimaryKey == true) && sourcekeys.Any(p => p.IsPrimaryKey == false))
            {
                sk = sourcekeys.Where(p => p.IsPrimaryKey == false && p.IsExactMatch == true).Select(p => p.Name).ToList();
                tk = targetkeys.Where(p => p.IsPrimaryKey == true).Select(p => p.Name).ToList();
                if (sk.Count == 0)
                {
                    sk = sourcekeys.Where(p => p.IsPrimaryKey == false).Select(p => p.Name).ToList();
                }
            }
            else if (parent == node)
            {
                // self join.
                sk = sourcekeys.Where(p => p.IsPrimaryKey == true).Select(p => p.Name).ToList();
                tk = targetkeys.Where(p => p.IsPrimaryKey == true).Select(p => p.Name).ToList();
            }

            if (sk == null || tk == null || sk.Count != tk.Count)
            {
                throw new ArgumentException("Mismatched nodes can not be joined.");
            }

            for (int i = 0; i < sk.Count; i++)
            {
                statements[sk[i]] = tk[i];
            }

            return statements;
        }

        /// <summary>
        /// Extract the keys for the given node.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="othernode">The other node.</param>
        /// <returns>The list of keys include name and whether it is a primary key.</returns>
        private static List<KeyMatch> ExtractKeys(CompositeNode node, CompositeNode othernode)
        {
            List<KeyMatch> keys = new List<KeyMatch>();
            IEnumerable<PropertyInfo> cols = GetProperties(node.ElementType);
            foreach (PropertyInfo pi in cols)
            {
                ColumnAttribute ca = GetAttribute<ColumnAttribute>(pi);
                ForeignKeyAttribute fka = GetAttribute<ForeignKeyAttribute>(pi);
                KeyAttribute ka = GetAttribute<KeyAttribute>(pi);

                if (ka != null)
                {
                    string sourcepkname = ReadColumnName(ca, pi);
                    keys.Add(new KeyMatch() { Name = sourcepkname, IsPrimaryKey = true, IsExactMatch = true });
                }

                if (fka != null)
                {
                    PropertyInfo col = cols.Single(p => p.Name.Equals(fka.Name, StringComparison.OrdinalIgnoreCase) == true);
                    if ((node.Reverse == false && col.Name.Equals(othernode.Path, StringComparison.OrdinalIgnoreCase) == true) ||
                        (node.Reverse == true && col.Name.Equals(node.Path, StringComparison.OrdinalIgnoreCase) == true))
                    {
                        string sourcefkname = ReadColumnName(ca, pi);
                        keys.Add(new KeyMatch() { Name = sourcefkname, IsPrimaryKey = false, IsExactMatch = true });
                    }
                    else if (col.PropertyType.IsAssignableFrom(othernode.ElementType) == true)
                    {
                        string sourcefkname = ReadColumnName(ca, pi);
                        keys.Add(new KeyMatch() { Name = sourcefkname, IsPrimaryKey = false, IsExactMatch = false });
                    }
                }
            }

            return keys;
        }

        /// <summary>
        /// Read the column name.
        /// </summary>
        /// <param name="ca">The column attribute to inspect.</param>
        /// <param name="pi">The property info to inspect.</param>
        /// <returns>The modified column name.</returns>
        private static string ReadColumnName(ColumnAttribute ca, PropertyInfo pi)
        {
            string sourcefkname = pi.Name;
            if (ca != null && string.IsNullOrEmpty(ca.Name) == false)
            {
                sourcefkname = ca.Name;
            }

            return sourcefkname;
        }

        /// <summary>
        /// Create a default value for the provided type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object CreateDefaultValue(Type type)
        {
            switch (type.Name)
            {
                case "Int32": return default(int);
                case "Int64": return default(int);
                case "String": return "-";
                case "DateTimeOffset": return default(DateTimeOffset);
                case "Guid": return default(Guid);
                default: return null;
            }
        }

        /// <summary>
        /// Encapsulate the operation to load property names for a given type.
        /// </summary>
        /// <param name="types">The types to inspect.</param>
        /// <returns>The distinct list of properties.</returns>
        private static List<PropertyInfo> GetProperties(params Type[] types)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (Type type in types)
            {
                PropertyInfo[] subprops = type.GetProperties();
                foreach (PropertyInfo subprop in subprops)
                {
                    if (properties.Any(p => p.Name == subprop.Name && p.DeclaringType == subprop.DeclaringType) == false)
                    {
                        properties.Add(subprop);
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Creates a fills a collection of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the collection.</typeparam>
        /// <typeparam name="U">The type of the members of the collection.</typeparam>
        /// <param name="values">A list of values to add.</param>
        /// <param name="node">The composite node where this collection resides.</param>
        /// <returns>The populated collection.</returns>
        private static ICollection<U> CreateAndFillCollection<T, U>(IEnumerable<object> values, CompositeNode node)
            where T : ICollection<U>
        {
            T list = default(T);
            if (typeof(T).IsInterface == false)
            {
                list = Activator.CreateInstance<T>();
            }
            else
            {
                list = (T)((ICollection<U>)new List<U>());
            }

            int skipindex = 0;
            int topindex = 0;
            foreach (object value in values)
            {
                if (node != null)
                {
                    if (node.Skip.HasValue && skipindex++ < node.Skip.Value)
                    {
                        continue;
                    }

                    if (node.Top.HasValue && topindex++ >= node.Top.Value)
                    {
                        break;
                    }
                }

                list.Add((U)value);
            }

            return list;
        }

        /// <summary>
        /// Inventory all of the relevant types.
        /// </summary>
        private static Dictionary<Type, IEnumerable<PropertyInfo>> InventoryTypes()
        {
            string code = null;
            Assembly entry = Assembly.GetEntryAssembly();
            if (entry != null)
            {
                code = entry.CodeBase;
                int pos = code.LastIndexOf('/');
                code = code.Substring(0, pos);
            }

            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(p => p.IsDynamic == false &&
                           (p.CodeBase.Contains("/Plugins/") == true ||
                            p.CodeBase.StartsWith(code ?? "X") == true));

            Dictionary<Type, IEnumerable<PropertyInfo>> typeMap = new Dictionary<Type, IEnumerable<PropertyInfo>>();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        PropertyInfo[] properties = type.GetProperties();
                        typeMap[type] = properties;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }

            return typeMap;
        }

        /// <summary>
        /// Order a given collection based on the provided property.
        /// </summary>
        /// <typeparam name="CollectionType">The type of the members in the collection.</typeparam>
        /// <param name="source">The list to sort.</param>
        /// <param name="property">The property to use for sorting.</param>
        /// <param name="first">True if first property in sort list, else false.</param>
        /// <returns>The sorted list of data.</returns>
        private static IOrderedQueryable<CollectionType> OrderBy<CollectionType>(
            IQueryable<CollectionType> source, 
            string property,
            bool first)
        {
            string[] parts = property.Split(' ');
            bool descending = false;
            if (parts.Length == 2 && parts[1].ToLower() == "desc")
            {
                descending = true;
            }

            if (first == true && descending == false)
            {
                return ApplyOrder<CollectionType>(source, parts[0], "OrderBy");
            }
            else if (first == true && descending == true)
            {
                return ApplyOrder<CollectionType>(source, parts[0], "OrderByDescending");
            }
            else if (first == false && descending == false)
            {
                return ApplyOrder<CollectionType>(source, parts[0], "ThenBy");
            }
            else
            {
                return ApplyOrder<CollectionType>(source, parts[0], "ThenByDescending");
            }
        }

        /// <summary>
        /// Apply an orderby statement to a queryable.
        /// </summary>
        /// <typeparam name="CollectionType"></typeparam>
        /// <param name="source"></param>
        /// <param name="property"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static IOrderedQueryable<CollectionType> ApplyOrder<CollectionType>(
            IQueryable<CollectionType> source, 
            string property, 
            string methodName)
        {
            Type type = typeof(CollectionType);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;

            PropertyInfo pi = type.GetProperty(property);
            expr = Expression.Property(expr, pi);
            type = pi.PropertyType;

            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(CollectionType), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            object result = typeof(Queryable).GetMethods().Single(
                    method => method.Name == methodName &&
                              method.IsGenericMethodDefinition &&
                              method.GetGenericArguments().Length == 2 &&
                              method.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(CollectionType), type)
                    .Invoke(null, new object[] { source, lambda });

            return (IOrderedQueryable<CollectionType>)result;
        }

        /// <summary>
        /// Helper class to describe steps in the path.
        /// </summary>
        internal class PathStep
        {
            /// <summary>
            /// Gets or sets the name of the step.
            /// </summary>
            public string Name
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the type of the step.
            /// </summary>
            public Type Type
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets a value indicating whether the step is a type constraint.
            /// </summary>
            public bool IsTypeConstraint
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the argument string, if applicable.
            /// </summary>
            public string Args
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Helper class to describe key matches.
        /// </summary>
        private class KeyMatch
        {
            /// <summary>
            /// Gets or sets the name of the key.
            /// </summary>
            public string Name
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets a value indicating whether the key is a PK.
            /// </summary>
            public bool IsPrimaryKey
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets a value indicating whether the key is an exact match.
            /// If this is false, it means the path does not match the property name, but the types do match.
            /// This may be a good guess for a match when resolving relationship from the target to the source.
            /// </summary>
            public bool IsExactMatch
            {
                get;
                set;
            }
        }
    }
}
