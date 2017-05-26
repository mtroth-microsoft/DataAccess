// -----------------------------------------------------------------------
// <copyright file="InsertedTimeAttribute.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Class for the inserted time attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class InsertedTimeAttribute : Attribute
    {
    }
}
