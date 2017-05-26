// -----------------------------------------------------------------------
// <copyright file="Container.cs" company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using Ninject;
    using NP = Ninject.Parameters;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Container
    {
        /// <summary>
        /// The ninject kernel.
        /// </summary>
        private static IKernel kernel;

        /// <summary>
        /// Initialize the Ioc Container.
        /// </summary>
        /// <param name="initial">The kernel to use.</param>
        public static void Initialize(IKernel initial)
        {
            kernel = initial;
        }

        /// <summary>
        /// Get a contained instance of a given component type.
        /// </summary>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The instance currently in scope.</returns>
        public static T Get<T>(params NP.IParameter[] parameters)
            where T : class
        {
            return kernel.Get<T>(parameters);
        }

        /// <summary>
        /// Get a contained instance of a given component type.
        /// </summary>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The instance currently in scope, null if not found.</returns>
        public static T TryGet<T>(params NP.IParameter[] parameters)
            where T : class
        {
            return kernel.TryGet<T>(parameters);
        }
    }
}
