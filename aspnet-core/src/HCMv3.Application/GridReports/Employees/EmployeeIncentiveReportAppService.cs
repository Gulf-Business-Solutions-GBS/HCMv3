using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Organizations;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Incentive analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeIncentive.ascx(.cs)</c>,
/// methods <c>Table1()</c> (lines 468-528) and <c>Table2()</c> (lines 529-644).
///
/// ATYPICAL: this report has a real TWO-LEVEL DRILL-DOWN, which the reference report did not. Legacy's
/// <c>GetKey()</c> (lines 381-403) returns "Department" for level 1 and "OID" for level 2, and the
/// callback panel pushed/popped the selected department name on a session stack. Here the two levels are
/// simply two endpoint pairs and the client passes the selected org unit ID back down.
///
/// Both levels apply the SAME transaction filter (wage type "1012", payment date in range, employee
/// status/code, RLS); level 2 just adds the selected org unit.
///
/// NOT PORTED (DevExpress/WebForms plumbing): the drill-up session stack, the ColumnChooser JSON
/// persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeIncentive)]
public class EmployeeIncentiveReportAppService : GridReportAppServiceBase, IEmployeeIncentiveReportAppService
{
    /// <summary>Legacy lines 473/534: wage type "1012" is the incentive wage type.</summary>
    private const string IncentiveWageTypeCode = "1012";

    private readonly IRepository<EmployeeRemuneration> _remunerationRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeIncentiveReportAppService(
        IRepository<EmployeeRemuneration> remunerationRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _remunerationRepository = remunerationRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeIncentive";

    protected override string ScreenCaption => "Employee Incentive";

    // ---------------------------------------------------------------------------------------------
    // LEVEL 1 -- legacy Table1()
    // ---------------------------------------------------------------------------------------------

    [HttpPost]
    public async Task<PagedResultDto<EmployeeIncentiveDepartmentRowDto>> GetEmployeeIncentiveReport(
        GetEmployeeIncentiveReportInput input)
    {
        input.Normalize();

        var rows = await BuildMasterRowsAsync(input);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), rows.Count);

