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
/// Employee Transfer analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeTransfer.ascx(.cs)</c>.
///
/// Ports <c>Table1()</c> (lines 464-640): every <see cref="EmployeeAction"/> whose
/// <c>PersonalAction.NameEn == "Transfer"</c> falls in the requested window, decorated with the org
/// assignment current as of the transfer date and the one immediately preceding it.
///
/// DEVIATION 1 -- legacy's window predicate is NOT(StartDate after To OR EndDate before From)
/// (line 530). HCMv3's <see cref="EmployeeAction"/> has NO EndDate column at all: the legacy property's
/// getter always just returned <c>StartDate</c>, so it was not ported (see the entity's class remarks).
/// The predicate therefore reduces exactly to "From &lt;= StartDate &lt;= To", which is what this query
/// does -- same result set, one column fewer.
///
/// DEVIATION 2 -- the 10 org-level columns collapse to 2; see
/// <see cref="EmployeeTransferReportRowDto"/> remarks.
///
/// DEVIATION 3 -- legacy resolved the current assignment with an unordered <c>FirstOrDefault()</c>
/// (lines 583-586). An unordered First is non-deterministic under server-side paging, so this port
/// orders by StartDate then Id, matching what the reference report does for the same situation.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing: the drill-up/drill-down session stack (single level
/// here), the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button,
/// and the dead <c>Table1_Old()</c> (line 642+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeTransfer)]
public class EmployeeTransferReportAppService : GridReportAppServiceBase, IEmployeeTransferReportAppService
{
    /// <summary>
    /// Legacy matched the personal action by its ENGLISH NAME, not by code
    /// (uscRptEmployeeTransfer.ascx.cs:529). Kept identical so the same master-data rows match.
    /// </summary>
    private const string TransferPersonalActionNameEn = "Transfer";

    private readonly IRepository<EmployeeAction> _employeeActionRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeTransferReportAppService(
        IRepository<EmployeeAction> employeeActionRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeActionRepository = employeeActionRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeTransfer";

    protected override string ScreenCaption => "Employee Transfer";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTransferReportRowDto>> GetEmployeeTransferReport(
        GetEmployeeTransferReportInput input)
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

        return new PagedResultDto<EmployeeTransferReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeTransferReportToExcel(GetEmployeeTransferReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeTransfer.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeTransfer.ascx.cs:464-640).
    /// </summary>
    private async Task<IQueryable<EmployeeTransferReportRowDto>> BuildQueryAsync(
        GetEmployeeTransferReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;

        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        var query = _employeeActionRepository.GetAll()
            // Legacy lines 528-531: PersonalAction.NameEn == "Transfer" and the action date in window
            // (see DEVIATION 1 in the class remarks for why the EndDate half is gone).
            .Where(a => a.PersonalAction.NameEn == TransferPersonalActionNameEn)
            .Where(a => a.StartDate.Date >= fromDate && a.StartDate.Date <= toDate)
            // Legacy lines 537-538.
            .WhereIf(employeeStatus.HasValue, a => a.Employee.Status == employeeStatus.Value)
            // Legacy lines 542-543.
            .WhereIf(employeeCodes.Count > 0, a => employeeCodes.Contains(a.Employee.Code))
            // Legacy line 546 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId));

        var assignments = _employeeOrgAssignmentRepository.GetAll();

        return query.Select(a => new EmployeeTransferReportRowDto
        {
            // Legacy "OID" column (line 603). Exposed as Id because it is the row key.
            Id = a.Id,
            EmployeeCode = a.Employee.Code,
            EmployeeName = useArabicNames ? a.Employee.FullNameAr : a.Employee.FullNameEn,
            TransferDate = a.StartDate,

            // ---- assignment CURRENT as of the transfer date (legacy lines 583-586) ----
            OrganizationUnitId = assignments
                .Where(o => o.EmployeeId == a.EmployeeId
                            && o.StartDate <= a.StartDate
                            && o.EndDate >= a.StartDate)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            WorkLocationName = assignments
                .Where(o => o.EmployeeId == a.EmployeeId
                            && o.StartDate <= a.StartDate
                            && o.EndDate >= a.StartDate)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? null
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            CostCenterName = assignments
                .Where(o => o.EmployeeId == a.EmployeeId
                            && o.StartDate <= a.StartDate
                            && o.EndDate >= a.StartDate)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null
                    ? null
                    : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            CountryName = assignments
                .Where(o => o.EmployeeId == a.EmployeeId
                            && o.StartDate <= a.StartDate
                            && o.EndDate >= a.StartDate)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null || o.PayrollArea.Country == null
                    ? null
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault(),

            // ---- PREVIOUS assignment: latest one starting strictly before the transfer date
            //      (legacy lines 589-591: StartDate < rec.StartDate, OrderByDescending(StartDate)) ----
            PreviousOrganizationUnitId = assignments
                .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate < a.StartDate)
                .OrderByDescending(o => o.StartDate)
                .ThenByDescending(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            PreviousWorkLocationName = assignments
                .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate < a.StartDate)
                .OrderByDescending(o => o.StartDate)
                .ThenByDescending(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? null
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            PreviousCostCenterName = assignments
                .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate < a.StartDate)
                .OrderByDescending(o => o.StartDate)
                .ThenByDescending(o => o.Id)
                .Select(o => o.CostCenter == null
                    ? null
                    : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Exported columns in legacy column order, with the collapsed org-unit columns standing in for
    /// legacy's OrgLevel1..7 / Previous OrgLevel2..4. OID is omitted -- a surrogate key.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeTransferReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeTransferReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("TransferDate", r => r.TransferDate),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("PreviousOrgLevel2", r => r.PreviousOrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("PreviousOrgLevel3", r => r.PreviousOrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("PreviousOrgLevel4", r => r.PreviousOrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocationName),
            new("PreviousWorkLocation", r => r.PreviousWorkLocationName),
            new("CostCenter", r => r.CostCenterName),
            new("PreviousCostCenter", r => r.PreviousCostCenterName),
            new("Country", r => r.CountryName)
        };
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// Also fills legacy's "Previous OrgLevel2/3/4" from the PREVIOUS assignment's org unit.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeTransferReportRowDto> rows)
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

            // Legacy rendered ONLY levels 2, 3 and 4 of the previous assignment (lines 610/613/616).
            var previousLevels = lookup.GetLevelNames(row.PreviousOrganizationUnitId);
            row.PreviousOrgLevel2 = previousLevels[1];
            row.PreviousOrgLevel3 = previousLevels[2];
            row.PreviousOrgLevel4 = previousLevels[3];
        }
    }
}
