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
/// Training Course analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TrainingReport/uscRptTrainingCourse.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 528-613). A flat master listing with a single optional code filter and no
/// employee/RLS concept at all.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_TrainingCourse)]
public class TrainingCourseReportAppService : GridReportAppServiceBase, ITrainingCourseReportAppService
{
    private readonly IRepository<TrainingCourse> _trainingCourseRepository;

    public TrainingCourseReportAppService(IRepository<TrainingCourse> trainingCourseRepository)
    {
        _trainingCourseRepository = trainingCourseRepository;
    }

    protected override string LegacyScreenName => "uscRptTrainingCourse";

    protected override string ScreenCaption => "Training Course";

    [HttpPost]
    public async Task<PagedResultDto<TrainingCourseReportRowDto>> GetTrainingCourseReport(
        GetTrainingCourseReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildCourseParameterString(input), totalCount);

        return new PagedResultDto<TrainingCourseReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetTrainingCourseReportToExcel(GetTrainingCourseReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        return await ExportToFileAsync("TrainingCourse.xlsx", rows, GetExportColumns(), BuildCourseParameterString(input));
    }

    /// <summary>Legacy <c>GetParameterStr()</c> (lines 507-523): only the training filter is logged.</summary>
    private static string BuildCourseParameterString(GetTrainingCourseReportInput input)
    {
        return input.TrainingCourseCode.IsNullOrWhiteSpace() ? string.Empty : "Training : " + input.TrainingCourseCode;
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>.
    /// </summary>
    private IQueryable<TrainingCourseReportRowDto> BuildQuery(GetTrainingCourseReportInput input)
    {
        var trainingCourseCode = input.TrainingCourseCode;

        var query = _trainingCourseRepository.GetAll()
            // Legacy lines 571-575.
            .WhereIf(!trainingCourseCode.IsNullOrWhiteSpace(), c => c.Code == trainingCourseCode);

        return query.Select(c => new TrainingCourseReportRowDto
        {
            Id = c.Id,
            Code = c.Code,
            NameEn = c.NameEn,
            NameAr = c.NameAr,
            DeliveryMethod = c.DeliveryMethod,
            Fees = c.Fees,
            Active = c.Active
        });
    }

    private static IReadOnlyList<GridReportColumn<TrainingCourseReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<TrainingCourseReportRowDto>>
        {
            new("Code", r => r.Code),
            new("NameEn", r => r.NameEn),
            new("NameAr", r => r.NameAr),
            new("DeliveryMethod", r => r.DeliveryMethod),
            new("Fees", r => r.Fees),
            new("Active", r => r.Active)
        };
    }
}
