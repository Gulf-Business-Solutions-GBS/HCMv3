using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.SystemSettings.Dto;
using HCMv3.SystemSettings.Services;

namespace HCMv3.SystemSettings;

/// <inheritdoc cref="ITenantUiSettingsAppService"/>
[AbpAuthorize]
public class TenantUiSettingsAppService : HCMv3AppServiceBase, ITenantUiSettingsAppService
{
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public TenantUiSettingsAppService(ISysSettingsAccessor sysSettingsAccessor)
    {
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task<TenantUiSettingsDto> GetTenantUiSettings()
    {
        var org = await _sysSettingsAccessor.GetOrgSettingsAsync();
        var payroll = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var report = await _sysSettingsAccessor.GetReportSettingsAsync();
        var general = await _sysSettingsAccessor.GetGeneralSettingsAsync();
        var time = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var personnel = await _sysSettingsAccessor.GetPersonnelSettingsAsync();

        return new TenantUiSettingsDto
        {
            EnablePostingGroup = org?.EnablePostingGroup ?? false,
            EnableBudgetingGroup = org?.EnableBudgetingGroup ?? false,
            EnablePosition = org?.EnablePosition ?? false,
            EnableOrgChart3Levels = org?.EnableOrgChart3Levels ?? false,
            ShowBillingTypeInPaymentScreen = payroll?.ShowBillingTypeInPaymentScreen ?? false,
            AutoFillFnFCompensation = payroll?.AutoFillFnFCompensation ?? false,
            EditableAutoFillFnFCompensation = payroll?.EditableAutoFillFnFCompensation ?? false,
            AllowSimulationRun = payroll?.AllowSimulationRun ?? false,
            DecimalInReports = report?.DecimalInReports > 0 ? report.DecimalInReports : 3,
            DisableAutoEmpcode = general?.DisableAutoEmpcode ?? false,
            EnableIsTravelling = time?.EnableIsTravelling ?? false,
            EnableAutoHalfDayLeave = time?.EnableAutoHalfDayLeave ?? false,
            AutoWageAssignOnHire = personnel?.AutoWageAssignOnHire ?? false
        };
    }
}
