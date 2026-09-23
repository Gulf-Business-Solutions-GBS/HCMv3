using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.SystemSecurity;

namespace HCMv3.SystemSettings.Services;

/// <inheritdoc cref="ISysSettingsAccessor"/>
/// <remarks>
/// <see cref="UnitOfWorkAttribute"/> at the class level so this domain service is independently
/// callable (opens its own unit of work/DbContext when not already invoked from within one, e.g. an
/// <c>IApplicationService</c> caller) -- matching how <c>IRepository&lt;T&gt;</c> itself behaves standalone.
///
/// FIRST READ CREATES THE ROW. Like legacy <c>SysService.Get*Settings(Session)</c>, when a tenant has no
/// row for a settings group yet, the first read builds one from the tenant's <see cref="SysTenantConfig"/>
/// keys (<see cref="SysSettingsTenantConfigFactory"/>) and saves it, so every later read and the settings
/// screen see the same row. Outside a tenant (host session, no tenant filter) nothing is created and
/// <c>null</c> is returned, as no legacy screen ever ran without a tenant.
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
    private readonly IAbpSession _abpSession;

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
        IRepository<SysTenantConfig> sysTenantConfigRepository,
        IAbpSession abpSession)
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
        _abpSession = abpSession;
    }

    // NOTE: HCMv3.Core has no reference to EntityFrameworkCore (per architecture rules), so the usual
    // Microsoft.EntityFrameworkCore async IQueryable extensions (.FirstOrDefaultAsync() etc.) are not
    // available here. These singleton lookups use the plain synchronous IQueryable returned by
    // IRepository<T>.GetAll() instead (same pattern as the already-shipped
    // HCMv3.Authorization.Users.Password.PasswordExpirationService domain service).

    public Task<ManageAppraisalSettings> GetAppraisalSettingsAsync() =>
        GetOrCreateAsync(_appraisalSettingsRepository, SysSettingsTenantConfigFactory.CreateAppraisalSettings);

    public Task<ManageDocumentSettings> GetDocumentSettingsAsync() =>
        GetOrCreateAsync(_documentSettingsRepository, SysSettingsTenantConfigFactory.CreateDocumentSettings);

    public Task<ManageFIPostingSettings> GetFIPostingSettingsAsync() =>
        GetOrCreateAsync(_fiPostingSettingsRepository, SysSettingsTenantConfigFactory.CreateFIPostingSettings);

    public Task<ManageGeneralSettings> GetGeneralSettingsAsync() =>
        GetOrCreateAsync(_generalSettingsRepository, SysSettingsTenantConfigFactory.CreateGeneralSettings);

    public Task<ManageNotificationSettings> GetNotificationSettingsAsync() =>
        GetOrCreateAsync(_notificationSettingsRepository, SysSettingsTenantConfigFactory.CreateNotificationSettings);

    public Task<ManageOrgSettings> GetOrgSettingsAsync() =>
        GetOrCreateAsync(_orgSettingsRepository, SysSettingsTenantConfigFactory.CreateOrgSettings);

    public Task<ManagePayrollSettings> GetPayrollSettingsAsync() =>
        GetOrCreateAsync(_payrollSettingsRepository, SysSettingsTenantConfigFactory.CreatePayrollSettings);

    public Task<ManagePersonnelSettings> GetPersonnelSettingsAsync() =>
        GetOrCreateAsync(_personnelSettingsRepository, SysSettingsTenantConfigFactory.CreatePersonnelSettings);

    public Task<ManageProjectCostingSettings> GetProjectCostingSettingsAsync() =>
        GetOrCreateAsync(_projectCostingSettingsRepository, SysSettingsTenantConfigFactory.CreateProjectCostingSettings);

    public Task<ManageReportSettings> GetReportSettingsAsync() =>
        GetOrCreateAsync(_reportSettingsRepository, SysSettingsTenantConfigFactory.CreateReportSettings);

    public Task<ManageSecuritySettings> GetSecuritySettingsAsync() =>
        GetOrCreateAsync(_securitySettingsRepository, SysSettingsTenantConfigFactory.CreateSecuritySettings);

    public Task<ManageSelfServiceSettings> GetSelfServiceSettingsAsync() =>
        GetOrCreateAsync(_selfServiceSettingsRepository, SysSettingsTenantConfigFactory.CreateSelfServiceSettings);

    public Task<ManageTimeSettings> GetTimeSettingsAsync() =>
        GetOrCreateAsync(_timeSettingsRepository, SysSettingsTenantConfigFactory.CreateTimeSettings);

    public Task<ManageWorkflowSettings> GetWorkflowSettingsAsync() =>
        GetOrCreateAsync(_workflowSettingsRepository, SysSettingsTenantConfigFactory.CreateWorkflowSettings);

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

    /// <summary>
    /// The tenant's row, or -- when it has none -- a new one built from its <see cref="SysTenantConfig"/>
    /// keys and saved. The tenant is the unit of work's current tenant (set by tenant-switching background
    /// workers) and otherwise the session's.
    /// </summary>
    private async Task<TSettings> GetOrCreateAsync<TSettings>(
        IRepository<TSettings> repository,
        Func<SysTenantConfigLookup, TSettings> create)
        where TSettings : class, IEntity<int>, IMustHaveTenant
    {
        var existing = repository.GetAll().FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        var tenantId = CurrentUnitOfWork?.GetTenantId() ?? _abpSession.TenantId;
        if (tenantId == null)
        {
            return null;
        }

        var settings = create(LoadTenantConfig());
        settings.TenantId = tenantId.Value;

        await repository.InsertAsync(settings);
        if (CurrentUnitOfWork != null)
        {
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        return settings;
    }

    /// <summary>
    /// Legacy read each key with <c>FirstOrDefault()</c>; with duplicate keys the lowest id wins here so
    /// the choice is at least stable.
    /// </summary>
    private SysTenantConfigLookup LoadTenantConfig()
    {
        var rows = _sysTenantConfigRepository.GetAll()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Key, r.KeyValue })
            .ToList();

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows.Where(r => r.Key != null))
        {
            values.TryAdd(row.Key, row.KeyValue);
        }

        return new SysTenantConfigLookup(values);
    }
}
