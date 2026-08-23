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
/// Employee History analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeHistory.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 444-670).
///
/// ATYPICAL ROW GRAIN: one row per employee PER CHANGE DATE, where the change dates are the distinct union
/// of the employee's (currently effective) basic-pay start date and EVERY one of the employee's org
/// assignment start dates (legacy lines 606-611). Each row then shows the assignment and the basic salary
/// in force on that date.
///
/// LEGACY QUIRK REPRODUCED: legacy's base query is <c>Employee.Status == Active</c> (line 529) and the
/// Employee Status combo is ANDed ON TOP of it (lines 533-537), so selecting anything other than Active
/// returns NOTHING. That is faithfully reproduced here rather than silently "fixed"; the Angular screen
/// keeps the status filter visible, exactly as legacy did.
///
/// NOT PORTED (DevExpress/WebForms plumbing): the drill-up/drill-down session stack (single level here),
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 671+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeHistory)]
public class EmployeeHistoryReportAppService : GridReportAppServiceBase, IEmployeeHistoryReportAppService
{
    /// <summary>Legacy line 553: wage type "1000" is the basic salary wage type.</summary>
    private const string BasicSalaryWageTypeCode = "1000";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeHistoryReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeHistory";

    protected override string ScreenCaption => "Employee History";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeHistoryReportRowDto>> GetEmployeeHistoryReport(
        GetEmployeeHistoryReportInput input)
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

        return new PagedResultDto<EmployeeHistoryReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeHistoryReportToExcel(GetEmployeeHistoryReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeHistory.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c> (lines 527-660).
    ///
    /// Legacy built the change-date list per employee in memory; here the same distinct union is expressed
    /// as a SQL UNION so the result stays a pageable <c>IQueryable</c>.
    /// </summary>
    private async Task<IQueryable<EmployeeHistoryReportRowDto>> BuildQueryAsync(
        GetEmployeeHistoryReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var employeeStatus = input.EmployeeStatus;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var employees = _employeeRepository.GetAll()
            // Legacy line 529: the base query is hard-coded to Active.
            .Where(e => e.Status == EmployeeStatus.Active)
            // Legacy lines 533-537: ANDed on top -- see the class remarks about this quirk.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            // Legacy lines 538-542.
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy line 544 ApplyRowLevelSecurity -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        // Legacy lines 548-562: only the basic pay CURRENTLY in force, and only wage type 1000.
        var basicPay = _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= today
                        && d.EmployeeBasicPay.EndDate >= today
                        && d.WageType != null
                        && d.WageType.Code == BasicSalaryWageTypeCode);

        // Legacy lines 564-566: NO date restriction on org assignments -- the date filter is commented out
        // in the legacy source, so the whole assignment history is in scope.
        var orgAssignments = _orgAssignmentRepository.GetAll();

        // Legacy lines 606-611: Union() is a DISTINCT union, so a date shared by both sources yields one row.
        var changeDates = basicPay
            .Select(d => new ChangeDate { EmployeeId = d.EmployeeBasicPay.EmployeeId, Date = d.EmployeeBasicPay.StartDate })
            .Union(orgAssignments.Select(o => new ChangeDate { EmployeeId = o.EmployeeId, Date = o.StartDate }));

        var query = from d in changeDates
                    join e in employees on d.EmployeeId equals e.Id
                    select new { d.Date, Employee = e };

        return query.Select(x => new EmployeeHistoryReportRowDto
        {
            // Legacy "OID" column (line 620) is the EMPLOYEE id -- rows are not unique by it.
            Id = x.Employee.Id,
            EmployeeCode = x.Employee.Code,
            EmployeeName = x.Employee.FullNameEn,
            EmployeeNameAr = x.Employee.FullNameAr,
            StartDate = x.Date,

            // Legacy lines 630-631: the assignment in force ON the change date.
            JobTitle = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : o.JobTitle.NameEn)
                .FirstOrDefault(),
            JobTitleAr = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : o.JobTitle.NameAr)
                .FirstOrDefault(),
            PayrollAreaName = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null ? string.Empty : o.PayrollArea.Name)
                .FirstOrDefault(),
            EmployeeGroupName = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.EmployeeGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.EmployeeGroup.NameAr : o.EmployeeGroup.NameEn))
                .FirstOrDefault(),
            EmployeeSubGroupName = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.EmployeeSubGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.EmployeeSubGroup.NameAr : o.EmployeeSubGroup.NameEn))
                .FirstOrDefault(),
            WorkLocationName = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            CostCenterName = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null
                    ? string.Empty
                    : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = orgAssignments
                .Where(o => o.EmployeeId == x.Employee.Id && o.StartDate <= x.Date && o.EndDate >= x.Date)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),

            // Legacy lines 654-656: the wage-type-1000 amount in force on the change date, 0 when none.
            BasicSalary = basicPay
                .Where(p => p.EmployeeBasicPay.EmployeeId == x.Employee.Id
                            && p.EmployeeBasicPay.StartDate <= x.Date
                            && p.EmployeeBasicPay.EndDate >= x.Date)
                .Select(p => p.Amount)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeHistoryReportRowDto> rows)
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

    /// <summary>The exported column set, in legacy column order. The technical OID key is omitted.</summary>
    private static IReadOnlyList<GridReportColumn<EmployeeHistoryReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeHistoryReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("FullNameAr", r => r.EmployeeNameAr),
            new("StartDate", r => r.StartDate),
            new("JobTitle", r => r.JobTitle),
            new("JobTitleAr", r => r.JobTitleAr),
            new("PayrollArea", r => r.PayrollAreaName),
            new("EmployeeGroup", r => r.EmployeeGroupName),
            new("EmployeeSubGroup", r => r.EmployeeSubGroupName),
            new("BasicSalary", r => r.BasicSalary),
            new("WorkLocation", r => r.WorkLocationName),
            new("CostCenter", r => r.CostCenterName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7)
        };
    }

    /// <summary>
    /// Named type (not anonymous) so the SQL <c>UNION</c> of the two date sources translates.
    /// </summary>
    private class ChangeDate
    {
        public int EmployeeId { get; set; }

        public System.DateTime Date { get; set; }
    }
}
