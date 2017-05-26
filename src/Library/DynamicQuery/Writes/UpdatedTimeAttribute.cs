// -----------------------------------------------------------------------
// <copyright file="UpdatedTimeAttribute.cs" company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;

    /// <summary>
    /// Class for the updated time attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class UpdatedTimeAttribute : Attribute
    {
    }
}
