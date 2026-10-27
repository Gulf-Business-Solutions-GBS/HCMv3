using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
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
/// Employee Duty Period analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptEmployeeDutyPeriod.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 484-640).
///
/// GRAIN: one row per <see cref="EmployeeDutyPeriod"/> assignment overlapping the requested window.
///
/// The legacy filter panel's <c>cmbEmpStatus</c> is a DUTY PERIOD combo on this screen, not an employee
/// status combo -- see <see cref="GetEmployeeDutyPeriodReportInput"/>.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack
/// (single-level here: <c>GetKey()</c>/<c>GetTable()</c> define only "Table1", lines 380-464), the
/// ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the dead
/// <c>Table1_Old()</c> (lines 641+), an earlier formulation of the same query.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeDutyPeriod)]
public class EmployeeDutyPeriodReportAppService
    : GridReportAppServiceBase, IEmployeeDutyPeriodReportAppService
{
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeDutyPeriodReportAppService(
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeDutyPeriod";

    protected override string ScreenCaption => "Employee Duty Period";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDutyPeriodReportRowDto>> GetEmployeeDutyPeriodReport(
        GetEmployeeDutyPeriodReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildDutyPeriodParameterString(input),
            totalCount);

        return new PagedResultDto<EmployeeDutyPeriodReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeDutyPeriodReportToExcel(GetEmployeeDutyPeriodReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data
        // set, so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeDutyPeriod.xlsx",
            rows,
            GetExportColumns(),
            BuildDutyPeriodParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (lines 466-479) logs the duty period on top of the employee code and
    /// the date range. The base helper appends extra parts at the END whereas legacy printed the duty
    /// period FIRST -- an ordering difference inside a free-text audit field.
    /// </summary>
    private string BuildDutyPeriodParameterString(GetEmployeeDutyPeriodReportInput input)
    {
        return BuildParameterString(
            input,
            input.DutyPeriodCode.IsNullOrWhiteSpace() ? null : "Duty Period : " + input.DutyPeriodCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s "Get Data" + "Fill Data" regions (lines 546-628).
    ///
    /// The OrgLevel columns are not part of this query; see <see cref="StampOrgLevelsAsync"/>, which both
    /// entry points also call.
    /// </summary>
    private async Task<IQueryable<EmployeeDutyPeriodReportRowDto>> BuildQueryAsync(
        GetEmployeeDutyPeriodReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var dutyPeriodCode = input.DutyPeriodCode;
        var hasDutyPeriodFilter = !dutyPeriodCode.IsNullOrWhiteSpace();

        // Legacy lines 570-572: the org columns are read from the assignment current as of TODAY, not as
        // of the duty-period row's dates and not as of the report window.
        var today = Today;

        var query = _employeeDutyPeriodRepository.GetAll()
            // Legacy lines 548-549: !(StartDate > dtTo || EndDate < dtFrom) -- an overlap test, so a row
            // touching either boundary is INCLUDED.
            .Where(d => d.StartDate.Date <= toDate && d.EndDate.Date >= fromDate)
            // Legacy lines 552-556: DutyPeriod.Code == the combo's value.
            .WhereIf(hasDutyPeriodFilter, d => d.DutyPeriod.Code == dutyPeriodCode)
            // Legacy lines 558-563: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.Employee.Code))
            // Legacy line 565 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId));

        // Legacy took FirstOrDefault() of the assignments current today (line 573); ordering by StartDate
        // then Id makes that pick deterministic.
        var currentAssignment = _orgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        return query.Select(d => new EmployeeDutyPeriodReportRowDto
        {
            // Legacy "OID" column (line 610).
            Id = d.Id,
            EmployeeCode = d.Employee.Code,
            EmployeeName = useArabicNames ? d.Employee.FullNameAr : d.Employee.FullNameEn,
            // Legacy lines 580/616: empty string, not null, when there is no org assignment or no job title.
            JobTitleName = currentAssignment
                .Where(o => o.EmployeeId == d.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault() ?? string.Empty,
            // Legacy OrgLevel1..7 collapsed to one OrganizationUnit column -- see the DTO remarks.
            OrganizationUnitId = currentAssignment
                .Where(o => o.EmployeeId == d.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            // Legacy line 590: empty string, not null, when the row has no duty period.
            DutyPeriodName = d.DutyPeriod == null
                ? string.Empty
                : (useArabicNames ? d.DutyPeriod.NameAr : d.DutyPeriod.NameEn),
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            StartPoint = d.StartPoint,
            // Legacy lines 588/625: PayrollArea.Country of the same current assignment, "" when absent.
            CountryName = currentAssignment
                .Where(o => o.EmployeeId == d.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null || o.PayrollArea.Country == null
                    ? string.Empty
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault() ?? string.Empty,
            // Legacy lines 587/626.
            WorkLocationName = currentAssignment
                .Where(o => o.EmployeeId == d.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault() ?? string.Empty
        });
    }

    /// <summary>
    /// Fills the seven legacy OrgLevel columns after materialization. Shared by the read and the export so
    /// the two can never drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeDutyPeriodReportRowDto> rows)
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

    /// <summary>
    /// The exported column set, in legacy column order (lines 528-544). The technical OID key is omitted
    /// because it is a surrogate database id with no meaning to the reader.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeDutyPeriodReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeDutyPeriodReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("DutyPeriod", r => r.DutyPeriodName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("StartPoint", r => r.StartPoint),
            new("Country", r => r.CountryName),
            new("WorkLocation", r => r.WorkLocationName)
        };
    }
}
