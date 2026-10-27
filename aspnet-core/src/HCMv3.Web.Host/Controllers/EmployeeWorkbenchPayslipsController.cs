using System;
using System.IO;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using HCMv3.Authorization;
using HCMv3.Configuration;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Reports.Dto;
using HCMv3.Reports.Payroll;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HCMv3.Web.Controllers;

/// <summary>
/// Employee Workbench, Payroll tile: one employee's payslip for one month, as a PDF.
///
/// <para>The same ported legacy report <c>rptPayslipEn</c> the print report screen shows and Go-Live Setup
/// renders (see <see cref="GoLivePayslipsController"/>), narrowed to a single employee. Rendered here for
/// the same reason as there: DevExpress reports live in <c>HCMv3.Reports</c>, which only Web.Host
/// references. The employee code comes from the workbench service rather than from the caller, so the PDF
/// always belongs to the employee id that was authorised.</para>
///
/// <para>Two permissions, both required: the workbench, and salary registers -- the same right that shows
/// the payslip rows the button sits on.</para>
/// </summary>
[AbpMvcAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkbench, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/services/app/employeeWorkbenchPayslips/[action]")]
public class EmployeeWorkbenchPayslipsController : HCMv3ControllerBase
{
    /// <summary>Legacy <c>@PrmEmployeeStatus</c> sentinel for "any status" -- an ex-employee's payslip still prints.</summary>
    private const int AnyEmployeeStatus = -1;

    private readonly IAppConfigurationAccessor _configurationAccessor;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IEmployeeWorkbenchAppService _workbenchAppService;

    public EmployeeWorkbenchPayslipsController(
        IAppConfigurationAccessor configurationAccessor,
        ISysSettingsAccessor sysSettingsAccessor,
        IEmployeeWorkbenchAppService workbenchAppService)
    {
        _configurationAccessor = configurationAccessor;
        _sysSettingsAccessor = sysSettingsAccessor;
        _workbenchAppService = workbenchAppService;
    }

    [HttpPost]
    [AbpMvcAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public async Task<ReportPdfOutput> GetPayslipPdf([FromBody] GetWorkbenchPayslipPdfInput input)
    {
        var employee = await _workbenchAppService.GetHeader(new EntityDto(input.EmployeeId));

        // The host reads its settings through this accessor, not through the injected IConfiguration.
        var connectionString = _configurationAccessor.Configuration.GetConnectionString("Default");

        // With an employee code the report ignores the payroll area parameter entirely.
        var report = new rptPayslipEn(
            employeeCode: employee.Code,
            payrollArea: string.Empty,
            runType: (int)input.PayrollRunType,
            month: input.Month,
            year: input.Year,
            employeeStatus: AnyEmployeeStatus);

        report.ApplyConnectionString(connectionString);
        report.ApplyTenant(AbpSession.TenantId);
        report.ApplyLoggedUserName(AbpSession.UserId?.ToString() ?? string.Empty);

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        report.ApplyPayslipSignature(payrollSettings?.PayslipSignature ?? true);
        var reportSettings = await _sysSettingsAccessor.GetReportSettingsAsync();
        report.ApplyDecimalInReports(reportSettings?.DecimalInReports ?? 0);

        report.CreateDocument();

        using var stream = new MemoryStream();
        report.ExportToPdf(stream);

        var fileName = "Payslip_" + employee.Code + "_" + input.Month.ToString("00") + input.Year + ".pdf";
        return new ReportPdfOutput(fileName, Convert.ToBase64String(stream.ToArray()));
    }
}
