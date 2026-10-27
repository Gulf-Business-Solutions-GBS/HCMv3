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
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Staff Headcount analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptStaffHeadcount.ascx(.cs)</c>.
///
/// ATYPICAL FOR THIS FAMILY -- this is the only report in this batch with a REAL multi-level
/// drill-down. Legacy defines three tables with three different grid keys
/// (<c>GetKey()</c>, lines 381-400):
/// <list type="number">
/// <item><description><c>Table1()</c> (lines 474-524), key "Department" -- headcount per department.</description></item>
/// <item><description><c>Table2(selectedColumnValue)</c> (lines 526-579), key "Employee" -- the employees
/// of the clicked department.</description></item>
/// <item><description><c>Table3(selectedColumnValue)</c> (lines 581+), key "OID" -- the full personnel
/// record of the clicked employee.</description></item>
/// </list>
/// Levels 1 and 2 are ported here as two read/export pairs.
///
/// LEVEL 3 IS NOT PORTED. It is not a headcount view at all: it re-emits the complete employee master
/// record (~60 columns -- basic pay, org, sponsor, separation, documents, addresses ...) and is
/// column-for-column the same payload as the separate <c>uscRptEmployeeInfo</c> report, which is its own
/// entry in the ~140-report wave. Duplicating it here would fork that report's column set. The Angular
/// screen therefore stops at level 2; a level-3 click should route to the employee record screen.
///
/// FILTER NOTE: every level re-applies the SAME status/employee-code/RLS filters from scratch (legacy
/// repeats the identical filter block in all three methods), so a drill-down never widens the result
/// set. That is reproduced -- <see cref="GetStaffHeadcountEmployeeReport"/> applies the full filter set
/// and not just the department selection.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing: the drill-up session stack itself
/// (<c>oldSelectedvalues</c>/<c>indexKey</c>/<c>DrillUp</c>, replaced by the client passing the selected
/// org unit id explicitly), the ColumnChooser JSON persistence, the callback-panel state machine, and
/// the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_StaffHeadcount)]
public class StaffHeadcountReportAppService : GridReportAppServiceBase, IStaffHeadcountReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public StaffHeadcountReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptStaffHeadcount";

    protected override string ScreenCaption => "Staff Headcount";

    // ---------------------------------------------------------------- LEVEL 1: department headcount

    [HttpPost]
    public async Task<PagedResultDto<StaffHeadcountReportRowDto>> GetStaffHeadcountReport(
        GetStaffHeadcountReportInput input)
    {
        input.Normalize();

        var query = await BuildDepartmentQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<StaffHeadcountReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetStaffHeadcountReportToExcel(GetStaffHeadcountReportInput input)
    {
        input.Normalize();

        var query = await BuildDepartmentQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "StaffHeadcount.xlsx",
            rows,
            GetDepartmentExportColumns(),
            BuildParameterString(input));
    }

    // -------------------------------------------------------- LEVEL 2: employees of one department

    [HttpPost]
    public async Task<PagedResultDto<StaffHeadcountEmployeeReportRowDto>> GetStaffHeadcountEmployeeReport(
        GetStaffHeadcountEmployeeReportInput input)
    {
        input.Normalize();

        var query = await BuildEmployeeQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildParameterString(input, "Organization Unit : " + input.OrganizationUnitId),
            totalCount);

        return new PagedResultDto<StaffHeadcountEmployeeReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetStaffHeadcountEmployeeReportToExcel(GetStaffHeadcountEmployeeReportInput input)
    {
        input.Normalize();

        var query = await BuildEmployeeQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "StaffHeadcountEmployees.xlsx",
            rows,
            GetEmployeeExportColumns(),
            BuildParameterString(input, "Organization Unit : " + input.OrganizationUnitId));
    }

    // ---------------------------------------------------------------------------------- queries

    /// <summary>
    /// The filter block legacy repeats verbatim in <c>Table1()</c> (lines 476-491),
    /// <c>Table2()</c> (lines 543-558) and <c>Table3()</c>: employee status, comma-separated employee
    /// codes and row-level security. No date predicate -- legacy has none on this report.
    /// </summary>
    private async Task<IQueryable<Employee>> BuildFilteredEmployeesAsync(GridReportInputBase input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;

        return _employeeRepository.GetAll()
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // An empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));
    }

    /// <summary>
    /// Single source of truth for LEVEL 1 filtering AND projection, shared by its read and its export.
    /// Ports legacy <c>Table1()</c> (uscRptStaffHeadcount.ascx.cs:474-524).
    /// </summary>
    private async Task<IQueryable<StaffHeadcountReportRowDto>> BuildDepartmentQueryAsync(
        GetStaffHeadcountReportInput input)
    {
        var employees = await BuildFilteredEmployeesAsync(input);
        var today = Today;

        // Legacy line 476: r.CurrentDepartment != null -- employees with no current org unit are
        // excluded from the headcount entirely. See the row DTO remarks for the CurrentDepartment
        // -> current assignment's OrganizationUnit adaptation.
        var assigned = from employee in employees
                       from assignment in _employeeOrgAssignmentRepository.GetAll()
                           .Where(o => o.EmployeeId == employee.Id
                                       && o.StartDate.Date <= today
                                       && o.EndDate.Date >= today
                                       && o.OrganizationUnitId != null)
                       select assignment;

        return assigned
            .GroupBy(o => new { OrganizationUnitId = o.OrganizationUnitId.Value, o.OrganizationUnit.DisplayName })
            .Select(g => new StaffHeadcountReportRowDto
            {
                Id = g.Key.OrganizationUnitId,
                DepartmentName = g.Key.DisplayName,
                EmployeeCount = g.Count()
            });
    }

    /// <summary>
    /// Single source of truth for LEVEL 2 filtering AND projection, shared by its read and its export.
    /// Ports legacy <c>Table2()</c> (uscRptStaffHeadcount.ascx.cs:526-579).
    /// </summary>
    private async Task<IQueryable<StaffHeadcountEmployeeReportRowDto>> BuildEmployeeQueryAsync(
        GetStaffHeadcountEmployeeReportInput input)
    {
        var employees = await BuildFilteredEmployeesAsync(input);
        var today = Today;
        var organizationUnitId = input.OrganizationUnitId;

        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        // Legacy lines 537-538: the same "current department" restriction, narrowed to the clicked one.
        var query = employees.Where(e => _employeeOrgAssignmentRepository.GetAll()
            .Any(o => o.EmployeeId == e.Id
                      && o.StartDate.Date <= today
                      && o.EndDate.Date >= today
                      && o.OrganizationUnitId == organizationUnitId));

        return query.Select(e => new StaffHeadcountEmployeeReportRowDto
        {
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn
        });
    }

    // ---------------------------------------------------------------------------------- exports

    /// <summary>Legacy level-1 columns: Department, Employees.</summary>
    private static IReadOnlyList<GridReportColumn<StaffHeadcountReportRowDto>> GetDepartmentExportColumns()
    {
        return new List<GridReportColumn<StaffHeadcountReportRowDto>>
        {
            new("Department", r => r.DepartmentName),
            new("Employees", r => r.EmployeeCount)
        };
    }

    /// <summary>Legacy level-2 columns: Code, Employee.</summary>
    private static IReadOnlyList<GridReportColumn<StaffHeadcountEmployeeReportRowDto>> GetEmployeeExportColumns()
    {
        return new List<GridReportColumn<StaffHeadcountEmployeeReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName)
        };
    }
}
