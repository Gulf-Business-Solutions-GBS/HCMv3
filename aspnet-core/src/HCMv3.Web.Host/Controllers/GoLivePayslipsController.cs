using System;
using System.IO;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Configuration;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using HCMv3.Reports.Dto;
using HCMv3.Reports.Payroll;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HCMv3.Web.Controllers;

/// <summary>
/// Go-Live Setup, step 2: the month's payslips for everyone in the payroll area, as one PDF.
///
/// <para>The document is the ported legacy report <c>rptPayslipEn</c> itself -- the same one the print
/// report screen shows -- rendered here rather than in the Document Viewer, because Go-Live Setup already knows the
/// month and the payroll area and has no reason to ask again through a parameter panel.</para>
///
/// <para>A controller rather than an application service: DevExpress reports live in
/// <c>HCMv3.Reports</c>, which only Web.Host references, and rendering a document is presentation.
/// Nothing is decided here -- the report's own SQL selects the employees, filtered by payroll area,
/// month, year and run type, and by the tenant every query carries.</para>
/// </summary>
[AbpMvcAuthorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/services/app/goLivePayslips/[action]")]
public class GoLivePayslipsController : HCMv3ControllerBase
{
    /// <summary>Legacy <c>@PrmEmployeeStatus</c> sentinel for "any status".</summary>
    private const int AnyEmployeeStatus = -1;

    private readonly IAppConfigurationAccessor _configurationAccessor;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public GoLivePayslipsController(IAppConfigurationAccessor configurationAccessor, ISysSettingsAccessor sysSettingsAccessor)
    {
        _configurationAccessor = configurationAccessor;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    [AbpMvcAuthorize(AppPermissions.Pages_GoLiveSetup, AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public async Task<ReportPdfOutput> GetPayslipsPdf([FromBody] GoLivePayslipsInput input)
    {
        if (input?.PayDate == null || string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            throw new UserFriendlyException(L("GoLivePayslipsNeedMonthAndArea"));
        }

        var monthEnd = input.PayDate.Value;
        // The host reads its settings through this accessor, not through the injected IConfiguration --
        // which is empty here, as the report connection provider beside this controller already knew.
        var connectionString = _configurationAccessor.Configuration.GetConnectionString("Default");

        // Every employee in the area: the report reads an empty employee code as "all of them".
        var report = new rptPayslipEn(
            employeeCode: string.Empty,
            payrollArea: input.PayrollAreaName.Trim(),
            runType: (int)PayrollRunType.Regular,
            month: monthEnd.Month,
            year: monthEnd.Year,
            employeeStatus: AnyEmployeeStatus);

        report.ApplyConnectionString(connectionString);
        report.ApplyTenant(AbpSession.TenantId);
        report.ApplyLoggedUserName(AbpSession.UserId?.ToString() ?? string.Empty);

        // Legacy rptPayslipEn constructor: the signature block follows ManagePayrollSettings.PayslipSignature,
        // and the amount format follows ManageReportSettings.DecimalInReports.
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        report.ApplyPayslipSignature(payrollSettings?.PayslipSignature ?? true);
        var reportSettings = await _sysSettingsAccessor.GetReportSettingsAsync();
        report.ApplyDecimalInReports(reportSettings?.DecimalInReports ?? 0);

        report.CreateDocument();

        using var stream = new MemoryStream();
        report.ExportToPdf(stream);

        var fileName = "Payslips_" + monthEnd.ToString("MMyyyy") + ".pdf";
        return new ReportPdfOutput(fileName, Convert.ToBase64String(stream.ToArray()));
    }
}
