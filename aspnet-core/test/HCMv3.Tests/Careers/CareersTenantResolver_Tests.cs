using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.Careers;
using HCMv3.Configuration;
using HCMv3.MultiTenancy;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Careers;

/// <summary>
/// Isolation tests for subdomain -> tenant resolution on the PUBLIC careers portal.
///
/// The failure mode these guard against: an anonymous request has no <c>AbpSession.TenantId</c>, and in ABP
/// a null session tenant means "host", which disables the <c>MustHaveTenant</c> filter. So any code path
/// that returns "no tenant" and is then treated as "host" leaks every tenant's data on a public page. Every
/// test below asserts the resolver returns <c>null</c> (i.e. reject) rather than something the caller could
/// mistake for a valid tenant.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CareersTenantResolver_Tests
{
    private const string DomainFormat = "{TENANCY_NAME}.careers.example.com";

    private readonly List<Tenant> _tenants = new List<Tenant>
    {
        NewTenant(1, "acme", "Acme Corp"),
        NewTenant(2, "globex", "Globex"),
        NewTenant(3, "dormant", "Dormant Inc", isActive: false)
    };

    private static Tenant NewTenant(int id, string tenancyName, string name, bool isActive = true)
    {
        return new Tenant(tenancyName, name) { Id = id, IsActive = isActive };
    }

    private CareersTenantResolver CreateResolver(string hostName, string domainFormat = DomainFormat)
    {
        var hostNameProvider = Substitute.For<ICareersHostNameProvider>();
        hostNameProvider.GetHostNameOrNull().Returns(hostName);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { CareersTenantResolver.DomainFormatConfigurationKey, domainFormat }
            })
            .Build();

        var configurationAccessor = Substitute.For<IAppConfigurationAccessor>();
        configurationAccessor.Configuration.Returns(configuration);

        var tenantRepository = Substitute.For<IRepository<Tenant>>();
        tenantRepository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Tenant, bool>>>())
            .Returns(call => Task.FromResult(
                _tenants.FirstOrDefault(call.Arg<Expression<Func<Tenant, bool>>>().Compile())));

        return new CareersTenantResolver(hostNameProvider, configurationAccessor, tenantRepository);
    }

    [Fact]
    public async Task Should_Resolve_Tenant_From_Its_Own_Subdomain()
    {
        var resolver = CreateResolver("acme.careers.example.com");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Resolve_Each_Subdomain_To_A_Different_Tenant()
    {
        (await CreateResolver("acme.careers.example.com").ResolveTenantIdOrNullAsync()).ShouldBe(1);
        (await CreateResolver("globex.careers.example.com").ResolveTenantIdOrNullAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task Should_Ignore_Port_And_Scheme_In_Host_Name()
    {
        var resolver = CreateResolver("acme.careers.example.com:44303");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Resolve_Unknown_Subdomain()
    {
        var resolver = CreateResolver("nosuchtenant.careers.example.com");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Resolve_When_Subdomain_Is_Missing()
    {
        // Apex domain, no tenant segment at all. Must NOT silently become host/null-tenant "see everything".
        var resolver = CreateResolver("careers.example.com");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Resolve_Www_As_A_Tenant()
    {
        var resolver = CreateResolver("www.careers.example.com");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Resolve_Completely_Unrelated_Host()
    {
        var resolver = CreateResolver("evil.example.org");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Resolve_A_Deactivated_Tenant()
    {
        var resolver = CreateResolver("dormant.careers.example.com");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Fail_Closed_When_Domain_Format_Is_Not_Configured()
    {
        var resolver = CreateResolver("acme.careers.example.com", domainFormat: null);

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_A_Domain_Format_Without_A_Tenancy_Name_Placeholder()
    {
        // A placeholder-less format would match the apex for every request -- i.e. "any tenant".
        var resolver = CreateResolver("careers.example.com", domainFormat: "careers.example.com");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Fail_Closed_When_There_Is_No_Http_Request()
    {
        var resolver = CreateResolver(hostName: null);

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Support_Multiple_Semicolon_Separated_Domain_Formats()
    {
        var resolver = CreateResolver(
            "globex.jobs.example.net",
            domainFormat: "{TENANCY_NAME}.careers.example.com;{TENANCY_NAME}.jobs.example.net");

        (await resolver.ResolveTenantIdOrNullAsync()).ShouldBe(2);
    }
}
