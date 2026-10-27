using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PayrollManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Leave Provision" analysis grid report (SuperAdmin-only, legacy line 254) --
/// port of legacy <c>uscRptPayrollJV_Custom016.ascx(.cs)</c>, <c>Table1()</c>,
/// <c>cmbType == "Leave Provision"</c> branch (lines 2397-2456). See
/// <see cref="PayrollJVCustom016ProvisionDiffHelper"/> for the shared implementation this and
/// <see cref="PayrollJVCustom016IndemnityProvisionReportAppService"/> both call.
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVCustom016PostingLineLoader"/>
/// remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016LeaveProvision)]
public class PayrollJVCustom016LeaveProvisionReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom016LeaveProvisionReportAppService
{
    /// <summary>Legacy line 2416: Annual Leave Provision.</summary>
    private const string LeaveProvisionWageTypeCode = "3001";

    private readonly PayrollJVCustom016PostingLineLoader _loader;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public PayrollJVCustom016LeaveProvisionReportAppService(
        PayrollJVCustom016PostingLineLoader loader,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _loader = loader;
        _payrollAreaRepository = payrollAreaRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom016";

    protected override string ScreenCaption => "Payroll JV (Custom 016) - Leave Provision";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom016ProvisionDiffRowDto>> GetPayrollJVCustom016LeaveProvisionReport(
        GetPayrollJVCustom016ProvisionDiffReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            PayrollJVCustom016ProvisionDiffHelper.BuildJvParameterString(input, Today, "Leave Provision"),
            rows.Count);

        return new PagedResultDto<PayrollJVCustom016ProvisionDiffRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom016LeaveProvisionReportToExcel(
        GetPayrollJVCustom016ProvisionDiffReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom016LeaveProvision.xlsx",
            ordered,
            ExportColumns,
            PayrollJVCustom016ProvisionDiffHelper.BuildJvParameterString(input, Today, "Leave Provision"));
    }

    private async Task<List<PayrollJVCustom016ProvisionDiffRowDto>> BuildRowsAsync(
        GetPayrollJVCustom016ProvisionDiffReportInput input)
    {
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        return await PayrollJVCustom016ProvisionDiffHelper.BuildRowsAsync(
            LeaveProvisionWageTypeCode,
            input,
            Today,
            _loader,
            _payrollAreaRepository,
            _salaryRegisterDetailRepository,
            _employeeOrgAssignmentRepository,
            rlsEmployeeIds);
    }

    private static readonly List<GridReportColumn<PayrollJVCustom016ProvisionDiffRowDto>> ExportColumns = new()
    {
        new("Oid", r => r.LineNumber),
        new("EmpCode", r => r.EmployeeCode),
        new("AmountReg", r => r.AmountReg),
        new("AmountJV", r => r.AmountJV),
        new("Difference", r => r.Difference)
    };
}
