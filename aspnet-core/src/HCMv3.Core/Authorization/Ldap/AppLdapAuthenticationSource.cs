using Abp.Zero.Ldap.Authentication;
using Abp.Zero.Ldap.Configuration;
using HCMv3.Authorization.Users;
using HCMv3.MultiTenancy;

namespace HCMv3.Authorization.Ldap;

public class AppLdapAuthenticationSource : LdapAuthenticationSource<Tenant, User>
{
    public AppLdapAuthenticationSource(ILdapSettings settings, IAbpZeroLdapModuleConfig ldapModuleConfig)
        : base(settings, ldapModuleConfig)
    {
    }
}

