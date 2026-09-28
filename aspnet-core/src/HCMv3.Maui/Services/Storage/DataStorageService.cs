using Abp.Dependency;
using Abp.ObjectMapping;
using HCMv3.ApiClient;
using HCMv3.ApiClient.Models;
using HCMv3.Maui.Core;
using HCMv3.Maui.Core.DataStorage;
using HCMv3.Maui.Models.Common;
using HCMv3.Sessions.Dto;

namespace HCMv3.Maui.Services.Storage;

public class DataStorageService : IDataStorageService, ISingletonDependency
{
    private readonly IDataStorageManager _dataStorageManager;
    private readonly IObjectMapper _objectMapper;

    public DataStorageService(
        IDataStorageManager dataStorageManager,
        IObjectMapper objectMapper)
    {
        _dataStorageManager = dataStorageManager;
        _objectMapper = objectMapper;
    }

    public async Task StoreAccessTokenAsync(string newAccessToken, string newEncryptedAccessToken)
    {
        var authenticateResult = _dataStorageManager.Retrieve<AuthenticateResultPersistanceModel>(MauiConsts.CurrentSessionTokenInfo);

        authenticateResult.AccessToken = newAccessToken;
        authenticateResult.EncryptedAccessToken = newEncryptedAccessToken;

        await _dataStorageManager.StoreAsync(MauiConsts.CurrentSessionTokenInfo, authenticateResult);
    }

    public AbpAuthenticateResultModel RetrieveAuthenticateResult()
    {
        var data = _dataStorageManager.Retrieve<AuthenticateResultPersistanceModel>(
            MauiConsts.CurrentSessionTokenInfo
        );

        return _objectMapper.Map<AbpAuthenticateResultModel>(
            data
        );
    }

    public async Task StoreAuthenticateResultAsync(AbpAuthenticateResultModel authenticateResultModel)
    {
        await _dataStorageManager.StoreAsync(
            MauiConsts.CurrentSessionTokenInfo,
            _objectMapper.Map<AuthenticateResultPersistanceModel>(authenticateResultModel)
        );
    }

    public TenantInformation RetrieveTenantInfo()
    {
        var stored = _dataStorageManager.Retrieve<TenantInformationPersistanceModel>(
            MauiConsts.CurrentSessionTenantInfo
        );

        // Nothing saved yet (first start, or the tenant was cleared): no tenant, not an error.
        return stored == null ? null : _objectMapper.Map<TenantInformation>(stored);
    }

    public async Task StoreTenantInfoAsync(TenantInformation tenantInfo)
    {
        await _dataStorageManager.StoreAsync(
            MauiConsts.CurrentSessionTenantInfo,
            _objectMapper.Map<TenantInformationPersistanceModel>(tenantInfo)
        );
    }

    public GetCurrentLoginInformationsOutput RetrieveLoginInfo()
    {
        return _objectMapper.Map<GetCurrentLoginInformationsOutput>(
            _dataStorageManager.Retrieve<CurrentLoginInformationPersistanceModel>(
                MauiConsts.CurrentSessionLoginInfo
            )
        );
    }

    public async Task StoreLoginInformationAsync(GetCurrentLoginInformationsOutput loginInfo)
    {
        await _dataStorageManager.StoreAsync(
            MauiConsts.CurrentSessionLoginInfo,
            _objectMapper.Map<CurrentLoginInformationPersistanceModel>(
                loginInfo
            )
        );
    }

    // Signing out forgets who was signed in, not which company: like the web app, the chosen tenant stays
    // so the next person on the phone does not have to pick it again.
    public void ClearSessionPersistance()
    {
        _dataStorageManager.RemoveIfExists(MauiConsts.CurrentSessionTokenInfo);
        _dataStorageManager.RemoveIfExists(MauiConsts.CurrentSessionLoginInfo);
    }

    public void ClearTenantInfo()
    {
        _dataStorageManager.RemoveIfExists(MauiConsts.CurrentSessionTenantInfo);
    }
}