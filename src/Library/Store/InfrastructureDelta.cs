// -----------------------------------------------------------------------
// <copyright file="InfrastructureDelta.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Web.OData;
    using Microsoft.OData;

    internal class InfrastructureDelta<T> : Delta<T>, IPropertyBag
        where T : class
    {
        /// <summary>
        /// Override the default behavior to account for enums and complex types.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>True if the property was found, otherwise false.</returns>
        public override bool TrySetPropertyValue(string name, object value)
        {
            bool result = false;
            if (value is ODataEnumValue)
            {
                Type propertyType;
                if (this.TryGetPropertyType(name, out propertyType))
                {
                    propertyType = ConvertNullableType(propertyType);
                    value = Enum.Parse(propertyType, ((ODataEnumValue)value).Value);
                    result = base.TrySetPropertyValue(name, value);
                }
            }
            else if (value is EdmEnumObject)
            {
                Type propertyType;
                if (this.TryGetPropertyType(name, out propertyType))
                {
                    propertyType = ConvertNullableType(propertyType);
                    value = Enum.Parse(propertyType, ((EdmEnumObject)value).Value);
                    result = base.TrySetPropertyValue(name, value);
                }
            }
            else if (value is IEdmStructuredObject)
            {
                Type propertyType;
                if (this.TryGetPropertyType(name, out propertyType))
                {
                    EdmStructuredObject eso = value as EdmStructuredObject;
                    Type type = typeof(InfrastructureDelta<>);
                    Type generic = type.MakeGenericType(propertyType);
                    IPropertyBag instance = Activator.CreateInstance(generic) as IPropertyBag;

                    IEnumerable<string> changedNames = eso.GetChangedPropertyNames();
                    foreach (string changedName in changedNames)
                    {
                        object item;
                        eso.TryGetPropertyValue(changedName, out item);
                        instance.TrySetPropertyValue(changedName, item);
                    }

                    MethodInfo method = generic.GetMethod("GetEntity");
                    object entity = method.Invoke(instance, null);
                    result = base.TrySetPropertyValue(name, entity);
                }
            }
            else
            {
                result = base.TrySetPropertyValue(name, value);
            }

            return result;
        }

        /// <summary>
        /// Helper to convert nullable types to underlying type.
        /// </summary>
        /// <param name="propertyType">The type to test.</param>
        /// <returns>The underlying type, if applicable.</returns>
        private static Type ConvertNullableType(Type propertyType)
        {
            if (propertyType.IsGenericType == true &&
                propertyType.Name.StartsWith("Nullable") == true)
            {
                Type converted = propertyType.GenericTypeArguments[0];
                return converted;
            }

            return propertyType;
        }
    }
}
