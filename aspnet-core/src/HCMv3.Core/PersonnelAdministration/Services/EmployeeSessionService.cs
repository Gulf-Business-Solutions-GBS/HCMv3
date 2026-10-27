using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.SysService</c> (user/session-resolution
/// members only -- the ~15 <c>GetXSettings</c> accessors are ported separately as
/// <c>HCMv3.SystemSettings.Services.SysSettingsAccessor</c>).
///
/// SKIPPED (not ported, documented per dispatch instructions):
/// <list type="bullet">
/// <item><description><c>AutoPasswordExpiry</c> -- functionally superseded by the already-ported
/// <c>HCMv3.Authorization.Users.Password.PasswordExpirationService</c>, which implements the same
/// "force password change after N days" domain logic against this app's real <c>User</c>/role model.</description></item>
/// <item><description><c>ExecuteNonQuery</c>/<c>GetData</c> -- raw ADO.NET arbitrary-SQL execution against a
/// hardcoded connection string; no realistic/safe use in this app's ABP/EF Core architecture.</description></item>
/// <item><description><c>ClearModifiedObjects</c> -- XAF <c>IObjectSpace.ModifiedObjects</c> change-tracking
/// concept with no EF Core equivalent (EF's own <c>ChangeTracker</c> is managed automatically per unit of
/// work).</description></item>
/// <item><description><c>GetSysTenantConfig</c> -- generic legacy key/value tenant-config table
/// (<c>SysTenantConfig</c>) was not ported (no equivalent generic config table exists); every setting it
/// used to seed now has a first-class field on the matching <c>Manage*Settings</c> entity instead, edited
/// directly via its own <c>Manage*SettingsAppService</c>.</description></item>
/// <item><description><c>SysUserList</c>/<c>IsSysUser</c> -- the legacy hardcoded "system user" login names
/// (SysAdmin/SysPayrollAdmin/SysTimeAdmin/SysDataAdmin/SysJobAdmin) are XAF-seeded service accounts with no
/// equivalent concept in this port's role/user model -- there is nothing to check against.</description></item>
/// <item><description><c>AddExecutionLog</c>/<c>AddRetroReasonLog</c>/<c>AddLoginAudit</c>/<c>LoadFile*</c>
/// -- out of scope for this wave (attachment/execution-log entity wiring, not a "business logic
/// calculation" port).</description></item>
/// </list>
/// </summary>
[UnitOfWork]
public class EmployeeSessionService : HCMv3DomainServiceBase, IEmployeeSessionService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly UserManager _userManager;
    private readonly IAbpSession _abpSession;

    public EmployeeSessionService(
        IRepository<Employee> employeeRepository,
        IRepository<User, long> userRepository,
        UserManager userManager,
        IAbpSession abpSession)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _abpSession = abpSession;
    }

    public async Task<Employee> GetCurrentEmployeeAsync()
    {
        if (!_abpSession.UserId.HasValue)
        {
            return null;
        }

        var userId = _abpSession.UserId.Value;

        // Authoritative path: the explicit Employee.UserId link, unique per tenant.
        var linkedEmployee = _employeeRepository.GetAll()
            .FirstOrDefault(e => e.UserId == userId);

        if (linkedEmployee != null)
        {
            return linkedEmployee;
        }

        // Fallback for employees not linked yet, so existing installations keep working while
        // EmployeeUserLinkAppService.AutoLinkByLoginId backfills them. Remove once every tenant is linked.
        var currentUser = await _userRepository.FirstOrDefaultAsync(userId);
        if (currentUser == null)
        {
            return null;
        }

        return _employeeRepository.GetAll()
            .FirstOrDefault(e => e.LoginID == currentUser.UserName);
    }

    public async Task<int?> GetCurrentEmployeeIdAsync()
    {
        if (!_abpSession.UserId.HasValue)
        {
            return null;
        }

        var userId = _abpSession.UserId.Value;

        var linkedEmployeeId = _employeeRepository.GetAll()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Id)
            .FirstOrDefault();

        if (linkedEmployeeId.HasValue)
        {
            return linkedEmployeeId;
        }

        var currentUser = await _userRepository.FirstOrDefaultAsync(userId);
        if (currentUser == null)
        {
            return null;
        }

        return _employeeRepository.GetAll()
            .Where(e => e.LoginID == currentUser.UserName)
            .Select(e => (int?)e.Id)
            .FirstOrDefault();
    }

    public async Task<string> GetSysCreatedByAsync(string sysCreator)
    {
        if (string.IsNullOrWhiteSpace(sysCreator))
        {
            return string.Empty;
        }

        var query = _employeeRepository.GetAll().Where(e => e.LoginID == sysCreator);

        var activeCode = query
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => e.Code)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(activeCode))
        {
            return activeCode;
        }

        var anyCode = query.Select(e => e.Code).FirstOrDefault();

        return !string.IsNullOrEmpty(anyCode) ? anyCode : sysCreator;
    }

    public async Task<bool> IsCurrentUserInAdminRoleAsync()
    {
        if (!_abpSession.UserId.HasValue)
        {
            return false;
        }

        var currentUser = await _userRepository.FirstOrDefaultAsync(_abpSession.UserId.Value);
        if (currentUser == null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(currentUser, StaticRoleNames.Tenants.Admin);
    }
}
