// -----------------------------------------------------------------------
// <copyright file="Column.cs" Company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// <summary>The File Summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using Config = Configuration;

    /// <summary>
    /// The class declaration.
    /// </summary>
    internal class Column : TabularObject
    {
        /// <summary>
        /// Initializes a new instance of the Column class.
        /// </summary>
        /// <param name="property">The property to use to create the class.</param>
        public Column(Config.Property property)
        {
            this.Name = property.Name;
            this.TypeName = Convert(property.Type);
            this.IsNullable = property.Nullable;
            this.IsIdentity = property.AutoIncrement;
            this.IsComputed = property.Computed;
            this.Formula = property.Formula;
            this.Precision = GetPrecision(property.Type);
            this.Scale = GetScale(property.Type);
            if (property.Size.HasValue == true)
            {
                this.MaxLength = (int)property.Size.Value;
            }

            if (this.IsIdentity == true)
            {
                this.Seed = property.InitialSeed;
                this.Increment = property.Increment;
            }

            if (string.IsNullOrEmpty(property.Default) == false)
            {
                this.DefaultConstraint = new DefaultConstraint(property);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public CheckConstraint CheckConstraint
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public DefaultConstraint DefaultConstraint
        {
            get;
            private set;
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
        public string TypeName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int MaxLength
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int Precision
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int Scale
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the column is nullable.
        /// </summary>
        public bool IsNullable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the column is an identity.
        /// </summary>
        public bool IsIdentity
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the seed for the identity, if applicable.
        /// </summary>
        public uint? Seed
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the increment for the identity, if applicable.
        /// </summary>
        public uint? Increment
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether the column is computed.
        /// </summary>
        public bool IsComputed
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value of the computed formula, if applicable.
        /// </summary>
        public string Formula
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the column is sparse.
        /// </summary>
        public bool IsSparse
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int ColumnId
        {
            get;
            private set;
        }

        /// <summary>
        /// Rewrites the constraints.
        /// </summary>
        /// <param name="body">The body of the constraint.</param>
        /// <returns>The fixed constraint.</returns>
        internal static string RewriteConstraint(string body)
        {
            Regex expression = new Regex(@"( CHECK\s*\(.+\))|( DEFAULT\s*\(.+\))");
            string newbody = expression.Replace(body, string.Empty);

            return newbody;
        }

        /// <summary>
        /// Rewries the identity.
        /// </summary>
        /// <param name="body">The body of the identity.</param>
        /// <returns>The fixed identity.</returns>
        internal static string RewriteIdentity(string body)
        {
            Regex expression = new Regex(@" IDENTITY\(.+\)");
            string newbody = expression.Replace(body, string.Empty);

            return newbody;
        }

        /// <summary>
        /// Get the hard coded precision value.
        /// </summary>
        /// <param name="type">The type of the property.</param>
        /// <returns>The value for the precision.</returns>
        private static int GetPrecision(Config.DataType type)
        {
            switch (type)
            {
                case Config.DataType.@decimal:
                    return 19;
                case Config.DataType.datetimeoffset:
                    return 7;
                default:
                    return default(int);
            }
        }

        /// <summary>
        /// Get the hard coded scale value.
        /// </summary>
        /// <param name="type">The type of the property.</param>
        /// <returns>The value for the precision.</returns>
        private static int GetScale(Config.DataType type)
        {
            switch (type)
            {
                case Config.DataType.@decimal:
                    return 9;
                default:
                    return default(int);
            }
        }

        /// <summary>
        /// Convert Property types to sql types.
        /// </summary>
        /// <param name="typeName">The property type name.</param>
        /// <returns>The corresponding sql type name.</returns>
        private static string Convert(Config.DataType typeName)
        {
            switch(typeName)
            {
                case Config.DataType.@string:
                    return "nvarchar";
                case Config.DataType.@long:
                    return "bigint";
                case Config.DataType.@short:
                    return "smallint";
                case Config.DataType.guid:
                    return "uniqueidentifier";
                case Config.DataType.@double:
                    return "float";
                case Config.DataType.@float:
                    return "real";
                case Config.DataType.@byte:
                    return "tinyint";
                case Config.DataType.@bool:
                    return "bit";
                case Config.DataType.@char:
                    return "nchar";
                default:
                    return typeName.ToString();
            }
        }

        /// <summary>
        /// Prepares the identity.
        /// </summary>
        /// <returns>The prepared identity.</returns>
        private string PrepareIdentity()
        {
            if (this.Seed.HasValue == false)
            {
                this.Seed = 1;
            }

            if (this.Increment.HasValue == false)
            {
                this.Increment = 1;
            }

            return string.Format(CultureInfo.InvariantCulture, "IDENTITY({0}, {1})", this.Seed, this.Increment);
        }

        /// <summary>
        /// Prepares the length.
        /// </summary>
        /// <returns>The prepared length.</returns>
        private string PrepareLength()
        {
            switch (this.TypeName)
            {
                case "char":
                case "varchar":
                case "varbinary":
                    return "(" + (this.MaxLength == -1 ? "max" : this.MaxLength.ToString(CultureInfo.InvariantCulture)) + ")";
                case "nchar":
                case "nvarchar":
                    return "(" + (this.MaxLength == -1 ? "max" : this.MaxLength.ToString(CultureInfo.InvariantCulture)) + ")";
                case "numeric":
                case "decimal":
                    return "(" + this.Precision + "," + this.Scale + ")";
                case "datetime2":
                case "datetimeoffset":
                case "time":
                    return "(" + this.Precision + ")";
                default:
                    return string.Empty;
            }
        }
    }
}
