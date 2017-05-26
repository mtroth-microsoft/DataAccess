// -----------------------------------------------------------------------
// <copyright file="InvalidDataFilterException.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// <summary>The file summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class to handle cases where data filters are invalid.
    /// </summary>
    public sealed class InvalidDataFilterException : InvalidOperationException
    {
        /// <summary>
        /// Initializes an instance of the InvalidDataFilterException class.
        /// </summary>
        public InvalidDataFilterException()
            : base()
        {
        }

        /// <summary>
        /// Initializes an instance of the InvalidDataFilterException class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public InvalidDataFilterException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes an instance of the InvalidDataFilterException class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception if applicable.</param>
        public InvalidDataFilterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes an instance of the InvalidDataFilterException class.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">The streaming context.</param>
        public InvalidDataFilterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
