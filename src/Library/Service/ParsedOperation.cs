// -----------------------------------------------------------------------
// <copyright file="ParsedFunction.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    using System.Web.OData;
    using System.Web.OData.Routing;
    using Microsoft.OData.Edm;
    using Parser = Microsoft.OData.UriParser;

    /// <summary>
    /// Helper class for function parsing.
    /// </summary>
    public class ParsedOperation
    {
        /// <summary>
        /// Initializes an instance of the ParsedFunction class.
        /// </summary>
        /// <param name="model">The current model.</param>
        /// <param name="modelType">The type of the current model.</param>
        /// <param name="path">The current odata path.</param>
        internal ParsedOperation(IEdmModel model, Type modelType, ODataPath path)
        {
            this.Initialize(model, modelType, path);
        }

        /// <summary>
        /// Gets the name of the function.
        /// May be either qualified or unqualified.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the entity set for the function.
        /// May be null if function is not bound to entityset.
        /// </summary>
        public IEdmEntitySet EntitySet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the singleton for the function.
        /// May be null if function is not bound to singleton.
        /// </summary>
        public IEdmSingleton Singleton
        {
            get;
            private set;
        }

        /// <summary>
        /// The list of parameters.
        /// </summary>
        public IList<ObjectParameter> Parameters
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the entity key for the current operation.
        /// </summary>
        public InfrastructureKey Key
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the type of the return value.
        /// </summary>
        public IEdmType ReturnType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the return type.
        /// </summary>
        public string ReturnTypeName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the operation allows return data.
        /// </summary>
        public bool AllowsReturnData
        {
            get;
            private set;
        }

        /// <summary>
        /// Configure the parameters for the function.
        /// </summary>
        /// <param name="parameters">The non-null list of parameters.</param>
        internal void ConfigureParameters(ODataActionParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            List<ObjectParameter> convertedParameters = new List<ObjectParameter>();
            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                convertedParameters.Add(new ObjectParameter(parameter.Key, parameter.Value));
            }

            this.Parameters = convertedParameters.AsReadOnly();
        }

        /// <summary>
        /// Convert an entity set segment into an entity set.
        /// </summary>
        /// <param name="segment">The segment to convert.</param>
        /// <returns>The entity set.</returns>
        private static IEdmEntitySet Convert(Parser.ODataPathSegment segment)
        {
            IEdmNavigationSource edm = segment.GetNavigationSource(null);

            return edm as IEdmEntitySet;
        }

        /// <summary>
        /// Read the name from the given path and segment.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <param name="segment">The segment number where the name lives.</param>
        /// <returns>The name of the function.</returns>
        private static string ReadName(ODataPath path, int segment)
        {
            string token = path.Segments[segment].Identifier;
            if (string.IsNullOrEmpty(token) == true)
            {
                Parser.OperationSegment op = path.Segments[segment] as Parser.OperationSegment;
                Parser.OperationImportSegment opimp = path.Segments[segment] as Parser.OperationImportSegment;
                if (op != null && op.Operations.Count() == 1)
                {
                    token = op.Operations.First().Name;
                }
                else if (opimp != null && opimp.OperationImports.Count() == 1)
                {
                    token = opimp.OperationImports.First().Name;
                }
            }

            int length = token.IndexOf('(');
            if (length < 0)
            {
                return token;
            }

            return token.Substring(0, length);
        }

        /// <summary>
        /// Determine the full name of the edm type.
        /// </summary>
        /// <param name="value">The edm type to evaluate.</param>
        /// <returns>The serialized name of the type.</returns>
        private static string AssignReturnTypeName(IEdmType value)
        {
            if (value == null)
            {
                return null;
            }

            switch (value.TypeKind)
            {
                case EdmTypeKind.Collection:
                    return AssignReturnTypeName(((IEdmCollectionType)value).ElementType.Definition);
                case EdmTypeKind.Complex:
                case EdmTypeKind.Entity:
                case EdmTypeKind.EntityReference:
                case EdmTypeKind.Enum:
                case EdmTypeKind.Primitive:
                case EdmTypeKind.TypeDefinition:
                default:
                    return value.FullTypeName();
            }
        }

        /// <summary>
        /// Determines whether to allow the operation to return data of type T.
        /// </summary>
        /// <param name="model">The current model.</param>
        /// <param name="modelType">The type of the current model.</param>
        /// <param name="path">The current odata path.</param>
        /// <returns>True if operation can return data, otherwise false.</returns>
        private void Initialize(IEdmModel model, Type modelType, ODataPath path)
        {
            int ordinal = path.Segments.Count - 1;
            this.Name = ReadName(path, ordinal);
            if (ordinal > 0)
            {
                this.EntitySet = Convert(path.Segments[0]);
                if (this.EntitySet == null)
                {
                    this.Singleton = path.Segments[0].GetNavigationSource(null) as IEdmSingleton;
                }
            }

            IEnumerable<EdmOperation> operations = model.SchemaElements
                .OfType<EdmOperation>()
                .Where(p => p.Name == this.Name || p.FullName() == this.Name);

            if (operations.Select(p => p.ReturnType == null ? "null" : p.ReturnType.FullName()).Distinct().Count() > 1)
            {
                throw new NotSupportedException();
            }

            EdmOperation operation = operations.First();

            this.ReturnType = operation.ReturnType != null ? operation.ReturnType.Definition : null;
            this.ReturnTypeName = AssignReturnTypeName(this.ReturnType);
            if (operation.ReturnType == null || operation.ReturnType.IsInt32() == true)
            {
                this.AllowsReturnData = false;
            }
            else
            {
                this.AllowsReturnData = true;
            }
        }
    }
}
