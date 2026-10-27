using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Abp.Dependency;
using Flurl.Http;

namespace HCMv3.ApiClient;

public class FlurlHttpErrorHandler
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private const string AuthorizationScheme = "Bearer";

    public Func<Task> OnSessionTimeOut { get; set; }

    public Func<string, string, Task> OnAccessTokenRefresh { get; set; }

    public FlurlHttpErrorHandler()
    {

    }

    public async Task Handle(FlurlCall call)
    {
        if (call.Response.StatusCode == (int)HttpStatusCode.Unauthorized && HasBearerAuthorizationHeader(call.Request))
        {
            await HandleUnauthorizedResponse(call);
        }
    }


    private async Task HandleUnauthorizedResponse(FlurlCall call)
    {
        await _semaphore.WaitAsync();

        try
        {
            var tokenManager = IocManager.Instance.IocContainer.Resolve<IAccessTokenManager>();

            if (tokenManager.IsRefreshTokenExpired)
            {
                await HandleSessionExpired(tokenManager);
            }
            else
            {
                await RefreshAccessTokenAndSendRequestAgain(call, tokenManager);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshAccessTokenAndSendRequestAgain(FlurlCall call, IAccessTokenManager tokenManager)
    {
        await RefreshToken(tokenManager, call.Request);
        call.Response = await call.Request.SendAsync(call.HttpRequestMessage.Method, call.Request.Content);
    }

    private async Task HandleSessionExpired(IAccessTokenManager tokenManager)
    {
        tokenManager.Logout();

        if (OnSessionTimeOut != null)
        {
            await OnSessionTimeOut();
        }
    }

    private async Task RefreshToken(IAccessTokenManager tokenManager, IFlurlRequest request)
    {
        var newTokens = await tokenManager.RefreshTokenAsync();

        if (newTokens.HasValue)
        {
            if (OnAccessTokenRefresh != null)
            {
                await OnAccessTokenRefresh(newTokens.Value.accessToken, newTokens.Value.encryptedAccessToken);
            }

            SetRequestAccessToken(newTokens.Value.accessToken, request);
        }
        else
        {
            await HandleSessionExpired(tokenManager);
        }
    }

    private static bool HasBearerAuthorizationHeader(IFlurlRequest request)
    {
        if (!request.Headers.Contains("Authorization"))
        {
            return false;
        }

        request.Headers.TryGetFirst("Authorization", out string auth_header);
        if (!auth_header.StartsWith(AuthorizationScheme))
        {
            return false;
        }

        return true;
    }

    private static void SetRequestAccessToken(string accessToken, IFlurlRequest request)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ApplicationException("Cannot handle empty access token!");
        }

        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", new AuthenticationHeaderValue(AuthorizationScheme, accessToken).ToString());
    }
}

