using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.SystemSecurity;

namespace HCMv3.SystemSettings.Services;

/// <inheritdoc cref="ISysSettingsAccessor"/>
/// <remarks>
/// <see cref="UnitOfWorkAttribute"/> at the class level so this domain service is independently
/// callable (opens its own unit of work/DbContext when not already invoked from within one, e.g. an
/// <c>IApplicationService</c> caller) -- matching how <c>IRepository&lt;T&gt;</c> itself behaves standalone.
/// </remarks>
[UnitOfWork]
public class SysSettingsAccessor : HCMv3DomainServiceBase, ISysSettingsAccessor
{
    private readonly IRepository<ManageAppraisalSettings> _appraisalSettingsRepository;
    private readonly IRepository<ManageDocumentSettings> _documentSettingsRepository;
    private readonly IRepository<ManageFIPostingSettings> _fiPostingSettingsRepository;
    private readonly IRepository<ManageGeneralSettings> _generalSettingsRepository;
    private readonly IRepository<ManageNotificationSettings> _notificationSettingsRepository;
    private readonly IRepository<ManageOrgSettings> _orgSettingsRepository;
    private readonly IRepository<ManagePayrollSettings> _payrollSettingsRepository;
    private readonly IRepository<ManagePersonnelSettings> _personnelSettingsRepository;
    private readonly IRepository<ManageProjectCostingSettings> _projectCostingSettingsRepository;
    private readonly IRepository<ManageReportSettings> _reportSettingsRepository;
    private readonly IRepository<ManageSecuritySettings> _securitySettingsRepository;
    private readonly IRepository<ManageSelfServiceSettings> _selfServiceSettingsRepository;
    private readonly IRepository<ManageTimeSettings> _timeSettingsRepository;
    private readonly IRepository<ManageWorkflowSettings> _workflowSettingsRepository;
    private readonly IRepository<SysTenantConfig> _sysTenantConfigRepository;

    public SysSettingsAccessor(
        IRepository<ManageAppraisalSettings> appraisalSettingsRepository,
        IRepository<ManageDocumentSettings> documentSettingsRepository,
        IRepository<ManageFIPostingSettings> fiPostingSettingsRepository,
        IRepository<ManageGeneralSettings> generalSettingsRepository,
        IRepository<ManageNotificationSettings> notificationSettingsRepository,
        IRepository<ManageOrgSettings> orgSettingsRepository,
        IRepository<ManagePayrollSettings> payrollSettingsRepository,
        IRepository<ManagePersonnelSettings> personnelSettingsRepository,
        IRepository<ManageProjectCostingSettings> projectCostingSettingsRepository,
        IRepository<ManageReportSettings> reportSettingsRepository,
        IRepository<ManageSecuritySettings> securitySettingsRepository,
        IRepository<ManageSelfServiceSettings> selfServiceSettingsRepository,
        IRepository<ManageTimeSettings> timeSettingsRepository,
        IRepository<ManageWorkflowSettings> workflowSettingsRepository,
        IRepository<SysTenantConfig> sysTenantConfigRepository)
    {
        _appraisalSettingsRepository = appraisalSettingsRepository;
        _documentSettingsRepository = documentSettingsRepository;
        _fiPostingSettingsRepository = fiPostingSettingsRepository;
        _generalSettingsRepository = generalSettingsRepository;
        _notificationSettingsRepository = notificationSettingsRepository;
        _orgSettingsRepository = orgSettingsRepository;
        _payrollSettingsRepository = payrollSettingsRepository;
        _personnelSettingsRepository = personnelSettingsRepository;
        _projectCostingSettingsRepository = projectCostingSettingsRepository;
        _reportSettingsRepository = reportSettingsRepository;
        _securitySettingsRepository = securitySettingsRepository;
        _selfServiceSettingsRepository = selfServiceSettingsRepository;
        _timeSettingsRepository = timeSettingsRepository;
        _workflowSettingsRepository = workflowSettingsRepository;
        _sysTenantConfigRepository = sysTenantConfigRepository;
    }

