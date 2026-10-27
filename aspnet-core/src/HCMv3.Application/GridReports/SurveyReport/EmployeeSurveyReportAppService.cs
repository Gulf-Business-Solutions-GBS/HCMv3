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
using HCMv3.GridReports.SurveyReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SurveyManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.SurveyReport;

/// <summary>
/// Employee Survey analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/SurveyReport/uscRptEmployeeSurvey.ascx(.cs)</c>,
/// <c>Table1()</c>. Row source is <see cref="EmployeeSurveyDetail"/> (one row per answered question),
/// filtered on the parent Survey's date window, employee status/code and survey code, with
/// JobTitle/WorkLocation/OrgLevel1..7 resolved from the assignment current AS OF TODAY -- same pattern as
/// <c>EmployeeTrainingReportAppService</c>.
///
/// NOT PORTED (DevExpress/WebForms UI plumbing): drill-up/drill-down stack, ColumnChooser JSON
/// persistence, callback-panel state machine, PDF export, dead <c>Table1_Old()</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeSurvey)]
public class EmployeeSurveyReportAppService : GridReportAppServiceBase, IEmployeeSurveyReportAppService
{
    private readonly IRepository<EmployeeSurveyDetail> _employeeSurveyDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeSurveyReportAppService(
        IRepository<EmployeeSurveyDetail> employeeSurveyDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeSurveyDetailRepository = employeeSurveyDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeSurvey";

    protected override string ScreenCaption => "Employee Survey";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSurveyReportRowDto>> GetEmployeeSurveyReport(
        GetEmployeeSurveyReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildSurveyParameterString(input), totalCount);

        return new PagedResultDto<EmployeeSurveyReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeSurveyReportToExcel(GetEmployeeSurveyReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync("EmployeeSurvey.xlsx", rows, GetExportColumns(), BuildSurveyParameterString(input));
    }

    private string BuildSurveyParameterString(GetEmployeeSurveyReportInput input)
    {
        return BuildParameterString(
            input,
            input.SurveyCode.IsNullOrWhiteSpace() ? null : "Survey : " + input.SurveyCode);
    }

    private async Task<IQueryable<EmployeeSurveyReportRowDto>> BuildQueryAsync(GetEmployeeSurveyReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var surveyCode = input.SurveyCode;
        var today = Today;

        var query = _employeeSurveyDetailRepository.GetAll()
            .Where(d => d.EmployeeSurvey.Survey != null)
            // Legacy: Survey.StartDate/EndDate window overlap.
            .Where(d => d.EmployeeSurvey.Survey.StartDate.Date <= toDate && d.EmployeeSurvey.Survey.EndDate.Date >= fromDate)
            .WhereIf(employeeStatus.HasValue, d => d.EmployeeSurvey.Employee.Status == employeeStatus.Value)
            .WhereIf(!surveyCode.IsNullOrWhiteSpace(), d => d.EmployeeSurvey.Survey.Code == surveyCode)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.EmployeeSurvey.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeSurvey.EmployeeId));

        var currentAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        return query.Select(d => new EmployeeSurveyReportRowDto
        {
            Id = d.Id,
            EmployeeCode = d.EmployeeSurvey.Employee.Code,
            EmployeeName = useArabicNames ? d.EmployeeSurvey.Employee.FullNameAr : d.EmployeeSurvey.Employee.FullNameEn,
            SurveyName = d.EmployeeSurvey.Survey == null
                ? string.Empty
                : (useArabicNames ? d.EmployeeSurvey.Survey.NameAr : d.EmployeeSurvey.Survey.NameEn),
            StartDate = d.EmployeeSurvey.Survey.StartDate,
            EndDate = d.EmployeeSurvey.Survey.EndDate,
            JobTitleName = currentAssignments
                .Where(o => o.EmployeeId == d.EmployeeSurvey.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? null : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            WorkLocationName = currentAssignments
                .Where(o => o.EmployeeId == d.EmployeeSurvey.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? null : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentAssignments
                .Where(o => o.EmployeeId == d.EmployeeSurvey.EmployeeId)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            Question = d.Question == null ? string.Empty : d.Question.Question,
            Answer = d.Answer
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeSurveyReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeSurveyReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Survey", r => r.SurveyName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("JobTitle", r => r.JobTitleName),
            new("WorkLocation", r => r.WorkLocationName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Question", r => r.Question),
            new("Answer", r => r.Answer)
        };
    }

    private async Task StampOrgLevelsAsync(List<EmployeeSurveyReportRowDto> rows)
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
