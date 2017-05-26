// -----------------------------------------------------------------------
// <copyright file="Result.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System.Collections.Generic;

    internal sealed class Result
    {
        /// <summary>
        /// Initializes a new instance of the Result class.
        /// </summary>
        public Result()
        {
            this.Nodes = new List<Result>();
        }

        /// <summary>
        /// Gets the component id for the result.
        /// </summary>
        public string ComponentId { get; internal set; }

        /// <summary>
        /// Gets the member value for the result.
        /// </summary>
        public object Member { get; internal set; }

        /// <summary>
        /// Gets the list of nodes for the result.
        /// </summary>
        public List<Result> Nodes { get; private set; }

        /// <summary>
        /// Gets the path for the result.
        /// </summary>
        public string Path { get; internal set; }
    }
}
