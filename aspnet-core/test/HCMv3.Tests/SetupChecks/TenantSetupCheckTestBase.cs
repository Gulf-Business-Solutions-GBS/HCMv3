using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Castle.MicroKernel.Registration;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.MultiTenancy;
using HCMv3.Releases;
using HCMv3.SetupChecks;

namespace HCMv3.Tests.SetupChecks;

/// <summary>
/// What's New &amp; Setup tests run against a made-up release history (<see cref="FakeReleases"/>) instead of the real
/// one, which starts empty: 3.0.0 lists nothing, 3.1.0 adds a feature needing no setup, one needing a currency, and
/// one whose check fails, and 3.2.0 adds one more needing a currency. Each test gets fresh, empty tenants.
/// </summary>
public abstract class TenantSetupCheckTestBase : AppTestBase
{
    protected const string V300 = "3.0.0";
    protected const string V310 = "3.1.0";
    protected const string V320 = "3.2.0";

    protected TenantSetupCheckTestBase()
    {
        LocalIocManager.Register<CurrencyExistsCheck>(Abp.Dependency.DependencyLifeStyle.Transient);
        LocalIocManager.Register<ThrowingCheck>(Abp.Dependency.DependencyLifeStyle.Transient);
        LocalIocManager.IocContainer.Register(
            Component.For<IReleaseCatalog>().Instance(new FakeReleases()).IsDefault().Named("FakeReleaseCatalog"));
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

    protected void AddCurrency(int tenantId)
    {
        UsingDbContext(tenantId, context =>
            context.Set<Currency>().Add(new Currency { TenantId = tenantId, Code = "KWD", NameEn = "Dinar", NameAr = "Dinar" }));
    }

    /// <summary>
    /// Signs in as an administrator of <paramref name="tenantId"/>, created on first use, who holds the What's New &amp;
    /// Setup permission (or not, to test refusal). The screen always works on the signed-in tenant.
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
                    TenantId = tenantId, UserId = user.Id, Name = AppPermissions.Pages_Administration_TenantSetupCheck, IsGranted = true,
                });
                context.SaveChanges();
            }

            return user.Id;
        });

        AbpSession.TenantId = tenantId;
        AbpSession.UserId = userId;
    }

    /// <summary>The made-up release history the tests run against.</summary>
    protected class FakeReleases : IReleaseCatalog
    {
        private static readonly string[] None = Array.Empty<string>();

        public IReadOnlyList<HCMv3Release> Releases { get; } = new List<HCMv3Release>
        {
            new(V300, new DateTime(2026, 9, 1), "R300", "R300Notes", Array.Empty<HCMv3ReleaseFeature>()),
            new(V310, new DateTime(2026, 10, 1), "R310", "R310Notes", new List<HCMv3ReleaseFeature>
            {
                new("310-plain", "Workflow", "F310Plain", "F310PlainText", None),
                new("310-currency", "Payroll", "F310Currency", "F310CurrencyText", new[] { CurrencyExistsCheck.CheckCode }),
                new("310-broken", "Payroll", "F310Broken", "F310BrokenText", new[] { ThrowingCheck.CheckCode }),
                new("310-unregistered", "Payroll", "F310Ghost", "F310GhostText", new[] { "TST-999" }),
            }),
            new(V320, new DateTime(2026, 11, 1), "R320", "R320Notes", new List<HCMv3ReleaseFeature>
            {
                new("320-currency", "Payroll", "F320Currency", "F320CurrencyText", new[] { CurrencyExistsCheck.CheckCode }),
            }),
        };

        public IReadOnlyList<Type> SetupCheckTypes { get; } = new[] { typeof(CurrencyExistsCheck), typeof(ThrowingCheck) };
    }

    /// <summary>A stand-in check: the tenant needs at least one currency.</summary>
    public class CurrencyExistsCheck : ISetupCheck
    {
        public const string CheckCode = "TST-001";

        private readonly IRepository<Currency> _currencies;

        public CurrencyExistsCheck(IRepository<Currency> currencies)
        {
            _currencies = currencies;
        }

        public string Code => CheckCode;
        public string IntroducedIn => V310;
        public SetupCheckSeverity Severity => SetupCheckSeverity.Blocker;
        public string FixScreenCode => "SGM015";

        public Task<SetupCheckFinding> RunAsync(SetupCheckContext context) =>
            Task.FromResult(SetupCheckFinding.Exists(_currencies.GetAll().Any()));
    }

    /// <summary>A stand-in check that always fails to run.</summary>
    public class ThrowingCheck : ISetupCheck
    {
        public const string CheckCode = "TST-002";

        public string Code => CheckCode;
        public string IntroducedIn => V310;
        public SetupCheckSeverity Severity => SetupCheckSeverity.Warning;
        public string FixScreenCode => "SGM015";

        public Task<SetupCheckFinding> RunAsync(SetupCheckContext context) => throw new InvalidOperationException("boom");
    }
}
