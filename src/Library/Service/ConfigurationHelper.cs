// -----------------------------------------------------------------------
// <copyright file="ConfigurationHelper.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// <summary>The file summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Reflection;
    using System.Web.OData.Builder;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Csdl;
    using Microsoft.OData.Edm.Vocabularies;

    /// <summary>
    /// Defines a delegate to allow adding actions to builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public delegate void ModelExtender(ODataConventionModelBuilder builder);

    /// <summary>
    /// The Configuration Helper.
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// The annotation namespace.
        /// </summary>
        private const string AnnotationNamespace = "http://schemas.microsoft.com/dataaccess/2015/01/annotations";

        /// <summary>
        /// The prefix to use for the annotation namespace.
        /// </summary>
        private const string AnnotationPrefix = "da";

        /// <summary>
        /// Creates a transaction for use by a datasource.
        /// </summary>
        /// <param name="datasource">The datasource to transact.</param>
        /// <returns>The created transaction.</returns>
        public static ITransaction CreateTransaction(IDatasource datasource)
        {
            return new DefaultTransaction(datasource);
        }

        /// <summary>
        /// Get the page size to use for the given entity set.
        /// </summary>
        /// <param name="datasource">The datasource in scope.</param>
        /// <param name="entitySet">The name of the entity set.</param>
        /// <returns>The relevant page size, if applicable.</returns>
        public static long? GetPageSize(IDatasource datasource, string entitySet)
        {
            long? pageSize = null;
            IEnumerable<PagingConfigurationItemType> items = null;
            InfrastructureConfigType config = datasource.GetConfig();
            if (config.PagingSizes != null && config.PagingSizes.EntitySets != null)
            {
                items = datasource.GetConfig().PagingSizes.EntitySets.Where(p => p.Name == entitySet || p.Name == "*");
                foreach (PagingConfigurationItemType item in items)
                {
                    if (item.Name == entitySet)
                    {
                        pageSize = item.Size;
                    }
                    else if (item.Name == "*" && pageSize.HasValue == false)
                    {
                        pageSize = item.Size;
                    }
                }
            }

            return pageSize;
        }

        /// <summary>
        /// Determines whether a given type is supported or not.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>True if type is supported, otherwise false.</returns>
        public static bool IsSupportedModelType(Type type)
        {
            if (type != typeof(DatasourceWrapper) && type.IsInterface == false && type.IsAbstract == false)
            {
                Type dsinterface = type.GetInterface(typeof(IDatasource).FullName);
                Type mtdinterface = type.GetInterface(typeof(IMetadataProvider).FullName);
                DatasourceAttribute da = type.GetCustomAttribute<DatasourceAttribute>(true);

                if (dsinterface != null || 
                    mtdinterface != null ||
                    type.IsSubclassOf(typeof(DbContext)) || 
                    type.IsSubclassOf(typeof(ObjectContext)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a default instance of the datasource config.
        /// </summary>
        /// <returns>The default config.</returns>
        public static InfrastructureConfigType CreateDefaultConfig()
        {
            InfrastructureConfigType instance = new InfrastructureConfigType();
            instance.Access = new AccessConfigurationType();
            instance.Access.EntitySets = new List<EntitySetAccessType>();
            instance.Access.ServiceOperations = new List<ServiceOperationAccessType>();
            instance.Access.ServiceActions = new List<ServiceActionAccessType>();
            instance.Access.WriteEnabled = false;
            instance.PagingSizes = new PagingConfigurationType();
            instance.PagingSizes.EntitySets = new List<PagingConfigurationItemType>();
            instance.DataFilters = new DataFilterConfigurationType();
            instance.DataFilters.EdmElements = new List<DataFilterConfigurationItemType>();
            instance.Obsoleted = new ObsoletedConfigurationType();
            instance.Obsoleted.Apis = new List<ObsoletedConfigurationItemType>();

            instance.Access.EntitySets.Add(new EntitySetAccessType() { Name = "*", Access = EntitySetAccessibility.All });
            instance.Access.ServiceOperations.Add(new ServiceOperationAccessType() { Name = "*", Access = ServiceOperationAccessibility.All });
            instance.Access.ServiceActions.Add(new ServiceActionAccessType() { Name = "*", Access = ServiceActionAccessibility.Invoke });
            instance.PagingSizes.EntitySets.Add(new PagingConfigurationItemType() { Name = "*", Size = 100 });

            return instance;
        }

        /// <summary>
        /// Get the model for the datasource.
        /// </summary>
        /// <param name="context">The context to build into a model.</param>
        /// <param name="extender">ModelExtender delegate, null if not applicable.</param>
        /// <returns>The model.</returns>
        public static IEdmModel BuildModel(
            DbContext context, 
            ModelExtender extender)
        {
            IObjectContextAdapter adapter = context as IObjectContextAdapter;

            return BuildModel(adapter.ObjectContext, context.GetType(), extender);
        }

        /// <summary>
        /// Get the model for the datasource.
        /// </summary>
        /// <param name="context">The context to build into a model.</param>
        /// <param name="actions">ModelExtender delegate, null if not applicable.</param>
        /// <returns>The model.</returns>
        public static IEdmModel BuildModel(
            ObjectContext context, 
            Type contextType,
            ModelExtender extender)
        {
            string name = contextType.Name;
            string ns = contextType.Namespace;

            IReadOnlyCollection<EntityType> et = context.MetadataWorkspace.GetItems<EntityType>(DataSpace.CSpace);
            IReadOnlyCollection<ComplexType> ct = context.MetadataWorkspace.GetItems<ComplexType>(DataSpace.CSpace);
            EntityContainer ec = context.MetadataWorkspace.GetEntityContainer(name, DataSpace.CSpace);

            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.ContainerName = ec.Name;
            builder.Namespace = ns;

            foreach (ComplexType item in ct)
            {
                Type type = contextType.Assembly.GetType(item.FullName);
                ComplexTypeConfiguration config = builder.AddComplexType(type);
                foreach (PropertyInfo pi in type.GetProperties())
                {
                    config.AddProperty(pi);
                }
            }

            foreach (EntityType item in et)
            {
                Type type = contextType.Assembly.GetType(item.FullName);
                EntityTypeConfiguration config = builder.AddEntityType(type);
                foreach (PropertyInfo pi in type.GetProperties())
                {
                    Type pitype = pi.PropertyType;
                    if (pitype.Name.StartsWith("EntityReference") == true)
                    {
                        config.RemoveProperty(pi);
                    }

                    if (pitype.Name.Equals("EntityKey") == true)
                    {
                        config.RemoveProperty(pi);
                    }

                    if (item.KeyProperties.SingleOrDefault(p => p.Name == pi.Name) != null)
                    {
                        config.HasKey(pi);
                    }
                }
            }

            foreach (EntitySet es in ec.EntitySets)
            {
                StructuralTypeConfiguration config = builder.StructuralTypes
                    .Where(p => p.Name == es.ElementType.Name)
                    .Single();
                builder.AddEntitySet(es.Name, config as EntityTypeConfiguration);
            }

            if (extender != null)
            {
                extender(builder);
            }

            IEdmModel model = builder.GetEdmModel();
            builder.ValidateModel(model);

            return model;
        }

        /// <summary>
        /// Write the annotations for the provided model.
        /// </summary>
        /// <param name="model">The model to use.</param>
        public static void WriteAnnotations(IEdmModel model, IDatasource datasource)
        {
            string dataFiltersLocalName = "DataFilters";
            string obsoletedLocalName = "Obsolete";
            string groupByLocalName = "GroupBy";

            // Configure the namespace prefixes for the EDM document.
            model.SetNamespacePrefixMappings(
                new KeyValuePair<string, string>[] 
                { 
                    new KeyValuePair<string, string>(AnnotationPrefix, AnnotationNamespace) 
                });

            // Use the configuration to write the datafilters to the $metadata.
            IEdmPrimitiveTypeReference dateType = EdmCoreModel.Instance.GetDate(true);
            InfrastructureConfigType config = datasource.GetConfig();
            foreach (DataFilterConfigurationItemType item in config.DataFilters.EdmElements)
            {
                EdmEntityType entityType = new EdmEntityType(AnnotationNamespace, dataFiltersLocalName);
                EdmTypeReference elementType = new EdmEntityTypeReference(entityType, false);
                EdmCollectionType collectionType = new EdmCollectionType(elementType);
                EdmTypeReference reference = new EdmCollectionTypeReference(collectionType);
                IEdmCollectionExpression value = new EdmCollection<DataFilterParameterType>(reference, item.Parameters);
                WriteDataFilterTarget(item, model);

                if (item is EntitySetDataFilterType)
                {
                    SetAnnotation(item.Name, value, EdmPrimitiveTypeKind.String, reference, dataFiltersLocalName, model);
                }
                else
                {
                    SetAnnotationOnOperation(item.Name, value, EdmPrimitiveTypeKind.String, reference, dataFiltersLocalName, model);
                }
            }

            if (config.GroupBy != null && config.GroupBy.Apis != null)
            {
                foreach (GroupByConfigurationItemType item in config.GroupBy.Apis)
                {
                    EdmEntityType entityType = new EdmEntityType(AnnotationNamespace, dataFiltersLocalName);
                    EdmTypeReference elementType = new EdmEntityTypeReference(entityType, false);
                    EdmCollectionType collectionType = new EdmCollectionType(elementType);
                    EdmTypeReference reference = new EdmCollectionTypeReference(collectionType);
                    IEdmCollectionExpression value = new EdmCollection<GroupByItemType>(reference, item.Items);

                    if (item is OperationGroupByType)
                    {
                        SetAnnotationOnOperation(item.Name, value, EdmPrimitiveTypeKind.String, reference, groupByLocalName, model);
                    }
                    else
                    {
                        SetAnnotation(item.Name, value, EdmPrimitiveTypeKind.String, reference, groupByLocalName, model);
                    }
                }
            }

            if (config.Obsoleted != null && config.Obsoleted.Apis != null)
            {
                foreach (ObsoletedConfigurationItemType item in config.Obsoleted.Apis)
                {
                    EdmDateConstant value = new EdmDateConstant(dateType, item.RemovalEstimate);
                    if (item is EntitySetObsoletedType)
                    {
                        SetAnnotation(item.Name, value, EdmPrimitiveTypeKind.Date, null, obsoletedLocalName, model);
                    }
                    else
                    {
                        SetAnnotationOnOperation(item.Name, value, EdmPrimitiveTypeKind.Date, null, obsoletedLocalName, model);
                    }
                }
            }
        }

        /// <summary>
        /// Write the data filter target entity set annotation, if applicable.
        /// </summary>
        /// <param name="item">The data filter configuration item.</param>
        /// <param name="model">The model being inspected.</param>
        private static void WriteDataFilterTarget(DataFilterConfigurationItemType item, IEdmModel model)
        {
            if (string.IsNullOrEmpty(item.TargetEntitySet) == false)
            {
                IEdmEntitySet target = (IEdmEntitySet)model.FindDeclaredEntitySet(item.TargetEntitySet);
                EdmPathExpression value = new EdmPathExpression(target.Path.PathSegments);
                if (item is EntitySetDataFilterType)
                {
                    SetAnnotation(item.Name, value, EdmPrimitiveTypeKind.String, null, "DataFilterTargetEntitySet", model);
                }
                else
                {
                    SetAnnotationOnOperation(item.Name, value, EdmPrimitiveTypeKind.String, null, "DataFilterTargetEntitySet", model);
                }
            }
        }

        /// <summary>
        /// Writes the given value to the model for the given entityset.
        /// </summary>
        /// <param name="entitySet">The name of the entity set to annotate.</param>
        /// <param name="value">The annotation value.</param>
        /// <param name="kind">The edm primitive kind.</param>
        /// <param name="reference">Reference type, if applicable.</param>
        /// <param name="localName">The attribute name for the annotation.</param>
        /// <param name="model">The model to use.</param>
        private static void SetAnnotation(
            string entitySet, 
            IEdmExpression value, 
            EdmPrimitiveTypeKind kind, 
            EdmTypeReference reference,
            string localName, 
            IEdmModel model)
        {
            IEdmEntitySet target = (IEdmEntitySet)model.FindDeclaredEntitySet(entitySet);
            if (target != null)
            {
                model.DirectValueAnnotationsManager.SetAnnotationValue(
                    target,
                    AnnotationNamespace,
                    localName,
                    ConvertValueForAttributeAnnotation(value));

                Microsoft.OData.Edm.EdmModel edm = model as Microsoft.OData.Edm.EdmModel;
                if (edm != null)
                {
                    EdmTerm term = null;
                    if (reference != null)
                    {
                        term = new EdmTerm(AnnotationNamespace, localName, reference);
                    }
                    else
                    {
                        term = new EdmTerm(AnnotationNamespace, localName, kind);
                    }

                    IEdmVocabularyAnnotation annotation = new EdmVocabularyAnnotation(target, term, value);
                    edm.SetVocabularyAnnotation(annotation);
                }
            }
        }

        /// <summary>
        /// Writes the given value to the model for the given operation.
        /// </summary>
        /// <param name="operation">The name of the operation to annotate.</param>
        /// <param name="value">The annotation value.</param>
        /// <param name="kind">The edm primitive kind.</param>
        /// <param name="reference">Reference type, if applicable.</param>
        /// <param name="localName">The attribute name for the annotation.</param>
        /// <param name="model">The model to use.</param>
        private static void SetAnnotationOnOperation(
            string operation,
            IEdmExpression value,
            EdmPrimitiveTypeKind kind,
            EdmTypeReference reference,
            string localName, 
            IEdmModel model)
        {
            IEdmOperation target = (IEdmOperation)model.FindDeclaredOperations(operation).SingleOrDefault();
            if (target != null)
            {
                model.DirectValueAnnotationsManager.SetAnnotationValue(
                    target,
                    AnnotationNamespace,
                    localName,
                    ConvertValueForAttributeAnnotation(value));

                Microsoft.OData.Edm.EdmModel edm = model as Microsoft.OData.Edm.EdmModel;
                if (edm != null)
                {
                    EdmTerm term = null;
                    if (reference != null)
                    {
                        term = new EdmTerm(AnnotationNamespace, localName, reference);
                    }
                    else
                    {
                        term = new EdmTerm(AnnotationNamespace, localName, kind);
                    }

                    IEdmVocabularyAnnotation annotation = new EdmVocabularyAnnotation(target, term, value);
                    edm.SetVocabularyAnnotation(annotation);
                }
            }
        }

        /// <summary>
        /// Convert the expression value, if applicable.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>The converted value.</returns>
        private static IEdmExpression ConvertValueForAttributeAnnotation(IEdmExpression value)
        {
            IEdmCollectionExpression collection = value as IEdmCollectionExpression;
            if (collection == null)
            {
                return value;
            }
            else
            {
                IEdmStringTypeReference stringType = EdmCoreModel.Instance.GetString(true);
                string joined = collection.ToString();
                EdmStringConstant data = new EdmStringConstant(stringType, joined);
                return data;
            }
        }

        /// <summary>
        /// Helper class for edm collections.
        /// </summary>
        private class EdmCollection<T> : IEdmCollectionExpression
        {
            /// <summary>
            /// Initializes a new instance of the EdmCollection class.
            /// </summary>
            /// <param name="declaredType">The declared type.</param>
            /// <param name="elements">The list of elements.</param>
            public EdmCollection(IEdmTypeReference declaredType, IEnumerable<T> elements)
            {
                this.DeclaredType = declaredType;
                List<IEdmExpression> contents = new List<IEdmExpression>();
                foreach (T element in elements)
                {
                    IEdmExpression value = null;
                    if (typeof(T) == typeof(string))
                    {
                        value = new EdmStringConstant(element.ToString());
                    }
                    else if (typeof(T).IsClass == true)
                    {
                        EdmEntityType entityType = new EdmEntityType(AnnotationNamespace, typeof(T).Name);
                        EdmEntityTypeReference elementType = new EdmEntityTypeReference(entityType, false);
                        List<EdmPropertyConstructor> constructors = new List<EdmPropertyConstructor>();
                        PropertyInfo[] properties = typeof(T).GetProperties();
                        foreach (PropertyInfo property in properties)
                        {
                            object v = TypeCache.GetValue(typeof(T), property.Name, element);
                            if (property.PropertyType == typeof(bool))
                            {
                                constructors.Add(new EdmPropertyConstructor(property.Name, new EdmBooleanConstant((bool)v)));
                            }
                            else
                            {
                                constructors.Add(new EdmPropertyConstructor(property.Name, new EdmStringConstant(v.ToString())));
                            }
                        }

                        value = new EdmRecordExpression(elementType, constructors);
                    }

                    contents.Add(value);
                }

                this.Elements = contents;
            }

            /// <summary>
            /// Gets the declared type.
            /// </summary>
            public IEdmTypeReference DeclaredType
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the elements.
            /// </summary>
            public IEnumerable<IEdmExpression> Elements
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the expression kind.
            /// </summary>
            public EdmExpressionKind ExpressionKind
            {
                get
                {
                    return EdmExpressionKind.Collection;
                }
            }

            /// <summary>
            /// Override of the tostring method.
            /// </summary>
            /// <returns>The serialized string.</returns>
            public override string ToString()
            {
                if (typeof(T) == typeof(string))
                {
                    IEnumerable<string> set = this.Elements.Cast<EdmStringConstant>().Select(p => p.Value);
                    string joined = string.Join(", ", set);
                    return joined;
                }
                else if (typeof(T).IsClass == true)
                {
                    IEnumerable<string> set = this.Elements.Cast<EdmRecordExpression>().Select(p => (p.Properties.First().Value as EdmStringConstant).Value);
                    string joined = string.Join(", ", set);
                    return joined;
                }

                return base.ToString();
            }
        }
    }
}