    // NOTE: HCMv3.Core has no reference to EntityFrameworkCore (per architecture rules), so the usual
    // Microsoft.EntityFrameworkCore async IQueryable extensions (.FirstOrDefaultAsync() etc.) are not
    // available here. These singleton lookups use the plain synchronous IQueryable returned by
    // IRepository<T>.GetAll() instead (same pattern as the already-shipped
    // HCMv3.Authorization.Users.Password.PasswordExpirationService domain service), wrapped in
    // Task.FromResult for interface consistency with the rest of this port's domain services.

    public Task<ManageAppraisalSettings> GetAppraisalSettingsAsync() =>
        Task.FromResult(_appraisalSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageDocumentSettings> GetDocumentSettingsAsync() =>
        Task.FromResult(_documentSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageFIPostingSettings> GetFIPostingSettingsAsync() =>
        Task.FromResult(_fiPostingSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageGeneralSettings> GetGeneralSettingsAsync() =>
        Task.FromResult(_generalSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageNotificationSettings> GetNotificationSettingsAsync() =>
        Task.FromResult(_notificationSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageOrgSettings> GetOrgSettingsAsync() =>
        Task.FromResult(_orgSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManagePayrollSettings> GetPayrollSettingsAsync() =>
        Task.FromResult(_payrollSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManagePersonnelSettings> GetPersonnelSettingsAsync() =>
        Task.FromResult(_personnelSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageProjectCostingSettings> GetProjectCostingSettingsAsync() =>
        Task.FromResult(_projectCostingSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageReportSettings> GetReportSettingsAsync() =>
        Task.FromResult(_reportSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageSecuritySettings> GetSecuritySettingsAsync() =>
        Task.FromResult(_securitySettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageSelfServiceSettings> GetSelfServiceSettingsAsync() =>
        Task.FromResult(_selfServiceSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageTimeSettings> GetTimeSettingsAsync() =>
        Task.FromResult(_timeSettingsRepository.GetAll().FirstOrDefault());

    public Task<ManageWorkflowSettings> GetWorkflowSettingsAsync() =>
        Task.FromResult(_workflowSettingsRepository.GetAll().FirstOrDefault());

    public async Task<string> GetSysDateTimeFormatAsync()
    {
        var dateFormat = await GetSysDateFormatAsync();
        var timeFormat = await GetSysTimeFormatAsync();

        return dateFormat + " " + timeFormat;
    }

    public async Task<string> GetSysDateFormatAsync()
    {
        var generalSettings = await GetGeneralSettingsAsync();
        var dateEditMask = generalSettings?.DateEditMask;

        return string.IsNullOrEmpty(dateEditMask) ? "dd/MM/yyyy" : dateEditMask;
    }

    public async Task<string> GetSysTimeFormatAsync()
    {
        var generalSettings = await GetGeneralSettingsAsync();
        var timeEditMask = generalSettings?.TimeEditMask;

        return string.IsNullOrEmpty(timeEditMask) ? "HH:mm" : timeEditMask;
    }

    public async Task<bool> IsDateAfterTimeEvalEarliestDateAsync(DateTime transDate)
    {
        var timeSettings = await GetTimeSettingsAsync();
        if (timeSettings == null)
        {
            return true;
        }

        var minStartDateLimit = timeSettings.EarliestEvaluationDate;
        if (transDate != default && minStartDateLimit != default && transDate < minStartDateLimit)
        {
            return false;
        }

        return true;
    }

    public Task<string> GetSysTenantConfigAsync(string key)
    {
        var keyValue = _sysTenantConfigRepository.GetAll()
            .Where(r => r.Key == key)
            .Select(r => r.KeyValue)
            .FirstOrDefault();

        return Task.FromResult(keyValue ?? string.Empty);
    }
}
