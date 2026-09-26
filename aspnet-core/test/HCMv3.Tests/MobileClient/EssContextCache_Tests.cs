using System.Threading.Tasks;
using HCMv3.ApiClient;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Sessions.Dto;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.MobileClient;

public class EssContextCache_Tests
{
    private readonly IEssSessionAppService _essSessionAppService = Substitute.For<IEssSessionAppService>();
    private readonly IApplicationContext _applicationContext = Substitute.For<IApplicationContext>();
    private readonly EssContextCache _cache;

    public EssContextCache_Tests()
    {
        _essSessionAppService.GetEssContext().Returns(
            _ => Task.FromResult(new EssContextDto { IsLinkedToEmployee = true }),
            _ => Task.FromResult(new EssContextDto { IsLinkedToEmployee = false }));
        SignIn(tenantId: 7, userId: 1001);
        _cache = new EssContextCache(_essSessionAppService, _applicationContext);
    }

    private void SignIn(int tenantId, long userId)
    {
        _applicationContext.LoginInfo.Returns(new GetCurrentLoginInformationsOutput
        {
            Tenant = new TenantLoginInfoDto { Id = tenantId },
            User = new UserLoginInfoDto { Id = userId }
        });
    }

    [Fact]
    public async Task Should_Ask_The_Server_Only_Once_For_The_Same_User()
    {
        var first = await _cache.GetAsync();
        var second = await _cache.GetAsync();

        second.ShouldBeSameAs(first);
        await _essSessionAppService.Received(1).GetEssContext();
    }

    [Fact]
    public async Task Should_Ask_Again_When_A_Refresh_Is_Requested()
    {
        await _cache.GetAsync();
        var refreshed = await _cache.GetAsync(refresh: true);

        refreshed.IsLinkedToEmployee.ShouldBeFalse();
        await _essSessionAppService.Received(2).GetEssContext();
    }

    [Fact]
    public async Task Should_Not_Reuse_Another_Users_Answer()
    {
        await _cache.GetAsync();
        SignIn(tenantId: 7, userId: 1002);

        var forNewUser = await _cache.GetAsync();

        forNewUser.IsLinkedToEmployee.ShouldBeFalse();
        await _essSessionAppService.Received(2).GetEssContext();
    }

    [Fact]
    public async Task Should_Ask_Again_After_Being_Cleared()
    {
        await _cache.GetAsync();
        _cache.Clear();

        await _cache.GetAsync();

        await _essSessionAppService.Received(2).GetEssContext();
    }
}