        var page = rows
            .AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        return new PagedResultDto<EmployeeIncentiveDepartmentRowDto>(rows.Count, page);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeIncentiveReportToExcel(GetEmployeeIncentiveReportInput input)
    {
        input.Normalize();

        // Deliberately NOT paged: the legacy exporter exported the entire filtered data set.
        var rows = await BuildMasterRowsAsync(input);

        var sorted = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "EmployeeIncentive.xlsx",
            sorted,
            GetMasterExportColumns(),
            BuildParameterString(input));
    }

    // ---------------------------------------------------------------------------------------------
    // LEVEL 2 -- legacy Table2(selectedColumnValue)
    // ---------------------------------------------------------------------------------------------

    [HttpPost]
    public async Task<PagedResultDto<EmployeeIncentiveDetailRowDto>> GetEmployeeIncentiveDetailReport(
        GetEmployeeIncentiveDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildDetailQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeIncentiveDetailRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeIncentiveDetailReportToExcel(
        GetEmployeeIncentiveDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildDetailQueryAsync(input);

        // Deliberately NOT PageBy -- the export must return every matching row.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeIncentiveDetail.xlsx",
            rows,
            GetDetailExportColumns(),
            BuildParameterString(input));
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// The shared transaction filter used by BOTH levels -- legacy lines 472-486 (Table1) and 533-554
    /// (Table2), which are character-for-character the same predicate apart from the department clause.
    /// </summary>
    private async Task<IQueryable<EmployeeRemuneration>> BuildFilteredTransactionsAsync(
        GridReportInputBase input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var employeeStatus = input.EmployeeStatus;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        return _remunerationRepository.GetAll()
            // Legacy lines 473-476.
            .Where(t => t.WageType != null
                        && t.WageType.Code == IncentiveWageTypeCode
                        && t.PaymentDate >= fromDate
                        && t.PaymentDate <= toDate)
            // Legacy lines 479-483.
            .WhereIf(employeeStatus.HasValue, t => t.Employee.Status == employeeStatus.Value)
            // Legacy lines 484-488.
            .WhereIf(employeeCodes.Count > 0, t => employeeCodes.Contains(t.Employee.Code))
            // Legacy line 491 ApplyRowLevelSecurity -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, t => rlsEmployeeIds.Contains(t.EmployeeId));
    }

    /// <summary>
    /// Single source of truth for the MASTER rows, shared by the level-1 read and its export.
    /// Ports legacy <c>Table1()</c>'s grouping (lines 509-517).
    ///
    /// Grouped in two stages -- COUNT per employee in SQL, then folded onto the employee's current org
    /// unit in memory -- because HCMv3 has no <c>Employee.CurrentDepartment</c> field to group on (see
    /// <see cref="EmployeeIncentiveDepartmentRowDto"/>). The master result is one row per org unit, so the
    /// in-memory fold is bounded by the org tree, not by the transaction volume.
    /// </summary>
    private async Task<List<EmployeeIncentiveDepartmentRowDto>> BuildMasterRowsAsync(
        GetEmployeeIncentiveReportInput input)
    {
        var transactions = await BuildFilteredTransactionsAsync(input);

        var countsByEmployee = await transactions
            .GroupBy(t => t.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync();

        if (countsByEmployee.Count == 0)
        {
            return new List<EmployeeIncentiveDepartmentRowDto>();
        }

        var employeeIds = countsByEmployee.Select(c => c.EmployeeId).ToList();
        var currentOrgUnitByEmployee = await GetCurrentOrgUnitIdsAsync(employeeIds);
        var orgUnitNames = await GetOrgUnitNamesAsync();

        return countsByEmployee
            .Select(c =>
            {
                currentOrgUnitByEmployee.TryGetValue(c.EmployeeId, out var organizationUnitId);
                return new { OrganizationUnitId = organizationUnitId, c.Count };
            })
            .GroupBy(x => x.OrganizationUnitId)
            .Select(g => new EmployeeIncentiveDepartmentRowDto
            {
                OrganizationUnitId = g.Key,
                OrganizationUnitName = g.Key != null && orgUnitNames.TryGetValue(g.Key.Value, out var name)
                    ? name
                    : string.Empty,
                // Legacy line 516: g.Count() over the TRANSACTIONS, despite the "Employee" caption.
                EmployeeCount = g.Sum(x => x.Count)
            })
            .ToList();
    }

    /// <summary>
    /// Single source of truth for the DETAIL filtering AND projection, shared by the level-2 read and its
    /// export. Ports legacy <c>Table2()</c> (lines 533-620).
    /// </summary>
    private async Task<IQueryable<EmployeeIncentiveDetailRowDto>> BuildDetailQueryAsync(
        GetEmployeeIncentiveDetailReportInput input)
    {
        var transactions = await BuildFilteredTransactionsAsync(input);

        var today = Today;
        var useArabicNames = UseArabicNames;
        var organizationUnitId = input.OrganizationUnitId;

        var currentAssignments = _orgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        // Legacy lines 537-538: restrict to the drilled-into department.
        transactions = transactions.Where(t => currentAssignments
            .Where(o => o.EmployeeId == t.EmployeeId)
            .OrderBy(o => o.StartDate)
            .ThenBy(o => o.Id)
            .Select(o => o.OrganizationUnitId)
            .FirstOrDefault() == organizationUnitId);

        return transactions.Select(t => new EmployeeIncentiveDetailRowDto
        {
            // Legacy "OID" column (line 583).
            Id = t.Id,
            EmployeeCode = t.Employee.Code,
            // Legacy line 585 used FullNameEn unconditionally; the Arabic name is offered here for parity
            // with every other report in the family, which all switch on the request culture.
            EmployeeName = useArabicNames ? t.Employee.FullNameAr : t.Employee.FullNameEn,
            JobTitle = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            CostCenterCode = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null ? string.Empty : o.CostCenter.Code)
                .FirstOrDefault(),
            CostCenterName = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null
                    ? string.Empty
                    : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            // Legacy lines 605-606 read the DEPARTMENT's payroll area; HCMv3 stores the payroll area on the
            // org assignment itself, which is the same value for the same employee.
            PayrollAreaName = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null ? string.Empty : o.PayrollArea.Name)
                .FirstOrDefault(),
            WageTypeName = useArabicNames ? t.WageType.NameAr : t.WageType.NameEn,
            PaymentDate = t.PaymentDate,
            PaymentNumber = t.PaymentNumber,
            PaymentUnit = t.PaymentUnit,
            OrganizationUnitId = currentAssignments
                .Where(o => o.EmployeeId == t.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Maps each employee to the org unit of the assignment current as of today -- legacy's
    /// <c>Employee.CurrentOrgAssignment</c> (Employee.cs:1298-1358).
    /// </summary>
    private async Task<Dictionary<int, long?>> GetCurrentOrgUnitIdsAsync(List<int> employeeIds)
    {
        var today = Today;

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= today && o.EndDate >= today)
            .Select(o => new { o.EmployeeId, o.OrganizationUnitId, o.StartDate, o.Id })
            .ToListAsync();

        return assignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.StartDate).ThenBy(a => a.Id).First().OrganizationUnitId);
    }

    private async Task<Dictionary<long, string>> GetOrgUnitNamesAsync()
    {
        var useArabicNames = UseArabicNames;

        var profiles = await _orgUnitProfileRepository.GetAll()
            .Select(p => new
            {
                p.OrganizationUnitId,
                Name = useArabicNames ? p.NameAr : p.OrganizationUnit.DisplayName
            })
            .ToListAsync();

        return profiles
            .GroupBy(p => p.OrganizationUnitId)
            .ToDictionary(g => g.Key, g => g.First().Name);
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the detail read and the detail
    /// export so the two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeIncentiveDetailRowDto> rows)
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

    private static IReadOnlyList<GridReportColumn<EmployeeIncentiveDepartmentRowDto>> GetMasterExportColumns()
    {
        return new List<GridReportColumn<EmployeeIncentiveDepartmentRowDto>>
        {
            new("OrganizationUnit", r => r.OrganizationUnitName),
            new("Employee", r => r.EmployeeCount)
        };
    }

    private static IReadOnlyList<GridReportColumn<EmployeeIncentiveDetailRowDto>> GetDetailExportColumns()
    {
        return new List<GridReportColumn<EmployeeIncentiveDetailRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitle),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("CostCenterCode", r => r.CostCenterCode),
            new("CostCenter", r => r.CostCenterName),
            new("PayrollArea", r => r.PayrollAreaName),
            new("WageType", r => r.WageTypeName),
            new("PaymentDate", r => r.PaymentDate),
            new("PaymentNumber", r => r.PaymentNumber),
            new("PaymentUnit", r => r.PaymentUnit.ToString())
        };
    }
}
