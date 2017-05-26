// -----------------------------------------------------------------------
// <copyright file="IMessageLogger.cs" company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Interface for the logger.
    /// </summary>
    public interface IMessageLogger
    {
        /// <summary>
        /// Get the location of the logging directory.
        /// </summary>
        string LoggingDirectory { get;  }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments.</param>
        void Info(string message, params object[] args);

        /// <summary>
        /// Log a warning..
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments.</param>
        void Warn(string message, params object[] args);

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments.</param>
        void Error(Exception exception, string message, params object[] args);

        /// <summary>
        /// A helper function to fire the qos event.
        /// </summary>
        /// <param name="transactionContext">The transaction context.</param>
        /// <param name="apiId">The api for the event.</param>
        /// <param name="duration">The time span.</param>
        /// <param name="exception">The exception encountered.</param>
        void FireQosEvent(string transactionContext, string apiId, TimeSpan elapsed, Exception exception);
    }
}
