using Abp.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;

namespace HCMv3.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {

    }
}

