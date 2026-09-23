using System;
using System.Threading.Tasks;

namespace HCMv3.SystemSettings.Services;

/// <summary>
/// Repository-accessor companion to the 14 <c>Manage*Settings</c> singleton entities (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.Services.SysService</c>'s ~15 <c>GetXSettings(Session)</c> methods).
///
/// Like legacy <c>GetXSettings</c>, a tenant's first read of a settings group creates its row from the
/// tenant's <c>SysTenantConfig</c> keys (see <see cref="SysSettingsTenantConfigFactory"/> for the key by
/// key mapping) and saves it. Each accessor therefore returns the tenant's one row; it returns
/// <c>null</c> only outside a tenant context (host session with no tenant filter), so callers still
/// guard against null.
/// </summary>
public interface ISysSettingsAccessor
{
    Task<ManageAppraisalSettings> GetAppraisalSettingsAsync();
    Task<ManageDocumentSettings> GetDocumentSettingsAsync();
    Task<ManageFIPostingSettings> GetFIPostingSettingsAsync();
    Task<ManageGeneralSettings> GetGeneralSettingsAsync();
    Task<ManageNotificationSettings> GetNotificationSettingsAsync();
    Task<ManageOrgSettings> GetOrgSettingsAsync();
    Task<ManagePayrollSettings> GetPayrollSettingsAsync();
    Task<ManagePersonnelSettings> GetPersonnelSettingsAsync();
    Task<ManageProjectCostingSettings> GetProjectCostingSettingsAsync();
    Task<ManageReportSettings> GetReportSettingsAsync();
    Task<ManageSecuritySettings> GetSecuritySettingsAsync();
    Task<ManageSelfServiceSettings> GetSelfServiceSettingsAsync();
    Task<ManageTimeSettings> GetTimeSettingsAsync();
    Task<ManageWorkflowSettings> GetWorkflowSettingsAsync();

    /// <summary>
    /// Ported from legacy <c>DateTimeService.GetSysDateTimeFormat</c>: combines
    /// <c>ManageGeneralSettings.DateEditMask</c> and <c>TimeEditMask</c>, defaulting to
    /// "dd/MM/yyyy"/"HH:mm" when blank or when no settings row exists yet.
    /// </summary>
    Task<string> GetSysDateTimeFormatAsync();

    /// <summary>Ported from legacy <c>DateTimeService.GetSysDateFormat</c>.</summary>
    Task<string> GetSysDateFormatAsync();

    /// <summary>Ported from legacy <c>DateTimeService.GetSysTimeFormat</c>.</summary>
    Task<string> GetSysTimeFormatAsync();

    /// <summary>
    /// Ported from legacy <c>SysService.IsDateAfter_TIME_EVAL_EARLIEST_DATE</c>: <c>false</c> only when
    /// both dates are non-default and <paramref name="transDate"/> is strictly before
    /// <c>ManageTimeSettings.EarliestEvaluationDate</c>.
    /// </summary>
    Task<bool> IsDateAfterTimeEvalEarliestDateAsync(DateTime transDate);

    /// <summary>
    /// Generic Key/Value config lookup against <see cref="SystemSecurity.SysTenantConfig"/>. Ported
    /// verbatim from legacy <c>SysService.GetSysTenantConfig(string strKey, Session session)</c>: despite
    /// <c>SysTenantConfig</c> having <c>StartDate</c>/<c>EndDate</c> columns, the legacy method body does
    /// NOT filter by them (only by <c>Key</c>) -- it takes the first matching row regardless of its
    /// date range, so no "effective as of now" date filtering is applied here either, matching legacy
    /// behavior exactly. Returns <c>""</c> (matching legacy's <c>refobject</c> default) when no row
    /// matches <paramref name="key"/> or the matching row's <c>KeyValue</c> is <c>null</c>.
    /// </summary>
    Task<string> GetSysTenantConfigAsync(string key);
}
