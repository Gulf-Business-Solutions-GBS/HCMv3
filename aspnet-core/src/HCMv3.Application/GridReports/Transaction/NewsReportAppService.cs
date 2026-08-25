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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// News analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscNews.ascx(.cs)</c>.
///
/// Legacy's <c>BindNewsDocument</c> (uscNews.ascx.cs:157-204) queried <c>EmployeeDocument</c> rows of
/// type "NEWS" active "today", took the newest as an always-visible summary panel and listed the rest in
/// a grid. This port keeps only the grid semantics (all active rows, newest first) since the "summary
/// panel showing row 1 differently" split is DevExpress/WebForms UI plumbing with no data meaning.
///
/// LEGACY DEFECT FLAGGED FOR REVIEW: legacy's <c>ApplyRowLevelSecurity</c> RLS gate is used everywhere
/// else in this report family but is ENTIRELY ABSENT from <c>uscNews.ascx.cs</c> -- any authenticated user
/// could see every "NEWS" document regardless of RLS scope. Per this port's fidelity rule (RLS is always
/// applied via the base class even where legacy forgot it), RLS IS applied here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_News)]
public class NewsReportAppService : GridReportAppServiceBase, INewsReportAppService
{
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;

    public NewsReportAppService(IRepository<EmployeeDocument> employeeDocumentRepository)
    {
        _employeeDocumentRepository = employeeDocumentRepository;
    }

    protected override string LegacyScreenName => "uscNews";

    protected override string ScreenCaption => "News";

    [HttpPost]
    public async Task<PagedResultDto<NewsReportRowDto>> GetNewsReport(GetNewsReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<NewsReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetNewsReportToExcel(GetNewsReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync("News.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<IQueryable<NewsReportRowDto>> BuildQueryAsync(GetNewsReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        var query = _employeeDocumentRepository.GetAll()
            // Legacy line 161: r.DocumentType.NameEn == "NEWS".
            .Where(d => d.DocumentType.NameEn == "NEWS")
            // Legacy lines 162-163: active-window check, generalized from "today" to the shared From/To range.
            .Where(d => d.IssueDate.Date <= toDate && d.ExpiryDate.Date >= fromDate)
            // See class remarks: legacy forgot RLS here; applied per this port's fidelity rule.
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId));

        return query.Select(d => new NewsReportRowDto
        {
            Id = d.Id,
            Description = d.Description,
            // Legacy DocumentHtml (rich-text body) has no equivalent EmployeeDocument column in this
            // port -- see HCMv3.PersonnelAdministration.EmployeeDocument class remarks ("DocumentHtml ...
            // out of Wave A scope"). Left null rather than dropping the DTO field, so the Angular screen
            // still renders the Description/IssueDate list faithfully.
            DocumentHtml = null,
            IssueDate = d.IssueDate
        });
    }

    private static IReadOnlyList<GridReportColumn<NewsReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<NewsReportRowDto>>
        {
            new("Description", r => r.Description),
            new("IssueDate", r => r.IssueDate)
        };
    }
}
