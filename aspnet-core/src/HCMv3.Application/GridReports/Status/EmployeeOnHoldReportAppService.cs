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
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Status.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee On-Hold analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeOnHold.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 459-655).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeOnHold)]
public class EmployeeOnHoldReportAppService : GridReportAppServiceBase, IEmployeeOnHoldReportAppService
{
    private readonly IRepository<EmployeeOnHold> _employeeOnHoldRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeOnHoldReportAppService(
        IRepository<EmployeeOnHold> employeeOnHoldRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeOnHoldRepository = employeeOnHoldRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeOnHold";

    protected override string ScreenCaption => "Employee On Hold";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeOnHoldReportRowDto>> GetEmployeeOnHoldReport(
        GetEmployeeOnHoldReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeOnHoldReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeOnHoldReportToExcel(GetEmployeeOnHoldReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeOnHold.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeOnHold.ascx.cs:459-655).
    /// </summary>
    private async Task<IQueryable<EmployeeOnHoldReportRowDto>> BuildQueryAsync(GetEmployeeOnHoldReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var bankTransfer = input.BankTransfer;
        var today = Today;

        var query = _employeeOnHoldRepository.GetAll()
            // Legacy line 547: !(StartDate > dtTo || EndDate < dtFrom), i.e. an overlapping window.
            .Where(o => !(o.StartDate.Date > toDate || o.EndDate.Date < fromDate))
            .WhereIf(bankTransfer.HasValue, o => o.BankTransfer == bankTransfer.Value)
            .WhereIf(employeeCodes.Count > 0, o => employeeCodes.Contains(o.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, o => rlsEmployeeIds.Contains(o.EmployeeId));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.StartDate.Date <= today && a.EndDate.Date >= today);

        return query.Select(o => new EmployeeOnHoldReportRowDto
        {
            Id = o.Id,
            EmployeeCode = o.Employee.Code,
            EmployeeName = useArabicNames ? o.Employee.FullNameAr : o.Employee.FullNameEn,
            CivilId = o.Employee.CivilID,
            StartDate = o.StartDate,
            EndDate = o.EndDate,
            OrganizationUnitId = currentOrgAssignments
                .Where(a => a.EmployeeId == o.EmployeeId && a.OrganizationUnitId != null)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.OrganizationUnitId)
                .FirstOrDefault(),
            JobTitleName = currentOrgAssignments
                .Where(a => a.EmployeeId == o.EmployeeId)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.JobTitle == null ? string.Empty : (useArabicNames ? a.JobTitle.NameAr : a.JobTitle.NameEn))
                .FirstOrDefault(),
            OnHold = o.OnHold,
            OnHoldBasicPay = o.OnHoldBasicPay,
            OnHoldLeavePay = o.OnHoldLeavePay,
            OnHoldIndemnityPay = o.OnHoldIndemnityPay,
            OnHoldAirTicketPay = o.OnHoldAirTicketPay,
            OnHoldLoanDeduction = o.OnHoldLoanDeduction,
            OnHoldAllEarnings = o.OnHoldAllEarnings,
            OnHoldAllDeductions = o.OnHoldAllDeductions,
            BankTransfer = o.BankTransfer,
            Comments = o.Comments
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeOnHoldReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeOnHoldReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("CivilId", r => r.CivilId),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("OnHold", r => r.OnHold),
            new("OnHoldBasicPay", r => r.OnHoldBasicPay),
            new("OnHoldLeavePay", r => r.OnHoldLeavePay),
            new("OnHoldIndemnityPay", r => r.OnHoldIndemnityPay),
            new("OnHoldAirTicketPay", r => r.OnHoldAirTicketPay),
            new("OnHoldLoanDeduction", r => r.OnHoldLoanDeduction),
            new("OnHoldAllEarnings", r => r.OnHoldAllEarnings),
            new("OnHoldAllDeductions", r => r.OnHoldAllDeductions),
            new("BankTransfer", r => r.BankTransfer.ToString()),
            new("Comments", r => r.Comments)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeOnHoldReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }
}
