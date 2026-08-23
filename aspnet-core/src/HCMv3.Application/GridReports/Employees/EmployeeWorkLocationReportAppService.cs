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
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Work Location analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeWorkLocation.ascx(.cs)</c>.
///
/// Ports <c>Table1()</c> (lines 513-683). The row source is <see cref="EmployeeOrgAssignment"/> rows
/// overlapping the requested window; note the subtlety that only the WorkLocation column comes from the
/// row itself -- JobTitle, the org unit and the country all come from a SECOND lookup of the assignment
/// current AS OF TODAY (legacy's <c>empOrgAss</c> sub-query is filtered on <c>DateTime.Now</c>, lines
/// 601-603), so a historical row can legitimately show today's job title. That is legacy behaviour and
/// is reproduced, not corrected.
///
/// DEVIATION 1 -- OrgLevel1..OrgLevel7 collapse to a single org-unit column; see
/// <see cref="EmployeeWorkLocationReportRowDto"/> remarks.
///
/// DEVIATION 2 -- legacy's "Remarks" column is a DateTime holding the last day in service; it is exposed
/// under its real meaning and typed nullable. See the row DTO remarks.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing: the drill-up/drill-down session stack (single level),
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (line 686+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeWorkLocation)]
public class EmployeeWorkLocationReportAppService : GridReportAppServiceBase,
    IEmployeeWorkLocationReportAppService
{
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;

    public EmployeeWorkLocationReportAppService(
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository)
    {
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeWorkLocation";

    protected override string ScreenCaption => "Employee Work Location";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeWorkLocationReportRowDto>> GetEmployeeWorkLocationReport(
        GetEmployeeWorkLocationReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildParameterString(input, WorkLocationParameterPart(input)),
            totalCount);

        return new PagedResultDto<EmployeeWorkLocationReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeWorkLocationReportToExcel(GetEmployeeWorkLocationReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the whole filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeWorkLocation.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input, WorkLocationParameterPart(input)));
    }

    /// <summary>
    /// Legacy appended the work-location filter to the logged parameter string
    /// (uscRptEmployeeWorkLocation.ascx.cs:503-504).
    /// </summary>
    private static string WorkLocationParameterPart(GetEmployeeWorkLocationReportInput input)
    {
        return input.WorkLocationCode.IsNullOrWhiteSpace()
            ? null
            : "Work Location : " + input.WorkLocationCode;
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeWorkLocation.ascx.cs:513-683).
    /// </summary>
    private async Task<IQueryable<EmployeeWorkLocationReportRowDto>> BuildQueryAsync(
        GetEmployeeWorkLocationReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;
        var workLocationCode = input.WorkLocationCode;
        var today = Today;

        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        var query = _employeeOrgAssignmentRepository.GetAll()
            // Legacy lines 578-579: !(r.StartDate.Date > dtTo.Date || r.EndDate.Date < dtFrom.Date).
            .Where(o => o.StartDate.Date <= toDate && o.EndDate.Date >= fromDate)
            // Legacy lines 584-585.
            .WhereIf(employeeStatus.HasValue, o => o.Employee.Status == employeeStatus.Value)
            // Legacy lines 588-589: the report-specific cmbWorkLocation, matched on the CODE.
            .WhereIf(!workLocationCode.IsNullOrWhiteSpace(),
                o => o.WorkLocation != null && o.WorkLocation.Code == workLocationCode)
            // Legacy lines 594-595.
            .WhereIf(employeeCodes.Count > 0, o => employeeCodes.Contains(o.Employee.Code))
            // Legacy line 598 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, o => rlsEmployeeIds.Contains(o.EmployeeId));

        // Legacy's empOrgAss sub-query -- the assignment current AS OF TODAY, not as of the row.
        var currentAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(c => c.StartDate.Date <= today && c.EndDate.Date >= today);

        // Legacy's empExit sub-query (lines 620-626): non-revoked separations only.
        var separations = _employeeSeparationRepository.GetAll()
            .Where(s => s.RevokeDate == null);

        return query.Select(o => new EmployeeWorkLocationReportRowDto
        {
            // Legacy "OID" column (line 651). Exposed as Id because it is the row key.
            Id = o.Id,
            EmployeeCode = o.Employee.Code,
            EmployeeName = useArabicNames ? o.Employee.FullNameAr : o.Employee.FullNameEn,
            CivilId = o.Employee.CivilID,
            NationalityName = o.Employee.Nationality == null
                ? null
                : (useArabicNames ? o.Employee.Nationality.NameAr : o.Employee.Nationality.NameEn),

            // ---- from TODAY's assignment (legacy lines 601-618/659-666) ----
            JobTitleName = currentAssignments
                .Where(c => c.EmployeeId == o.EmployeeId)
                .OrderBy(c => c.StartDate)
                .ThenBy(c => c.Id)
                .Select(c => c.JobTitle == null
                    ? null
                    : (useArabicNames ? c.JobTitle.NameAr : c.JobTitle.NameEn))
                .FirstOrDefault(),
            OrganizationUnitName = currentAssignments
                .Where(c => c.EmployeeId == o.EmployeeId)
                .OrderBy(c => c.StartDate)
                .ThenBy(c => c.Id)
                .Select(c => c.OrganizationUnit.DisplayName)
                .FirstOrDefault(),
            CountryName = currentAssignments
                .Where(c => c.EmployeeId == o.EmployeeId)
                .OrderBy(c => c.StartDate)
                .ThenBy(c => c.Id)
                .Select(c => c.PayrollArea == null || c.PayrollArea.Country == null
                    ? null
                    : (useArabicNames ? c.PayrollArea.Country.NameAr : c.PayrollArea.Country.NameEn))
                .FirstOrDefault(),

            // ---- from the ROW's own assignment (legacy line 640) ----
            WorkLocationName = o.WorkLocation == null
                ? null
                : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn),

            // Legacy lines 636-637: submitted on or before the FROM date, most recent last day first.
            LastDayInService = separations
                .Where(s => s.EmployeeId == o.EmployeeId && s.SubmissionDate <= fromDate)
                .OrderByDescending(s => s.LastDayInService)
                .Select(s => (System.DateTime?)s.LastDayInService)
                .FirstOrDefault()
        });
    }

    /// <summary>Exported columns in legacy column order. OID is omitted -- a surrogate key.</summary>
    private static IReadOnlyList<GridReportColumn<EmployeeWorkLocationReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeWorkLocationReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("CivilId", r => r.CivilId),
            new("Nationality", r => r.NationalityName),
            new("OrganizationUnit", r => r.OrganizationUnitName),
            new("WorkLocation", r => r.WorkLocationName),
            new("Country", r => r.CountryName),
            new("LastDayInService", r => r.LastDayInService)
        };
    }
}
