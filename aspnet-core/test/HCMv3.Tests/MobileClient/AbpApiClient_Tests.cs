using System.Threading.Tasks;
using Abp.Configuration.Startup;
using Flurl.Http;
using Flurl.Http.Testing;
using HCMv3.ApiClient;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.MobileClient;

public class AbpApiClient_Tests
{
    private class ConfigDto
    {
        public string Name { get; set; }
    }

    private static AbpApiClient CreateClient()
    {
        var multiTenancyConfig = Substitute.For<IMultiTenancyConfig>();
        multiTenancyConfig.TenantIdResolveKey.Returns("Abp.TenantId");
        return new AbpApiClient(Substitute.For<IAccessTokenManager>(), Substitute.For<IApplicationContext>(), multiTenancyConfig);
    }

    [Fact]
    public async Task Should_Report_A_Timeout_Instead_Of_Returning_Nothing()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateTimeout();

        await Should.ThrowAsync<FlurlHttpTimeoutException>(() => CreateClient().GetAnonymousAsync<ConfigDto>("AbpUserConfiguration/GetAll"));
    }

    [Fact]
    public async Task Should_Report_A_Failed_Connection_Instead_Of_Returning_Nothing()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateException(new System.Net.Http.HttpRequestException("No connection could be made."));

        await Should.ThrowAsync<FlurlHttpException>(() => CreateClient().GetAnonymousAsync<ConfigDto>("AbpUserConfiguration/GetAll"));
    }

    [Fact]
    public async Task Should_Return_The_Unwrapped_Result_When_The_Server_Answers()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { result = new { name = "ok" }, success = true, __abp = true });

        var result = await CreateClient().GetAnonymousAsync<ConfigDto>("AbpUserConfiguration/GetAll");

        result.Name.ShouldBe("ok");
    }
}
