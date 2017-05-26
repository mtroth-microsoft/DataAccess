// -----------------------------------------------------------------------
// <copyright file="CacheNotReadyException.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// <summary>The file summary.</summary>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class to handle cases where a Cache is not yet in a readable state.
    /// </summary>
    public sealed class CacheNotReadyException : InvalidOperationException
    {
        /// <summary>
        /// Initializes an instance of the CacheNotReadyException class.
        /// </summary>
        public CacheNotReadyException()
            : base()
        {
        }

        /// <summary>
        /// Initializes an instance of the CacheNotReadyException class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public CacheNotReadyException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes an instance of the CacheNotReadyException class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception if applicable.</param>
        public CacheNotReadyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes an instance of the CacheNotReadyException class.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">The streaming context.</param>
        public CacheNotReadyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
