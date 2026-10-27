using System.Linq;

namespace HCMv3.Authorization.Users;

/// <summary>
/// Hardcoded system/service-account usernames (ported verbatim from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.SysService</c>'s <c>userSys*</c> constants and
/// <c>SysUserList</c>/<c>IsSysUser</c> methods). "System user" here means one of these five specific,
/// hardcoded login names -- NOT a role check (that is legacy's separate <c>IsAdminRole</c>, which checks
/// role membership against <c>AdminRolesList</c> and is out of scope for this port item).
/// </summary>
public static class SysUserNames
{
    public const string SysAdmin = "SysAdmin";
    public const string SysDataAdmin = "SysDataAdmin";
    public const string SysPayrollAdmin = "SysPayrollAdmin";
    public const string SysTimeAdmin = "SysTimeAdmin";
    public const string SysJobAdmin = "SysJobAdmin";

    /// <summary>Ported verbatim from legacy <c>SysService.SysUserList()</c>.</summary>
    public static string[] SysUserList() => new[]
    {
        SysAdmin,
        SysPayrollAdmin,
        SysTimeAdmin,
        SysDataAdmin,
        SysJobAdmin
    };

    /// <summary>Ported verbatim from legacy <c>SysService.IsSysUser(string userName)</c>.</summary>
    public static bool IsSysUser(string userName) => SysUserList().Contains(userName);
}
