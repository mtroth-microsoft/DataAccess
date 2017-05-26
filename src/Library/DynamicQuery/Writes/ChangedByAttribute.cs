// -----------------------------------------------------------------------
// <copyright file="ChangedByAttribute.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Class for the changed by attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ChangedByAttribute : Attribute
    {
    }
}
