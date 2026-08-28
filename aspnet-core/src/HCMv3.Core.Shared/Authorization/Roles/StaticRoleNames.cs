namespace HCMv3.Authorization.Roles;

public static class StaticRoleNames
{
    public static class Host
    {
        public const string Admin = "Admin";
    }

    public static class Tenants
    {
        public const string Admin = "Admin";

        public const string User = "User";

        /// <summary>
        /// Role held by careers-portal candidates. It is created with NO permissions and is never
        /// <c>IsDefault</c>, so it cannot pick up whatever a tenant admin has granted to the default
        /// <see cref="User"/> role. <c>CareersAccountAppService</c> overwrites a freshly registered
        /// candidate's roles with exactly this one for that reason.
        /// </summary>
        public const string Candidate = "Candidate";

        /// <summary>
        /// Role held by every employee who uses self service. Granted only <c>Ess.*</c> permissions, never
        /// <c>Pages.*</c>, which is what keeps the ~575-item admin menu invisible to them without a separate
        /// application. Deliberately NOT <c>IsDefault</c>: it must not be handed to every new user
        /// automatically, and it must not pick up whatever a tenant admin has granted to the default
        /// <see cref="User"/> role.
        ///
        /// <para>
        /// Holding this role is not the same as being an employee. Access to any actual record still
        /// requires the user to be linked to an <c>Employee</c> via <c>Employee.UserId</c>; an unlinked user
        /// with this role resolves to an empty employee scope and sees nothing.
        /// </para>
        /// </summary>
        public const string Employee = "Employee";
    }
}

