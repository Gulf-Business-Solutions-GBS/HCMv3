using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using HCMv3.Authentication.TwoFactor.Google;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.Editions;
using HCMv3.MultiTenancy;

namespace HCMv3.Identity;

public static class IdentityRegistrar
{
    public static IdentityBuilder Register(IServiceCollection services)
    {
        services.AddLogging();

        return services.AddAbpIdentity<Tenant, User, Role>(options =>
            {
                options.Tokens.ProviderMap[GoogleAuthenticatorProvider.Name] = new TokenProviderDescriptor(typeof(GoogleAuthenticatorProvider));
            })
            .AddAbpTenantManager<TenantManager>()
            .AddAbpUserManager<UserManager>()
            .AddAbpRoleManager<RoleManager>()
            .AddAbpEditionManager<EditionManager>()
            .AddAbpUserStore<UserStore>()
            .AddAbpRoleStore<RoleStore>()
            .AddAbpSignInManager<SignInManager>()
            .AddAbpUserClaimsPrincipalFactory<UserClaimsPrincipalFactory>()
            .AddAbpSecurityStampValidator<SecurityStampValidator>()
            .AddPermissionChecker<PermissionChecker>()
            .AddDefaultTokenProviders();
    }
}

