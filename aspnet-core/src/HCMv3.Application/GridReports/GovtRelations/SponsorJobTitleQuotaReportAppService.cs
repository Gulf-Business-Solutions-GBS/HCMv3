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
using HCMv3.GovtRelationsManagement;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.GovtRelations.Dto;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.GovtRelations;

/// <summary>
/// Sponsor Job Title Quota analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/GovtRelations/uscRptSponsorJobTitleQuota.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 476-587). Flat listing of <see cref="SponsorJobTitle"/>, one optional filter on
/// Sponsor code, no employee/date/RLS concept.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SponsorJobTitleQuota)]
public class SponsorJobTitleQuotaReportAppService : GridReportAppServiceBase, ISponsorJobTitleQuotaReportAppService
{
    private readonly IRepository<SponsorJobTitle> _sponsorJobTitleRepository;

    public SponsorJobTitleQuotaReportAppService(IRepository<SponsorJobTitle> sponsorJobTitleRepository)
    {
        _sponsorJobTitleRepository = sponsorJobTitleRepository;
    }

    protected override string LegacyScreenName => "uscRptSponsorJobTitleQuota";

    protected override string ScreenCaption => "Sponsor Job Title Quota";

    [HttpPost]
    public async Task<PagedResultDto<SponsorJobTitleQuotaReportRowDto>> GetSponsorJobTitleQuotaReport(
        GetSponsorJobTitleQuotaReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildQuotaParameterString(input), totalCount);

        return new PagedResultDto<SponsorJobTitleQuotaReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetSponsorJobTitleQuotaReportToExcel(GetSponsorJobTitleQuotaReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        return await ExportToFileAsync(
            "SponsorJobTitleQuota.xlsx",
            rows,
            GetExportColumns(),
            BuildQuotaParameterString(input));
    }

    private static string BuildQuotaParameterString(GetSponsorJobTitleQuotaReportInput input)
    {
        return input.SponsorCode.IsNullOrWhiteSpace() ? string.Empty : "Sponsor : " + input.SponsorCode;
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>. <c>CitizenQuotaBalance</c>/<c>NonCitizenQuotaBalance</c> are computed
    /// here (Accrued - Used) since they are DTO-only in this port -- see the row DTO remarks.
    /// </summary>
    private IQueryable<SponsorJobTitleQuotaReportRowDto> BuildQuery(GetSponsorJobTitleQuotaReportInput input)
    {
        var sponsorCode = input.SponsorCode;
        var useArabicNames = UseArabicNames;

        var query = _sponsorJobTitleRepository.GetAll()
            .Where(r => r.Sponsor != null)
            // Legacy lines 532-536.
            .WhereIf(!sponsorCode.IsNullOrWhiteSpace(), r => r.Sponsor.Code == sponsorCode);

        return query.Select(r => new SponsorJobTitleQuotaReportRowDto
        {
            Id = r.Id,
            SponsorName = useArabicNames ? r.Sponsor.NameAr : r.Sponsor.NameEn,
            GovtJobTitleName = r.GovtJobTitle == null
                ? string.Empty
                : (useArabicNames ? r.GovtJobTitle.NameAr : r.GovtJobTitle.NameEn),
            GovtJobCategoryName = r.GovtJobTitle == null || r.GovtJobTitle.GovtJobCategory == null
                ? string.Empty
                : (useArabicNames ? r.GovtJobTitle.GovtJobCategory.NameAr : r.GovtJobTitle.GovtJobCategory.NameEn),
            NoofPosition = r.NoofPosition,
            CitizenPercent = r.CitizenPercent,
            CitizenQuotaAccrued = r.CitizenQuotaAccrued,
            CitizenQuotaUsed = r.CitizenQuotaUsed,
            CitizenQuotaBalance = r.CitizenQuotaAccrued - r.CitizenQuotaUsed,
            NonCitizenPercent = r.NonCitizenPercent,
            NonCitizenQuotaAccrued = r.NonCitizenQuotaAccrued,
            NonCitizenQuotaUsed = r.NonCitizenQuotaUsed,
            NonCitizenQuotaBalance = r.NonCitizenQuotaAccrued - r.NonCitizenQuotaUsed
        });
    }

    private static IReadOnlyList<GridReportColumn<SponsorJobTitleQuotaReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<SponsorJobTitleQuotaReportRowDto>>
        {
            new("Sponsor", r => r.SponsorName),
            new("GovtJobTitle", r => r.GovtJobTitleName),
            new("GovtJobCategory", r => r.GovtJobCategoryName),
            new("NoOfPosition", r => r.NoofPosition),
            new("CitizenPercent", r => r.CitizenPercent),
            new("CitizenQuotaAccrued", r => r.CitizenQuotaAccrued),
            new("CitizenQuotaUsed", r => r.CitizenQuotaUsed),
            new("CitizenQuotaBalance", r => r.CitizenQuotaBalance),
            new("NonCitizenPercent", r => r.NonCitizenPercent),
            new("NonCitizenQuotaAccrued", r => r.NonCitizenQuotaAccrued),
            new("NonCitizenQuotaUsed", r => r.NonCitizenQuotaUsed),
            new("NonCitizenQuotaBalance", r => r.NonCitizenQuotaBalance)
        };
    }
}
