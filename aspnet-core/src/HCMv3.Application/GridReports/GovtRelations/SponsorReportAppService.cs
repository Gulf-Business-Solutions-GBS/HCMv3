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
using HCMv3.GridReports.GovtRelations.Dto;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.GovtRelations;

/// <summary>
/// Sponsor analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/GovtRelations/uscRptSponsor.ascx(.cs)</c>, <c>Table1()</c>.
/// An unconditional, unfiltered listing of every Sponsor -- no employee/date/RLS concept, exactly like
/// legacy. See the row DTO remarks for the legacy Code/Oid column defect (reproduced faithfully).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_Sponsor)]
public class SponsorReportAppService : GridReportAppServiceBase, ISponsorReportAppService
{
    private readonly IRepository<Sponsor> _sponsorRepository;

    public SponsorReportAppService(IRepository<Sponsor> sponsorRepository)
    {
        _sponsorRepository = sponsorRepository;
    }

    protected override string LegacyScreenName => "uscRptSponsor";

    protected override string ScreenCaption => "Sponsor";

    [HttpPost]
    public async Task<PagedResultDto<SponsorReportRowDto>> GetSponsorReport(GetSponsorReportInput input)
    {
        input.Normalize();

        var query = BuildQuery();

        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<SponsorReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetSponsorReportToExcel(GetSponsorReportInput input)
    {
        input.Normalize();

        var query = BuildQuery();

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        return await ExportToFileAsync("Sponsor.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for projection, shared by the paged read and the export. Ports legacy
    /// <c>Table1()</c> -- no filters at all in legacy.
    /// </summary>
    private IQueryable<SponsorReportRowDto> BuildQuery()
    {
        var useArabicNames = UseArabicNames;

        return _sponsorRepository.GetAll().Select(s => new SponsorReportRowDto
        {
            Id = s.Id,
            // LEGACY DEFECT FLAGGED FOR REVIEW: legacy's "Code" column value is the Oid, not the business
            // Code -- see the row DTO remarks. Reproduced faithfully; the real code is SponsorCode.
            Code = s.Id,
            SponsorCode = s.Code,
            Name = useArabicNames ? s.NameAr : s.NameEn,
            OrganizationUnitName = s.OrganizationUnit == null ? string.Empty : s.OrganizationUnit.DisplayName,
            UnifiedNum = s.UnifiedNum,
            SocialSecurityNum = s.SocialSecurityNum,
            MOSALID = s.MOSALID,
            GovernorateName = s.Governorate == null ? string.Empty : (useArabicNames ? s.Governorate.NameAr : s.Governorate.NameEn),
            Email = s.Email,
            Gender = s.Gender,
            Profession = s.Profession,
            Building = s.Building,
            Street = s.Street,
            Area = s.Area,
            Telephone = s.Telephone,
            NationalityName = s.Nationality == null ? string.Empty : (useArabicNames ? s.Nationality.NameAr : s.Nationality.NameEn),
            BusinessTypeName = s.BusinessType == null ? string.Empty : (useArabicNames ? s.BusinessType.NameAr : s.BusinessType.NameEn),
            Block = s.Block,
            Fax = s.Fax,
            POBox = s.POBox
        });
    }

    private static IReadOnlyList<GridReportColumn<SponsorReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<SponsorReportRowDto>>
        {
            new("Code", r => r.Code),
            new("Name", r => r.Name),
            new("OrganizationUnit", r => r.OrganizationUnitName),
            new("UnifiedNum", r => r.UnifiedNum),
            new("SocialSecurityNum", r => r.SocialSecurityNum),
            new("MOSALID", r => r.MOSALID),
            new("Governorate", r => r.GovernorateName),
            new("Email", r => r.Email),
            new("Gender", r => r.Gender),
            new("profession", r => r.Profession),
            new("Building", r => r.Building),
            new("Street", r => r.Street),
            new("Area", r => r.Area),
            new("Telephone", r => r.Telephone),
            new("Nationality", r => r.NationalityName),
            new("BusinessType", r => r.BusinessTypeName),
            new("Block", r => r.Block),
            new("Fax", r => r.Fax),
            new("POBox", r => r.POBox)
        };
    }
}
