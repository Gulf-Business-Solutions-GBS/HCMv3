using HCMv3.Authentication.PasswordlessLogin;
using HCMv3.Authorization.Accounts;
using HCMv3.Authorization.Accounts.Dto;
using System;
using System.Threading.Tasks;
using Xunit;

namespace HCMv3.Tests.Authorization.PasswordlessLogin;

public class AccountAppService_SendPasswordlessLoginCode_Tests : PasswordlessLognTestBase
{
    private readonly IAccountAppService _accountAppService;
    public AccountAppService_SendPasswordlessLoginCode_Tests()
    {
        _accountAppService = Resolve<IAccountAppService>();

        SetMockCode();
    }

    [Fact]
    public async Task Send_Email_Passwordless_Code()
    {
        try
        {
            await CreateAndSetUser();

            var input = new SendPasswordlessLoginCodeInput
            {
                ProviderType = PasswordlessLoginProviderType.Email,
                ProviderValue = ProviderKeyEmail
            };

            await _accountAppService.SendPasswordlessLoginCode(input);
        }
        catch (Exception exception)
        {

            throw new Exception("Send email passwordless code operation failed. Message:" + exception.Message);
        }
    }

    [Fact]
    public async Task Send_Sms_Passwordless_Code()
    {
        try
        {
            await CreateAndSetUser();

            var input = new SendPasswordlessLoginCodeInput
            {
                ProviderType = PasswordlessLoginProviderType.Sms,
                ProviderValue = ProviderKeyPhoneNumber
            };

            await _accountAppService.SendPasswordlessLoginCode(input);
        }
        catch (Exception exception)
        {
            throw new Exception("Send sms passwordless code operation failed. Message:" + exception.Message);
        }
    }
}
