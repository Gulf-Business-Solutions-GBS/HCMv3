using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Recruitment.Dto;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Recruitment;

/// <summary>
/// Vacancy Status analysis grid report -- port of legacy <c>uscRptVacancyStatus.ascx.cs</c>. Its whole
/// data contract is delegated to <see cref="VacancyStatusReportQueryHelper"/>, shared with
/// <see cref="CandidateOnbInitReportAppService"/> because the two legacy screens' <c>Table1()</c> are
/// byte-for-byte identical -- see <see cref="Dto.VacancyStatusReportRowDto"/> remarks.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the commented-out
/// <c>Table2</c> applicant drill (dead code in legacy).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_VacancyStatus)]
public class VacancyStatusReportAppService : GridReportAppServiceBase, IVacancyStatusReportAppService
{
    private readonly VacancyStatusReportQueryHelper _queryHelper;

    public VacancyStatusReportAppService(VacancyStatusReportQueryHelper queryHelper)
    {
        _queryHelper = queryHelper;
    }

    protected override string LegacyScreenName => "uscRptVacancyStatus";

    protected override string ScreenCaption => "Vacancy Status";

    [HttpPost]
    public async Task<PagedResultDto<VacancyStatusReportRowDto>> GetVacancyStatusReport(GetVacancyStatusReportInput input)
    {
        input.Normalize();

        var baseQuery = _queryHelper.BuildBaseQuery(input.VacancyId, input.VacancyStatus, UseArabicNames);

        var totalCount = await baseQuery.CountAsync();

        var source = await baseQuery
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var rows = source.Select(ToRow).ToList();
        await _queryHelper.StampAsync(rows, source, UseArabicNames);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<VacancyStatusReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetVacancyStatusReportToExcel(GetVacancyStatusReportInput input)
    {
        input.Normalize();

        var baseQuery = _queryHelper.BuildBaseQuery(input.VacancyId, input.VacancyStatus, UseArabicNames);

        var source = await baseQuery
            .OrderBy(input.Sorting)
            .ToListAsync();

        var rows = source.Select(ToRow).ToList();
        await _queryHelper.StampAsync(rows, source, UseArabicNames);

        return await ExportToFileAsync(
            "VacancyStatus.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    private static VacancyStatusReportRowDto ToRow(VacancyStatusReportQueryHelper.VacancyRow source)
    {
        return new VacancyStatusReportRowDto
        {
            Id = source.VacancyId,
            VacancyId = source.VacancyId,
            Code = source.Code,
            VacancyName = source.VacancyName,
            PublishOn = source.PublishOn,
            ClosedOn = source.ClosedOn
        };
    }

    private static IReadOnlyList<GridReportColumn<VacancyStatusReportRowDto>> GetExportColumns()
    {
        var columns = new List<GridReportColumn<VacancyStatusReportRowDto>>
        {
            new("Code", r => r.Code),
            new("VacancyId", r => r.VacancyId),
            new("VacancyName", r => r.VacancyName),
            new("Company", r => r.Company),
            new("Department", r => r.Department),
            new("PublishOn", r => r.PublishOn),
            new("ClosedOn", r => r.ClosedOn)
        };

        foreach (var status in System.Enum.GetValues(typeof(HCMv3.Recruitment.AppStatus)).Cast<HCMv3.Recruitment.AppStatus>())
        {
            var s = status;
            columns.Add(GridReportColumn<VacancyStatusReportRowDto>.FromLiteralHeader(
                s.ToString(),
                r => r.StatusCounts.FirstOrDefault(c => c.Status == s)?.Count ?? 0));
        }

        return columns;
    }
}
