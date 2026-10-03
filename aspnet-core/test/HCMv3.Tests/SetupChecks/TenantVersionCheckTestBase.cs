using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Castle.MicroKernel.Registration;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.MultiTenancy;
using HCMv3.SetupChecks;

namespace HCMv3.Tests.SetupChecks;

/// <summary>
/// Tenant Version Check tests run against a made-up change log of five builds (<see cref="LogJson"/>) in place of the
/// real one read from git, and a made-up list of applied database changes. Each test gets fresh, empty tenants.
/// </summary>
public abstract class TenantVersionCheckTestBase : AppTestBase
{
    protected const string AppliedMigration = "20250101000000_Old_One";
    protected const string MissingMigration = "20260101000000_Added_Geo_Punch";

    /// <summary>
    /// Build 5 is installed. Build 5 brought a permission, a screen and a database change; build 4 a manual step, a
    /// host-only permission (not a tenant's business), a tenant setting and an older database change; build 2 the
    /// same permission as build 5 and a screen that no longer exists.
    /// </summary>
    public const string LogJson = @"{
      ""baseVersion"": ""3.0"", ""version"": ""3.0.5"", ""number"": 5, ""commit"": ""abc"", ""dirty"": false,
      ""builtOn"": ""2026-10-01T10:00:00Z"",
      ""changes"": [
        { ""n"": 5, ""hash"": ""e5"", ""date"": ""2026-09-30"", ""subject"": ""Add geo punch"", ""module"": ""Ess"", ""setup"": [
            { ""kind"": ""Permission"", ""text"": ""Pages.Administration.TenantVersionCheck"" },
            { ""kind"": ""Screen"", ""text"": ""SGM015"" },
            { ""kind"": ""Database"", ""text"": ""20260101000000_Added_Geo_Punch"" } ] },
        { ""n"": 4, ""hash"": ""d4"", ""date"": ""2026-09-20"", ""subject"": ""A default workflow chain"", ""module"": ""ZzUnnamedModule"", ""setup"": [
            { ""kind"": ""Manual"", ""text"": ""Create a default workflow chain"" },
            { ""kind"": ""Permission"", ""text"": ""Pages.Tenants"" },
            { ""kind"": ""Setting"", ""text"": ""App.Releases.LastSeenVersion"" },
            { ""kind"": ""Database"", ""text"": ""20250101000000_Old_One"" } ] },
        { ""n"": 3, ""hash"": ""c3"", ""date"": ""2026-09-10"", ""subject"": ""Fix a typo"", ""module"": """", ""setup"": [
            { ""kind"": ""Screen"", ""text"": ""SGEN012"" } ] },
        { ""n"": 2, ""hash"": ""b2"", ""date"": ""2026-09-05"", ""subject"": ""Earlier work"", ""module"": ""Payroll"", ""setup"": [
            { ""kind"": ""Permission"", ""text"": ""Pages.Administration.TenantVersionCheck"" },
            { ""kind"": ""Screen"", ""text"": ""SZZ999"" } ] },
        { ""n"": 1, ""hash"": ""a1"", ""date"": ""2026-09-01"", ""subject"": ""Start"", ""module"": """", ""setup"": [] }
      ] }";

    protected readonly FakeAppliedMigrations Migrations = new();

    protected TenantVersionCheckTestBase()
    {
        LocalIocManager.IocContainer.Register(
            Component.For<IReleaseLog>().Instance(new ReleaseLog(LogJson)).IsDefault().Named("FakeReleaseLog"),
            Component.For<IAppliedMigrationsReader>().Instance(Migrations).IsDefault().Named("FakeAppliedMigrations"));
    }

    protected int CreateEmptyTenant(string tenancyName)
    {
        return UsingDbContext(null, context =>
        {
            var tenant = context.Tenants.Add(new Tenant(tenancyName, tenancyName + " Co") { IsActive = true }).Entity;
            context.SaveChanges();
            return tenant.Id;
        });
    }

    /// <summary>A role in the tenant. The static Admin role holds every permission; any other only what it is granted.</summary>
    protected void CreateRole(int tenantId, string name, params string[] grantedPermissions)
    {
        UsingDbContext(tenantId, context =>
        {
            var role = new Role(tenantId, name, name) { IsStatic = name == StaticRoleNames.Tenants.Admin };
            role.SetNormalizedName();
            context.Roles.Add(role);
            context.SaveChanges();

            foreach (var permission in grantedPermissions)
            {
                context.RolePermissions.Add(new RolePermissionSetting { TenantId = tenantId, RoleId = role.Id, Name = permission, IsGranted = true });
            }
        });
    }

    /// <summary>
    /// Signs in as an administrator of <paramref name="tenantId"/>, created on first use, who holds the Tenant Setup
    /// Check permission (or not, to test refusal). The screen always works on the signed-in tenant.
    /// </summary>
    protected void LoginAsAdminOf(int tenantId, bool withPermission = true)
    {
        var userId = UsingDbContext(tenantId, context =>
        {
            var existing = context.Users.FirstOrDefault(u => u.TenantId == tenantId && u.UserName == "setupadmin");
            if (existing != null)
            {
                return existing.Id;
            }

            var user = new User
            {
                TenantId = tenantId,
                UserName = "setupadmin",
                Name = "Setup",
                Surname = "Admin",
                EmailAddress = $"setupadmin{tenantId}@example.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw==",
            };
            user.SetNormalizedNames();
            context.Users.Add(user);
            context.SaveChanges();

            if (withPermission)
            {
                context.UserPermissions.Add(new UserPermissionSetting
                {
                    TenantId = tenantId, UserId = user.Id, Name = AppPermissions.Pages_Administration_TenantVersionCheck, IsGranted = true,
                });
                context.SaveChanges();
            }

            return user.Id;
        });

        AbpSession.TenantId = tenantId;
        AbpSession.UserId = userId;
    }

    /// <summary>Stands in for the database's migration history. Null: the database keeps none.</summary>
    protected class FakeAppliedMigrations : IAppliedMigrationsReader
    {
        public IReadOnlyCollection<string> Applied { get; set; } = new[] { AppliedMigration };

        public Task<IReadOnlyCollection<string>> GetAppliedAsync() => Task.FromResult(Applied);
    }
}
