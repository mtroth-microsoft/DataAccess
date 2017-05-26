//-------------------------------------------------------------------------------------------------
// <copyright file="SimpleParameter.cs" Company="Lensgrinder, Ltd.">
//     Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
//-------------------------------------------------------------------------------------------------
namespace Infrastructure.DataAccess.OdataExpressionModel
{
    using System;
    using System.Data;

    /// <summary>
    /// Helper class to contain parameter data for various purposes.
    /// </summary>
    public class SimpleParameter
    {
        /// <summary>
        /// Initializes a new instance of the SimpleParameter class.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="clrType">The clr type of the parameter.</param>
        public SimpleParameter(
            string name,
            Type clrType)
        {
            this.Name = name;
            this.ClrType = clrType;
        }

        /// <summary>
        /// Initializes a new instance of the SimpleParameter class.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="clrType">The clr type of the parameter.</param>
        public SimpleParameter(
            string name,
            object value,
            Type clrType)
        {
            this.Name = name;
            this.Value = value;
            this.ClrType = clrType;
        }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        /// <value>The property value.</value>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        /// <value>The property value.</value>
        public Type ClrType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        /// <value>The property value.</value>
        public object Value
        {
            get;
            set;
        }
    }
}
