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
    }
}

