// -----------------------------------------------------------------------
// <copyright file="IUserInRoleHelper.cs" Company="Lensgrinder, Ltd.">
//     Copyright (C) Lensgrinder, Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    /// <summary>
    /// Helper interface to check whether current user is in the provided role.
    /// </summary>
    public interface IUserInRoleHelper
    {
        /// <summary>
        /// Check that current user is a member of the provided role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        void CheckCurrentUser(string role);
    }
}
