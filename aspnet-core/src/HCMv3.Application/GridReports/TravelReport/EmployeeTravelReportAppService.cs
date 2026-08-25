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
using HCMv3.GridReports.TravelReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TravelManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TravelReport;

/// <summary>
/// Employee Travel analysis grid report -- port of legacy <c>TravelReport/uscRptEmployeeTravel.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 529-709).
///
/// NOT PORTED: the drill-up/drill-down session stack (legacy also has Table2/Table3 transaction-detail
/// drill-downs -- lines 710+ -- out of scope, same as every other report in this family), the ColumnChooser
/// JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeTravel)]
public class EmployeeTravelReportAppService : GridReportAppServiceBase, IEmployeeTravelReportAppService
{
    private readonly IRepository<Travel> _travelRepository;
    private readonly IRepository<TravelFeedback> _travelFeedbackRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeTravelReportAppService(
        IRepository<Travel> travelRepository,
        IRepository<TravelFeedback> travelFeedbackRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _travelRepository = travelRepository;
        _travelFeedbackRepository = travelFeedbackRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeTravel";

    protected override string ScreenCaption => "Employee Travel";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTravelReportRowDto>> GetEmployeeTravelReport(GetEmployeeTravelReportInput input)
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

        return new PagedResultDto<EmployeeTravelReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeTravelReportToExcel(GetEmployeeTravelReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeTravel.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    private async Task<IQueryable<EmployeeTravelReportRowDto>> BuildQueryAsync(GetEmployeeTravelReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var travelTypeCode = input.TravelTypeCode;
        var today = Today;

        var query = _travelRepository.GetAll()
            // Legacy lines 594-598: overlap test against [dtFrom, dtTo].
            .Where(t => t.StartDate.Date <= toDate && t.EndDate.Date >= fromDate)
            .WhereIf(employeeStatus.HasValue, t => t.Employee.Status == employeeStatus.Value)
            .WhereIf(!travelTypeCode.IsNullOrWhiteSpace(), t => t.TravelType.Code == travelTypeCode)
            .WhereIf(employeeCodes.Count > 0, t => employeeCodes.Contains(t.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, t => rlsEmployeeIds.Contains(t.EmployeeId));

        var orgAss = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        return query.Select(t => new EmployeeTravelReportRowDto
        {
            Id = t.Id,
            EmployeeCode = t.Employee.Code,
            EmployeeName = useArabicNames ? t.Employee.FullNameAr : t.Employee.FullNameEn,
            Applicant = "",
            TravelCode = t.Code,
            TravelType = t.TravelType == null ? "" : (useArabicNames ? t.TravelType.NameAr : t.TravelType.NameEn),
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            JobTitle = orgAss
                .Where(o => o.EmployeeId == t.EmployeeId && o.JobTitleId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                .FirstOrDefault(),
            WorkLocation = orgAss
                .Where(o => o.EmployeeId == t.EmployeeId && o.WorkLocationId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn)
                .FirstOrDefault(),
            OrganizationUnitId = orgAss
                .Where(o => o.EmployeeId == t.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            TotalTravelCost = t.TotalTravelCost,
            // Legacy lines 622-628/662-663: sum of TravelFeedback.ClaimAmount for this Employee+Travel.
            ClaimAmount = _travelFeedbackRepository.GetAll()
                .Where(f => f.EmployeeId == t.EmployeeId && f.TravelId == t.Id)
                .Sum(f => (double?)f.ClaimAmount) ?? 0
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeTravelReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeTravelReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("Applicant", r => r.Applicant),
            new("TravelCode", r => r.TravelCode),
            new("TravelType", r => r.TravelType),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("JobTitle", r => r.JobTitle),
            new("WorkLocation", r => r.WorkLocation),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("TotalTravelCost", r => r.TotalTravelCost),
            new("ClaimAmount", r => r.ClaimAmount)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeTravelReportRowDto> rows)
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
