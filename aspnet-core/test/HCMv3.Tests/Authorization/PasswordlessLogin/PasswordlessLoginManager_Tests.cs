using Abp;
using Abp.Runtime.Caching;
using Abp.UI;
using HCMv3.Authentication.PasswordlessLogin;
using HCMv3.Authorization.PasswordlessLogin;
using HCMv3.Authorization.Users;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HCMv3.Tests.Authorization.PasswordlessLogin;

public class PasswordlessLoginManager_Tests : PasswordlessLognTestBase
{
    private readonly IPasswordlessLoginManager _passwordlessLoginManager;
    private readonly UserManager _userManager;
    private readonly IUserRepository _userRepository;
    private readonly ICacheManager _cacheManager;


    public PasswordlessLoginManager_Tests()
    {
        _passwordlessLoginManager = Resolve<PasswordlessLoginManager>();
        _userManager = Resolve<UserManager>();
        _userRepository = Resolve<IUserRepository>();
        _cacheManager = Resolve<ICacheManager>();

        SetMockCode();
    }

    [Fact]
    public async Task Get_User_By_Passwordless_Provider_Should_Return_User_By_Email()
    {
        await CreateAndSetUser();

        var provider = PasswordlessLoginProviderType.Email.ToString();

        var userFromPasswordlessLoginManager =
            await _passwordlessLoginManager.GetUserByPasswordlessProviderAndKeyAsync(
                provider,
                ProviderKeyEmail
            );

        var userFromUserManager = await _userManager.FindByEmailAsync(ProviderKeyEmail);

        Assert.NotNull(userFromPasswordlessLoginManager);

        userFromPasswordlessLoginManager.ShouldBeEquivalentTo(userFromUserManager);
    }

    [Fact]
    public async Task Get_User_By_Passwordless_Provider_Should_Return_User_By_Sms()
    {
        await CreateAndSetUser();

        var provider = PasswordlessLoginProviderType.Sms.ToString();

        var userFromPasswordlessLoginManager =
            await _passwordlessLoginManager.GetUserByPasswordlessProviderAndKeyAsync(provider,
                ProviderKeyPhoneNumber);

        var userFromUserRepository = await _userRepository.FindByPhoneNumberAsync(ProviderKeyPhoneNumber);

        Assert.NotNull(userFromPasswordlessLoginManager);
        userFromPasswordlessLoginManager.ShouldBeEquivalentTo(userFromUserRepository);
    }

    [Fact]
    public async Task Get_User_By_Passwordless_Provider_Should_Throw_Exception_When_User_Not_Found()
    {
        await CreateAndSetUser();

        var provider = "NonExistingProvider";

        await Assert.ThrowsAsync<AbpException>(async () =>
        {
            await _passwordlessLoginManager.GetUserByPasswordlessProviderAndKeyAsync(
                provider,
                ProviderKeyEmail
            );
        });
    }

