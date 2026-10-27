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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.SurveyReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SurveyManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.SurveyReport;

/// <summary>
/// Survey Status analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/SurveyReport/uscRptSurveyStatus.ascx(.cs)</c>,
/// <c>Table1()</c>. Rows are Surveys, not employees, so there is no RLS to apply (legacy has none here
/// either). The completion percentage is computed from the whole tenant's active-employee count and the
/// whole <c>EmployeeSurvey</c> table, exactly as legacy did (no RLS narrowing there either).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SurveyStatus)]
public class SurveyStatusReportAppService : GridReportAppServiceBase, ISurveyStatusReportAppService
{
    private readonly IRepository<Survey> _surveyRepository;
    private readonly IRepository<EmployeeSurvey> _employeeSurveyRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public SurveyStatusReportAppService(
        IRepository<Survey> surveyRepository,
        IRepository<EmployeeSurvey> employeeSurveyRepository,
        IRepository<Employee> employeeRepository)
    {
        _surveyRepository = surveyRepository;
        _employeeSurveyRepository = employeeSurveyRepository;
        _employeeRepository = employeeRepository;
    }

    protected override string LegacyScreenName => "uscRptSurveyStatus";

    protected override string ScreenCaption => "Survey Status";

    [HttpPost]
    public async Task<PagedResultDto<SurveyStatusReportRowDto>> GetSurveyStatusReport(GetSurveyStatusReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var totalCount = rows.Count;

        var pagedRows = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildStatusParameterString(input), totalCount);

        return new PagedResultDto<SurveyStatusReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetSurveyStatusReportToExcel(GetSurveyStatusReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("SurveyStatus.xlsx", orderedRows, GetExportColumns(), BuildStatusParameterString(input));
    }

    private string BuildStatusParameterString(GetSurveyStatusReportInput input)
    {
        return BuildParameterString(
            input,
            input.SurveyCode.IsNullOrWhiteSpace() ? null : "Survey : " + input.SurveyCode);
    }

    /// <summary>
    /// Ports legacy <c>Table1()</c>: percentage requires the whole active-employee count in memory, so the
    /// survey list is materialized once here and shared by the paged read and the export.
    /// </summary>
    private async Task<List<SurveyStatusReportRowDto>> BuildRowsAsync(GetSurveyStatusReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var surveyCode = input.SurveyCode;
        var useArabicNames = UseArabicNames;

        var surveys = await _surveyRepository.GetAll()
            // Legacy: window overlap against the survey's own StartDate/EndDate.
            .Where(s => s.StartDate.Date <= toDate && s.EndDate.Date >= fromDate)
            .WhereIf(!surveyCode.IsNullOrWhiteSpace(), s => s.Code == surveyCode)
            .Select(s => new
            {
                s.Id,
                s.Code,
                Name = useArabicNames ? s.NameAr : s.NameEn,
                s.StartDate,
                s.EndDate
            })
            .ToListAsync();

        // Legacy: object_Space.GetObjectsQuery<Employee>().Where(Status == Active).Count() -- whole tenant.
        var activeEmployeeCount = await _employeeRepository.GetAll()
            .CountAsync(e => e.Status == EmployeeStatus.Active);

        var rows = new List<SurveyStatusReportRowDto>();

        foreach (var survey in surveys)
        {
            var responded = activeEmployeeCount > 0
                ? await _employeeSurveyRepository.GetAll().CountAsync(es => es.SurveyId == survey.Id)
                : 0;

            // Legacy: Count() * 100 / empCount, INTEGER division, 0 when empCount is 0.
            var completePercent = activeEmployeeCount > 0 ? responded * 100 / activeEmployeeCount : 0;

            rows.Add(new SurveyStatusReportRowDto
            {
                Id = survey.Id,
                Code = survey.Code,
                Name = survey.Name,
                StartDate = survey.StartDate,
                EndDate = survey.EndDate,
                CompletePercent = completePercent
            });
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<SurveyStatusReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<SurveyStatusReportRowDto>>
        {
            new("Code", r => r.Code),
            new("Name", r => r.Name),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("Complete", r => r.CompletePercent)
        };
    }
}
