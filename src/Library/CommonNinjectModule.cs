// -----------------------------------------------------------------------
// <copyright file="CommonNinjectModule.cs" company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using Ninject.Modules;

    /// <summary>
    /// Ninject Module for DI support.
    /// </summary>
    public class CommonNinjectModule : NinjectModule
    {
        /// <summary>
        /// Load override to initialize the module.
        /// </summary>
        public override void Load()
        {
        }
    }
}
