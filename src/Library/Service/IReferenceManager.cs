// -----------------------------------------------------------------------
// <copyright Company="Lensgrinder, Ltd." file="IReferenceManager.cs">
//      Copyright (c) Lensgrinder, Ltd.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    public interface IReferenceManager
    {
        /// <summary>
        /// Add a reference to the host from the member.
        /// </summary>
        /// <typeparam name="T">The hosting instance type.</typeparam>
        /// <typeparam name="U">The member instance type.</typeparam>
        /// <param name="host">The host instance.</param>
        /// <param name="member">The member instance type.</param>
        /// <param name="propertyName">The name of the navigation property.</param>
        void CreateRef<T, U>(T host, U member, string propertyName);

        /// <summary>
        /// Remove a reference to the host from the member.
        /// </summary>
        /// <typeparam name="T">The hosting instance type.</typeparam>
        /// <typeparam name="U">The member instance type.</typeparam>
        /// <param name="host">The host instance.</param>
        /// <param name="member">The member instance type.</param>
        /// <param name="propertyName">The name of the navigation property.</param>
        void DeleteRef<T, U>(T host, U member, string propertyName);
    }
}
