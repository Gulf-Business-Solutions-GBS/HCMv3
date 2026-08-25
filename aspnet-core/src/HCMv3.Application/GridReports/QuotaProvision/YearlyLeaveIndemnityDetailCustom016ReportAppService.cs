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
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Yearly Leave Indemnity Detail (Custom016) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptYearlyLeaveIndemnityDetail_Custom016.ascx(.cs)</c>.
/// Query logic lives in <see cref="YearlyLeaveIndemnityDetailReportQueryHelper"/>, shared verbatim with the
/// Custom023 variant -- see <see cref="Dto.GetYearlyLeaveIndemnityDetailReportInput"/> remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_YearlyLeaveIndemnityDetailCustom016)]
public class YearlyLeaveIndemnityDetailCustom016ReportAppService : GridReportAppServiceBase, IYearlyLeaveIndemnityDetailCustom016ReportAppService
{
    private readonly YearlyLeaveIndemnityDetailReportQueryHelper _queryHelper;

    public YearlyLeaveIndemnityDetailCustom016ReportAppService(YearlyLeaveIndemnityDetailReportQueryHelper queryHelper)
    {
        _queryHelper = queryHelper;
    }

    protected override string LegacyScreenName => "uscRptYearlyLeaveIndemnityDetail_Custom016";

    protected override string ScreenCaption => "Yearly Leave Indemnity Detail (Custom016)";

    [HttpPost]
    public async Task<PagedResultDto<YearlyLeaveIndemnityDetailReportRowDto>> GetYearlyLeaveIndemnityDetailCustom016Report(GetYearlyLeaveIndemnityDetailReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<YearlyLeaveIndemnityDetailReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetYearlyLeaveIndemnityDetailCustom016ReportToExcel(GetYearlyLeaveIndemnityDetailReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("YearlyLeaveIndemnityDetailCustom016.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<YearlyLeaveIndemnityDetailReportRowDto>> BuildRowsAsync(GetYearlyLeaveIndemnityDetailReportInput input)
    {
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var forfeitRule = timeSettings?.ForfeitRule ?? HCMv3.SystemSettings.ForfeitRule.Yearly;
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? System.DateTime.MinValue;

        return await _queryHelper.BuildRowsAsync(input, Today, UseArabicNames, forfeitRule, earliestEvaluationDate, GetRlsEmployeeIdsAsync);
    }

    private static IReadOnlyList<GridReportColumn<YearlyLeaveIndemnityDetailReportRowDto>> GetExportColumns()
    {
        return YearlyLeaveIndemnityDetailColumns.Get();
    }
}
