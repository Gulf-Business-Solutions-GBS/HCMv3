using Abp.Dependency;
using HCMv3.ApiClient;

namespace HCMv3.Maui.Services.Permission;

public class PermissionService : IPermissionService, ISingletonDependency
{
    private readonly IApplicationContext _appContext;

    public PermissionService(IApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public bool HasPermission(string key)
    {
        if (_appContext.Configuration.Auth.GrantedPermissions.TryGetValue(key,
                out var permissionValue))
        {
            return string.Equals(permissionValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}