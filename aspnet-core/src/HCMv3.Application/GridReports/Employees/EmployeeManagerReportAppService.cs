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
/// Employee Manager analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeManager.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 461-641).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (<c>GetKey()</c>/<c>GetTable()</c> define a single "Table1", so it never has a second level);
/// the ColumnChooser JSON persistence; the callback-panel state machine; the PDF export button; and the
/// dead <c>Table1_Old()</c> (lines 643+), an earlier formulation of the same query.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeManager)]
public class EmployeeManagerReportAppService : GridReportAppServiceBase, IEmployeeManagerReportAppService
{
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeManagerReportAppService(
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeManagerRepository = employeeManagerRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeManager";

    protected override string ScreenCaption => "Employee Manager";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeManagerReportRowDto>> GetEmployeeManagerReport(
        GetEmployeeManagerReportInput input)
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

        return new PagedResultDto<EmployeeManagerReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeManagerReportToExcel(GetEmployeeManagerReportInput input)
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
            "EmployeeManager.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeManager.ascx.cs:461-641).
    /// </summary>
    private async Task<IQueryable<EmployeeManagerReportRowDto>> BuildQueryAsync(
        GetEmployeeManagerReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Legacy lines 566-568: the whole organisation block comes from the assignment current as of
        // DateTime.Now -- NOT as of the report's From date. Kept exactly as legacy had it.
        var today = Today;

        var query = _employeeManagerRepository.GetAll()
            // Legacy lines 540-542: Employee != null && Manager != null && !(StartDate > dtTo ||
            // EndDate < dtFrom). Both FKs are non-nullable ints in HCMv3, so only the overlap remains.
            .Where(m => m.StartDate.Date <= toDate && m.EndDate.Date >= fromDate)
            // Legacy lines 548-549: Employee.Status == employeeStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, m => m.Employee.Status == employeeStatus.Value)
            // Legacy lines 553-554: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, m => employeeCodes.Contains(m.Employee.Code))
            // Legacy line 557 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, m => rlsEmployeeIds.Contains(m.EmployeeId));

        // Assignments current TODAY. Legacy took FirstOrDefault over exactly this set; the OrderBy only
        // makes "first" deterministic, which server-side paging needs and the in-memory grid did not.
        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        return query.Select(m => new EmployeeManagerReportRowDto
        {
            // Legacy "OID" column. Exposed as Id because it is the row key, not a data column.
            Id = m.Id,
            EmployeeCode = m.Employee.Code,
            EmployeeName = useArabicNames ? m.Employee.FullNameAr : m.Employee.FullNameEn,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == m.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            // Replaces the legacy OrgLevel1..7 block: HCMv3 collapsed those seven FKs into one FK to
            // ABP's hierarchical OrganizationUnit tree.
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == m.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == m.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            CostCenterCode = currentOrgAssignments
                .Where(o => o.EmployeeId == m.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null ? string.Empty : o.CostCenter.Code)
                .FirstOrDefault(),
            CostCenterName = currentOrgAssignments
                .Where(o => o.EmployeeId == m.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null
                    ? string.Empty
                    : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            PostingGroupName = currentOrgAssignments
                .Where(o => o.EmployeeId == m.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PostingGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.PostingGroup.NameAr : o.PostingGroup.NameEn))
                .FirstOrDefault(),
            BudgetingGroupName = currentOrgAssignments
                .Where(o => o.EmployeeId == m.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.BudgetingGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.BudgetingGroup.NameAr : o.BudgetingGroup.NameEn))
                .FirstOrDefault(),
            // Legacy line 615: Manager.CodeWithFullNameAr/En, whose PersistentAlias is
            // Concat(IsNull(Code,''), ' ', IsNull(FullName,'')).
            ManagerName = m.Manager.Code + " " + (useArabicNames ? m.Manager.FullNameAr : m.Manager.FullNameEn)
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The legacy technical OID key is omitted (it is a
    /// surrogate database id with no meaning to the reader); OrgLevel1..7 is one column here and
    /// WorkflowGroup is absent -- see <see cref="EmployeeManagerReportRowDto"/> for why.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeManagerReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeManagerReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocationName),
            new("CostCenterCode", r => r.CostCenterCode),
            new("CostCenter", r => r.CostCenterName),
            new("PostingGroup", r => r.PostingGroupName),
            new("BudgetingGroup", r => r.BudgetingGroupName),
            new("Manager", r => r.ManagerName)
        };
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeManagerReportRowDto> rows)
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
