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
using HCMv3.GridReports.TrainingReport.Dto;
using HCMv3.SystemSecurity;
using HCMv3.TrainingManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TrainingReport;

/// <summary>
/// Training Schedule analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TrainingReport/uscRptTrainingSchedule.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 528-621). Flat master listing, date-window overlap plus an optional course
/// filter, no employee/RLS concept.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_TrainingSchedule)]
public class TrainingScheduleReportAppService : GridReportAppServiceBase, ITrainingScheduleReportAppService
{
    private readonly IRepository<TrainingSchedule> _trainingScheduleRepository;

    public TrainingScheduleReportAppService(IRepository<TrainingSchedule> trainingScheduleRepository)
    {
        _trainingScheduleRepository = trainingScheduleRepository;
    }

    protected override string LegacyScreenName => "uscRptTrainingSchedule";

    protected override string ScreenCaption => "Training Schedule";

    [HttpPost]
    public async Task<PagedResultDto<TrainingScheduleReportRowDto>> GetTrainingScheduleReport(
        GetTrainingScheduleReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildScheduleParameterString(input), totalCount);

        return new PagedResultDto<TrainingScheduleReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetTrainingScheduleReportToExcel(GetTrainingScheduleReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        return await ExportToFileAsync("TrainingSchedule.xlsx", rows, GetExportColumns(), BuildScheduleParameterString(input));
    }

    /// <summary>Legacy <c>GetParameterStr()</c> (lines 507-524): training filter + the two dates.</summary>
    private string BuildScheduleParameterString(GetTrainingScheduleReportInput input)
    {
        return BuildParameterString(
            input,
            input.TrainingCourseCode.IsNullOrWhiteSpace() ? null : "Training : " + input.TrainingCourseCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (lines 563-577).
    /// </summary>
    private IQueryable<TrainingScheduleReportRowDto> BuildQuery(GetTrainingScheduleReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var trainingCourseCode = input.TrainingCourseCode;
        var useArabicNames = UseArabicNames;

        var query = _trainingScheduleRepository.GetAll()
            // Legacy line 563: !(StartDate > dtTo || EndDate < dtFrom).
            .Where(s => s.StartDate.Date <= toDate && s.EndDate.Date >= fromDate)
            // Legacy lines 573-577.
            .WhereIf(!trainingCourseCode.IsNullOrWhiteSpace(), s => s.TrainingCourse.Code == trainingCourseCode);

        return query.Select(s => new TrainingScheduleReportRowDto
        {
            Id = s.Id,
            Code = s.Code,
            NameEn = s.NameEn,
            NameAr = s.NameAr,
            TrainingCourseName = s.TrainingCourse == null
                ? string.Empty
                : (useArabicNames ? s.TrainingCourse.NameAr : s.TrainingCourse.NameEn),
            StartDate = s.StartDate,
            EndDate = s.EndDate
        });
    }

    private static IReadOnlyList<GridReportColumn<TrainingScheduleReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<TrainingScheduleReportRowDto>>
        {
            new("Code", r => r.Code),
            new("NameEn", r => r.NameEn),
            new("NameAr", r => r.NameAr),
            new("TrainingCourse", r => r.TrainingCourseName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate)
        };
    }
}
