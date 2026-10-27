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
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Rejoin analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeRejoin.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 462-629).
///
/// NOTE the date semantics differ from most reports in this family: legacy filters rejoins whose
/// <c>StartDate</c> falls INSIDE the window (line 528-530: <c>StartDate &gt;= dtFrom &amp;&amp;
/// StartDate &lt;= dtTo</c>), not rows whose range overlaps it.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (single "Table1"), the ColumnChooser JSON persistence, the callback-panel state machine, the PDF
/// export button, and the dead <c>Table1_Old()</c> (lines 631+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeRejoin)]
public class EmployeeRejoinReportAppService : GridReportAppServiceBase, IEmployeeRejoinReportAppService
{
    private readonly IRepository<EmployeeRejoin> _employeeRejoinRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeRejoinReportAppService(
        IRepository<EmployeeRejoin> employeeRejoinRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<Employee> employeeRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRejoinRepository = employeeRejoinRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeRepository = employeeRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeRejoin";

    protected override string ScreenCaption => "Employee Rejoin";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRejoinReportRowDto>> GetEmployeeRejoinReport(
        GetEmployeeRejoinReportInput input)
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

        return new PagedResultDto<EmployeeRejoinReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeRejoinReportToExcel(GetEmployeeRejoinReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeRejoin.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeRejoin.ascx.cs:462-629).
    /// </summary>
    private async Task<IQueryable<EmployeeRejoinReportRowDto>> BuildQueryAsync(
        GetEmployeeRejoinReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Legacy line 549: the org block comes from the assignment current as of DateTime.Now, NOT as of
        // the report window. Kept as legacy had it.
        var today = Today;

        var query = _employeeRejoinRepository.GetAll()
            // Legacy lines 528-530: Employee != null && StartDate >= dtFrom && StartDate <= dtTo.
            // Containment, not overlap. HCMv3 renamed the field to RejoinDate.
            .Where(r => r.RejoinDate >= fromDate && r.RejoinDate <= toDate)
            // Legacy lines 536-537: Employee.Status == employeeStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, r => r.Employee.Status == employeeStatus.Value)
            // Legacy lines 541-542: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // Legacy line 545 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId));

        return query.Select(r => new EmployeeRejoinReportRowDto
        {
            // Legacy "OID" column -- the row key, not a data column.
            Id = r.Id,
            EmployeeCode = r.Employee.Code,
            EmployeeName = useArabicNames ? r.Employee.FullNameAr : r.Employee.FullNameEn,
            StartDate = r.RejoinDate,
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == r.EmployeeId
                            && o.StartDate <= today
                            && o.EndDate >= today
                            && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            RejoinWithPrevLeaveAccrual = r.CarryForwardLeaveBalance,
            RejoinWithPrevIndemnityAccrual = r.CarryForwardIndemnityBalance,
            RejoinWithPrevTicketAccrual = r.CarryForwardTicketBalance,
            RejoinWithPrevLoan = r.CarryForwardLoanBalance,
            // Legacy line 587: the code of the employee record pointing back at this one through
            // PreviousEmpCode -- i.e. the new record the rejoin created.
            NewEmployeeCode = _employeeRepository.GetAll()
                .Where(e => e.PreviousEmpCode == r.Employee.Code)
                .OrderBy(e => e.Id)
                .Select(e => e.Code)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted; OrgLevel1..7 is
    /// the single OrganizationUnit column and CreateNewEmployeeCode is absent -- see the row DTO remarks.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeRejoinReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeRejoinReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("StartDate", r => r.StartDate),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("RejoinWithPrevLeaveAccrual", r => r.RejoinWithPrevLeaveAccrual),
            new("RejoinWithPrevIndemnityAccrual", r => r.RejoinWithPrevIndemnityAccrual),
            new("RejoinWithPrevTicketAccrual", r => r.RejoinWithPrevTicketAccrual),
            new("RejoinWithPrevLoan", r => r.RejoinWithPrevLoan),
            new("NewEmployeeCode", r => r.NewEmployeeCode)
        };
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeRejoinReportRowDto> rows)
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
