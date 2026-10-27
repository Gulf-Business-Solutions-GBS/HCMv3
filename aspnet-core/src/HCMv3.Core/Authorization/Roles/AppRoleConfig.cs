using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace HCMv3.Authorization.Roles;

public static class AppRoleConfig
{
    public static void Configure(IRoleManagementConfig roleManagementConfig)
    {
        //Static host roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.Admin,
                MultiTenancySides.Host,
                grantAllPermissionsByDefault: true)
            );

        //Static tenant roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Admin,
                MultiTenancySides.Tenant,
                grantAllPermissionsByDefault: true)
            );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.User,
                MultiTenancySides.Tenant)
            );

        // Careers-portal candidates. grantAllPermissionsByDefault is left false -- a candidate role must
        // start with zero permissions. Registered here (rather than only in TenantRoleAndUserBuilder) so
        // TenantManager.CreateStaticRoles creates it for every NEW tenant too.
        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Candidate,
                MultiTenancySides.Tenant)
            );

        // Employee self service. grantAllPermissionsByDefault stays false -- the Ess.* grants are applied
        // explicitly by EmployeeRoleBuilder, because "all permissions" would include every Pages.* admin
        // screen. Registered here as well as in the seeder so TenantManager.CreateStaticRoles creates it
        // for every NEW tenant too.
        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Employee,
                MultiTenancySides.Tenant)
            );
    }
}

