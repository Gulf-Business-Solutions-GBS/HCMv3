using System;
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
using HCMv3.GovtRelationsManagement;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Organization.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Organization;

/// <summary>
/// Organization Document analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Organization/uscRptOrganizationDocument.ascx(.cs)</c>.
///
/// This report has NO employee dimension (<c>OrganizationDocument</c> is org-unit-scoped, not
/// employee-scoped) so it does not call <see cref="GridReportAppServiceBase.GetRlsEmployeeIdsAsync"/> --
/// there is nothing to restrict by.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_OrganizationDocument)]
public class OrganizationDocumentReportAppService : GridReportAppServiceBase, IOrganizationDocumentReportAppService
{
    private readonly IRepository<OrganizationDocument> _organizationDocumentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;

    public OrganizationDocumentReportAppService(
        IRepository<OrganizationDocument> organizationDocumentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository)
    {
        _organizationDocumentRepository = organizationDocumentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
    }

    protected override string LegacyScreenName => "uscRptOrganizationDocument";

    protected override string ScreenCaption => "Organization Documents";

    [HttpPost]
    public async Task<PagedResultDto<OrganizationDocumentReportRowDto>> GetOrganizationDocumentReport(
        GetOrganizationDocumentReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<OrganizationDocumentReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetOrganizationDocumentReportToExcel(GetOrganizationDocumentReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        // Deliberately NOT PageBy: export must return every filtered row, not just the current page.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "OrganizationDocument.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptOrganizationDocument.ascx.cs:484-586).
    /// </summary>
    private IQueryable<OrganizationDocumentReportRowDto> BuildQuery(GetOrganizationDocumentReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var documentTypeId = input.DocumentTypeId;
        var useArabicNames = UseArabicNames;
        var today = Today;

        var query = _organizationDocumentRepository.GetAll()
            // Legacy lines 531-534: 4-way date-overlap OR predicate, reproduced verbatim.
            .Where(d => (d.IssueDate <= fromDate && fromDate <= d.ExpiryDate)
                        || (d.IssueDate <= toDate && toDate <= d.ExpiryDate)
                        || (fromDate <= d.IssueDate && d.IssueDate <= toDate)
                        || (fromDate <= d.ExpiryDate && d.ExpiryDate <= toDate))
            // Legacy line 540: r.DocumentType.Code == _code, applied only when a document type was picked.
            .WhereIf(documentTypeId.HasValue, d => d.DocumentTypeId == documentTypeId.Value);

        return query.Select(d => new OrganizationDocumentReportRowDto
        {
            Id = d.Id,
            // OrganizationUnit has no reverse navigation to OrgUnitProfile (1:1 companion row keyed the
            // other way round, see OrgUnitProfile class remarks), so the Arabic name is resolved with a
            // correlated subquery rather than a navigation property.
            OrganizationUnitName = useArabicNames
                ? (_orgUnitProfileRepository.GetAll()
                    .Where(p => p.OrganizationUnitId == d.OrganizationUnitId)
                    .Select(p => p.NameAr)
                    .FirstOrDefault() ?? d.OrganizationUnit.DisplayName)
                : d.OrganizationUnit.DisplayName,
            DocumentTypeName = d.DocumentType == null
                ? string.Empty
                : (useArabicNames ? d.DocumentType.NameAr : d.DocumentType.NameEn),
            IssueDate = d.IssueDate,
            ExpiryDate = d.ExpiryDate,
            DocumentPlace = d.DocumentPlace,
            DocumentNum = d.DocumentNum,
            Description = d.Comments,
            // Legacy computed OrganizationDocument.DaysLeft, dropped from the ported entity -- recomputed
            // here from ExpiryDate, the same value legacy stored.
            DaysLeft = (int)EF.Functions.DateDiffDay(today, d.ExpiryDate.Date)
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order (OrgUnit, DocumentType, IssueDate, ExpiryDate,
    /// DocumentPlace, DocumentNumber, Description, DaysLeft). OID omitted, same convention as
    /// <c>EmployeeContractReportAppService</c>.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<OrganizationDocumentReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<OrganizationDocumentReportRowDto>>
        {
            new("OrgUnit", r => r.OrganizationUnitName),
            new("DocumentType", r => r.DocumentTypeName),
            new("IssueDate", r => r.IssueDate),
            new("ExpiryDate", r => r.ExpiryDate),
            new("DocumentPlace", r => r.DocumentPlace),
            new("DocumentNumber", r => r.DocumentNum),
            new("Description", r => r.Description),
            new("DaysLeft", r => r.DaysLeft)
        };
    }
}