    [Fact]
    public async Task Should_Throw_Exception_When_Code_Not_Found_In_Cache()
    {
        await CreateAndSetUser();

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _passwordlessLoginManager.VerifyPasswordlessLoginCode(
                DefaultTenantId,
                ProviderKeyEmail,
                MockCode
            );
        });
    }

    [Fact]
    public async Task Should_Throw_UserFriendlyException_When_Code_Does_Not_Match()
    {
        await CreateAndSetUser();

        string wrongCode = "54321";

        var cacheItem = new PasswordlessLoginCodeCacheItem
        {
            Code = wrongCode
        };

        await _cacheManager.GetPasswordlessVerificationCodeCache().SetAsync(
            GetPasswordlessLoginCodeCacheKey(DefaultTenantId, ProviderKeyEmail),
            cacheItem
        );

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _passwordlessLoginManager.VerifyPasswordlessLoginCode(
                DefaultTenantId,
                ProviderKeyEmail,
                MockCode
            );
        });
    }

    [Fact]
    public async Task Should_Not_Throw_Exception_When_Code_Matches()
    {
        await CreateAndSetUser();

        var cacheItem = new PasswordlessLoginCodeCacheItem
        {
            Code = MockCode
        };

        await _cacheManager.GetPasswordlessVerificationCodeCache().SetAsync(
            GetPasswordlessLoginCodeCacheKey(DefaultTenantId, ProviderKeyEmail),
            cacheItem);

        await _passwordlessLoginManager.VerifyPasswordlessLoginCode(DefaultTenantId, ProviderKeyEmail, MockCode);
    }

    [Fact]
    public async Task Should_Return_Valid_Code_And_Store_In_Cache()
    {
        await CreateAndSetUser();

        var generatedCode = await _passwordlessLoginManager.GeneratePasswordlessLoginCode(
            DefaultTenantId,
            ProviderKeyEmail
        );

        Assert.NotNull(generatedCode);

        var cachedCode = await _cacheManager.GetPasswordlessVerificationCodeCache().GetOrDefaultAsync(
            GetPasswordlessLoginCodeCacheKey(DefaultTenantId, ProviderKeyEmail)
        );

        Assert.NotNull(cachedCode);
        Assert.Equal(generatedCode, cachedCode.Code);
    }

    [Fact]
    public async Task Should_Overwrite_Old_Code_With_New_One_In_Cache()
    {
        await CreateAndSetUser();

        var oldCode = await _passwordlessLoginManager.GeneratePasswordlessLoginCode(
            DefaultTenantId,
            ProviderKeyEmail
        );

        var newCode = await _passwordlessLoginManager.GeneratePasswordlessLoginCode(
            DefaultTenantId,
            ProviderKeyEmail
        );

        Assert.NotEqual(oldCode, newCode);

        var cachedCode = await _cacheManager.GetPasswordlessVerificationCodeCache().GetOrDefaultAsync(
            GetPasswordlessLoginCodeCacheKey(DefaultTenantId, ProviderKeyEmail)
        );

        Assert.NotNull(cachedCode);
        Assert.Equal(newCode, cachedCode.Code);
    }

    [Fact]
    public async Task Should_Return_Valid_Code_When_TenantId_Null()
    {
        await CreateAndSetUser();

        var generatedCode = await _passwordlessLoginManager.GeneratePasswordlessLoginCode(
            null,
            ProviderKeyEmail
        );

        Assert.NotNull(generatedCode);

        var cachedCode = await _cacheManager.GetPasswordlessVerificationCodeCache().GetOrDefaultAsync(
            GetPasswordlessLoginCodeCacheKey(null, ProviderKeyEmail)
        );

        Assert.NotNull(cachedCode);
        Assert.Equal(generatedCode, cachedCode.Code);
    }

    [Fact]
    public async Task Should_Remove_Code_From_Cache()
    {
        await CreateAndSetUser();

        await _passwordlessLoginManager.GeneratePasswordlessLoginCode(DefaultTenantId, ProviderKeyEmail);

        await _passwordlessLoginManager.RemovePasswordlessLoginCode(DefaultTenantId, ProviderKeyEmail);

        var cachedCode = await _cacheManager.GetPasswordlessVerificationCodeCache().GetOrDefaultAsync(
            GetPasswordlessLoginCodeCacheKey(DefaultTenantId, ProviderKeyEmail)
        );

        Assert.Null(cachedCode);
    }

    [Fact]
    public async Task Should_Return_List_Of_SelectListItems()
    {
        await CreateAndSetUser();

        var providers = _passwordlessLoginManager.GetProviders();

        Assert.NotNull(providers);
        Assert.NotEmpty(providers);

        var expectedProviderItems = Enum.GetValues(typeof(PasswordlessLoginProviderType))
            .Cast<PasswordlessLoginProviderType>()
            .Select(providerType => Enum.GetName(typeof(PasswordlessLoginProviderType), providerType))
            .ToList();

        var providerTexts = providers.Select(p => p.Text).ToList();
        Assert.Equal(expectedProviderItems.OrderBy(p => p), providerTexts.OrderBy(p => p));
    }
}