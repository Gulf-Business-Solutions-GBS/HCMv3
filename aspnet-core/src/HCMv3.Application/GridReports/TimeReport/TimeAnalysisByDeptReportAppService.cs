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
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Time Analysis By Department grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeAnalysisByDept.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 481-543).
///
/// SUMMARY GRAIN: one row per department, over the <c>TimeRegister</c> rows whose <c>DayDate</c> falls in
/// the requested window. Legacy columns: <c>Department</c>, <c>TotalValue</c> and a hidden <c>Period</c>
/// that is literally the report's TO date stamped on every row.
///
/// FLAGGED DEFECT -- THE VALUE COLUMN IS DEAD IN LEGACY. <c>TotalValue</c> is hardcoded to <c>0</c>: the
/// expression that produced it, <c>g.Sum(r =&gt; r.TimeRegisterDetail.Sum(v =&gt; v.TimeValue))</c>, is
/// commented out with the note "below line commented during time reg restructing" (lines 527-529), and the
/// same is true of all three drill-down levels (<c>Table2</c> 583, <c>Table3</c> 641-642, <c>Table4</c>
/// 697-698). This report has therefore been rendering a column of zeros in production ever since. It is
/// NOT reconstructed here -- the <c>TimeRegisterDetail</c>/<c>TimeValue</c> model it summed does not exist
/// in HCMv3 either, so any replacement number would be invented rather than ported. The zero is
/// reproduced, pinned by a test, and reported so a product decision can be taken.
///
/// AGREED DIVERGENCE: legacy would throw a <c>NullReferenceException</c> on <c>g.Key.org.NameEn</c>
/// (line 526) for any employee with no org unit. Crashing is not behaviour worth reproducing, so such
/// employees fall into the <c>""</c> department -- the same choice already made in
/// <c>PayrollAnalysisByDeptReportAppService</c>.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the four-level drill-down stack
/// (<c>Table2</c>..<c>Table4</c>), the ColumnChooser JSON persistence, the callback-panel state machine
/// and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_TimeAnalysisByDept)]
public class TimeAnalysisByDeptReportAppService
    : GridReportAppServiceBase, ITimeAnalysisByDeptReportAppService
{
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public TimeAnalysisByDeptReportAppService(
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _timeRegisterRepository = timeRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptTimeAnalysisByDept";

    protected override string ScreenCaption => "Time Analysis By Department";

    [HttpPost]
    public async Task<PagedResultDto<TimeAnalysisByDeptReportRowDto>> GetTimeAnalysisByDeptReport(
        GetTimeAnalysisByDeptReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<TimeAnalysisByDeptReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetTimeAnalysisByDeptReportToExcel(
        GetTimeAnalysisByDeptReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "TimeAnalysisByDept.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND aggregation, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (lines 483-541).
    ///
    /// IN-MEMORY GROUPING (deliberate, same rationale as <c>PayrollAnalysisByDeptReportAppService</c>):
    /// the group key is the DEPARTMENT NAME, which in HCMv3 is not a stored column but the deepest
    /// classified ancestor of the employee's org-unit leaf. The database narrows to the DISTINCT EMPLOYEE
    /// IDS that have a time register in the window -- bounded by the head count, not by the number of days
    /// -- and the department roll-up happens here.
    /// </summary>
    private async Task<IQueryable<TimeAnalysisByDeptReportRowDto>> BuildQueryAsync(
        GetTimeAnalysisByDeptReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;

        var employeeIds = await _timeRegisterRepository.GetAll()
            // Legacy lines 483-484: DayDate >= dtFrom && DayDate <= dtTo.
            .Where(r => r.DayDate >= fromDate && r.DayDate <= toDate)
            // Legacy lines 488-493: Employee.Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, r => r.Employee.Status == employeeStatus.Value)
            // Legacy lines 494-498: employees.Contains(r.Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // Legacy line 500 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId))
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync();

        if (employeeIds.Count == 0)
        {
            return new List<TimeAnalysisByDeptReportRowDto>().AsQueryable();
        }

        var departmentNameByEmployeeId = await BuildDepartmentNamesAsync(employeeIds);

        // Legacy lines 520-537: one row per distinct department, TotalValue always 0 (see class remarks),
        // Period always the report's TO date.
        var rows = employeeIds
            .Select(id => departmentNameByEmployeeId.TryGetValue(id, out var name) ? name : string.Empty)
            .Distinct()
            .Select(name => new TimeAnalysisByDeptReportRowDto
            {
                DepartmentName = name,
                Period = toDate,
                TotalValue = 0
            })
            .ToList();

        return rows.AsQueryable();
    }

    /// <summary>
    /// Legacy <c>OrgService.GetOrgUnitForUC(employee)</c> -- the DEEPEST non-null
    /// <c>Employee.OrgLevel7..OrgLevel1</c> -- rendered as <c>NameEn</c> (line 526; legacy used the English
    /// name even in Arabic, so the lookup is built with <c>useArabicNames: false</c>).
    ///
    /// MODEL ADAPTATION: "deepest non-null level" becomes "the deepest CLASSIFIED level of the org-unit
    /// leaf on the employee's org assignment current as of today" -- legacy read the employee's live org
    /// fields, which are the current ones.
    /// </summary>
    private async Task<Dictionary<int, string>> BuildDepartmentNamesAsync(List<int> employeeIds)
    {
        var today = Today;

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= today && o.EndDate >= today)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId })
            .ToListAsync();

        var leafByEmployee = assignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.StartDate).ThenBy(a => a.Id).First().OrganizationUnitId);

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(false);

        var result = new Dictionary<int, string>();
        foreach (var employeeId in employeeIds)
        {
            var leaf = leafByEmployee.TryGetValue(employeeId, out var unitId) ? unitId : null;
            var levels = lookup.GetLevelNames(leaf);

            // Deepest classified level first -- legacy scanned OrgLevel7 down to OrgLevel1.
            result[employeeId] = levels.LastOrDefault(l => !string.IsNullOrEmpty(l)) ?? string.Empty;
        }

        return result;
    }

    /// <summary>
    /// The exported column set. Legacy's grid showed Department and TotalValue with Period hidden
    /// (<c>ColumnMapping = MappingType.Hidden</c>, line 511); a hidden DataColumn IS still written by the
    /// ASPxGridViewExporter's data source, so Period is exported here too.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<TimeAnalysisByDeptReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<TimeAnalysisByDeptReportRowDto>>
        {
            new("Department", r => r.DepartmentName),
            new("Period", r => r.Period),
            new("TotalValue", r => r.TotalValue)
        };
    }
}
