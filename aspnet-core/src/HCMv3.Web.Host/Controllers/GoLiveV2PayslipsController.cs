using System;
using System.IO;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.Reports.Dto;
using HCMv3.Reports.Payroll;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HCMv3.Web.Controllers;

/// <summary>
/// Go-Live Setup V2, step 2: the month's payslips for everyone in the payroll area, as one PDF.
///
/// <para>The document is the ported legacy report <c>rptPayslipEn</c> itself -- the same one the print
/// report screen shows -- rendered here rather than in the Document Viewer, because V2 already knows the
/// month and the payroll area and has no reason to ask again through a parameter panel.</para>
///
/// <para>A controller rather than an application service: DevExpress reports live in
/// <c>HCMv3.Reports</c>, which only Web.Host references, and rendering a document is presentation.
/// Nothing is decided here -- the report's own SQL selects the employees, filtered by payroll area,
/// month, year and run type, and by the tenant every query carries.</para>
/// </summary>
[AbpMvcAuthorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/services/app/goLiveV2Payslips/[action]")]
public class GoLiveV2PayslipsController : HCMv3ControllerBase
{
    /// <summary>Legacy <c>@PrmEmployeeStatus</c> sentinel for "any status".</summary>
    private const int AnyEmployeeStatus = -1;

    private readonly IConfiguration _configuration;

    public GoLiveV2PayslipsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    [AbpMvcAuthorize(AppPermissions.Pages_GoLiveSetup, AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<ReportPdfOutput> GetPayslipsPdf([FromBody] GoLiveV2PayslipsInput input)
    {
        if (input?.PayDate == null || string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            throw new UserFriendlyException(L("GoLiveV2PayslipsNeedMonthAndArea"));
        }

        var monthEnd = input.PayDate.Value;
        var connectionString = _configuration.GetConnectionString("Default");

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

        report.CreateDocument();

        using var stream = new MemoryStream();
        report.ExportToPdf(stream);

        var fileName = "Payslips_" + monthEnd.ToString("MMyyyy") + ".pdf";
        return Task.FromResult(new ReportPdfOutput(fileName, Convert.ToBase64String(stream.ToArray())));
    }
}
