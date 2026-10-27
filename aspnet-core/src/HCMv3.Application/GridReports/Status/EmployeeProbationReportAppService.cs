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
/// Employee Probation analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeProbation.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 460-654).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeProbation)]
public class EmployeeProbationReportAppService : GridReportAppServiceBase, IEmployeeProbationReportAppService
{
    private static readonly string[] CivilIdDocumentTypeNames = { "CID", "Civil Id" };

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeProbationReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeDocument> employeeDocumentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeDocumentRepository = employeeDocumentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeProbation";

    protected override string ScreenCaption => "Employee Probation";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeProbationReportRowDto>> GetEmployeeProbationReport(
        GetEmployeeProbationReportInput input)
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

        return new PagedResultDto<EmployeeProbationReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeProbationReportToExcel(GetEmployeeProbationReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeProbation.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeProbation.ascx.cs:460-654).
    /// </summary>
    private async Task<IQueryable<EmployeeProbationReportRowDto>> BuildQueryAsync(GetEmployeeProbationReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var today = Today;

        var query = _employeeRepository.GetAll()
            // Legacy lines 535-536: dtFrom <= ProbationEndDate <= dtTo.
            .Where(e => fromDate <= e.ProbationEndDate && toDate >= e.ProbationEndDate)
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        // Legacy lines 573-576: most recent CID/Civil Id document by ExpiryDate desc, no date-window.
        var civilIdDocuments = _employeeDocumentRepository.GetAll()
            .Where(d => d.DocumentType != null && CivilIdDocumentTypeNames.Contains(d.DocumentType.NameEn));

        return query.Select(e => new EmployeeProbationReportRowDto
        {
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn,
            NationalityName = e.Nationality == null ? string.Empty : (useArabicNames ? e.Nationality.NameAr : e.Nationality.NameEn),
            OrganizationUnitId = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            JoiningDate = e.JoiningDate,
            ProbationDays = e.ProbationDays,
            ProbationEndDate = e.ProbationEndDate,
            IssueDate = civilIdDocuments
                .Where(d => d.EmployeeId == e.Id)
                .OrderByDescending(d => d.ExpiryDate).ThenByDescending(d => d.Id)
                .Select(d => (System.DateTime?)d.IssueDate)
                .FirstOrDefault(),
            ExpiryDate = civilIdDocuments
                .Where(d => d.EmployeeId == e.Id)
                .OrderByDescending(d => d.ExpiryDate).ThenByDescending(d => d.Id)
                .Select(d => (System.DateTime?)d.ExpiryDate)
                .FirstOrDefault(),
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? string.Empty : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            // Legacy line 603: always empty -- the legacy projection never assigns this field.
            Description = string.Empty,
            CountryName = currentOrgAssignments
                .Where(o => o.EmployeeId == e.Id && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null ? null : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault()
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeProbationReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeProbationReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("Nationality", r => r.NationalityName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("JoiningDate", r => r.JoiningDate),
            new("ProbationDays", r => r.ProbationDays),
            new("ProbationEndDate", r => r.ProbationEndDate),
            new("IssueDate", r => r.IssueDate),
            new("ExpiryDate", r => r.ExpiryDate),
            new("WorkLocation", r => r.WorkLocationName),
            new("Description", r => r.Description),
            new("Country", r => r.CountryName)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeProbationReportRowDto> rows)
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
