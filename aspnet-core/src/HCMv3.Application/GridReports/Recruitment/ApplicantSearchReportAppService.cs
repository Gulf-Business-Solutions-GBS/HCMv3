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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Recruitment.Dto;
using HCMv3.Recruitment;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Recruitment;

/// <summary>
/// Applicant Search analysis grid report -- port of legacy <c>uscRptApplicantSearch.ascx.cs</c>, whose
/// entire data contract is <c>Table1()</c> (lines 633-767).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the commented-out
/// <c>Table2</c> (vacancy application-status drill, dead code in legacy).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ApplicantSearch)]
public class ApplicantSearchReportAppService : GridReportAppServiceBase, IApplicantSearchReportAppService
{
    private readonly IRepository<Applicant> _applicantRepository;

    public ApplicantSearchReportAppService(IRepository<Applicant> applicantRepository)
    {
        _applicantRepository = applicantRepository;
    }

    protected override string LegacyScreenName => "uscRptApplicantSearch";

    protected override string ScreenCaption => "Applicant Search";

    [HttpPost]
    public async Task<PagedResultDto<ApplicantSearchReportRowDto>> GetApplicantSearchReport(
        GetApplicantSearchReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<ApplicantSearchReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetApplicantSearchReportToExcel(GetApplicantSearchReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "ApplicantSearch.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection. Ports legacy <c>Table1()</c>
    /// (uscRptApplicantSearch.ascx.cs:633-767): the Nationality/Gender/MaritalStatus combos filter BEFORE
    /// projection, the Name/Age filters AFTER (legacy applied them on the anonymous projection, lines
    /// 715-733) -- reproduced here in the same order.
    /// </summary>
    private IQueryable<ApplicantSearchReportRowDto> BuildQuery(GetApplicantSearchReportInput input)
    {
        var useArabicNames = UseArabicNames;
        var currentYear = Today.Year;

        var applicants = _applicantRepository.GetAll()
            .WhereIf(input.NationalityId.HasValue, a => a.NationalityId == input.NationalityId.Value)
            .WhereIf(input.Gender.HasValue, a => a.Gender == input.Gender.Value)
            .WhereIf(input.MaritalStatusId.HasValue, a => a.MaritalStatusId == input.MaritalStatusId.Value);

        var query = applicants.Select(a => new ApplicantSearchReportRowDto
        {
            Id = a.Id,
            Code = a.Code,
            FirstName = a.FirstNameEn,
            LastName = a.LastNameEn,
            Email = a.Email,
            NationalityName = a.Nationality != null ? (useArabicNames ? a.Nationality.NameAr : a.Nationality.NameEn) : string.Empty,
            MaritalStatusName = a.MaritalStatus != null ? (useArabicNames ? a.MaritalStatus.NameAr : a.MaritalStatus.NameEn) : string.Empty,
            Gender = a.Gender.ToString(),
            // Legacy line 711: DateTime.Now.Year - rec.BirthDate.Year, birthday-naive, preserved as-is.
            Age = currentYear - a.BirthDate.Year,
            Mobile = a.Mobile
        });

        query = query
            .WhereIf(!string.IsNullOrWhiteSpace(input.FirstName), r => r.FirstName.Contains(input.FirstName))
            .WhereIf(!string.IsNullOrWhiteSpace(input.LastName), r => r.LastName.Contains(input.LastName));

        // Legacy lines 721-733: the age filter only applies when AgeFrom is present AND AgeTo != 0 AND
        // AgeTo is present; From < 0 clamps to 0, To <= 0 clamps to 100.
        if (input.AgeFrom.HasValue && input.AgeTo.HasValue && input.AgeTo.Value != 0)
        {
            var ageFrom = input.AgeFrom.Value < 0 ? 0 : input.AgeFrom.Value;
            var ageTo = input.AgeTo.Value <= 0 ? 100 : input.AgeTo.Value;
            query = query.Where(r => r.Age >= ageFrom && r.Age <= ageTo);
        }

        return query;
    }

    private static IReadOnlyList<GridReportColumn<ApplicantSearchReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<ApplicantSearchReportRowDto>>
        {
            new("Code", r => r.Code),
            new("FirstName", r => r.FirstName),
            new("LastName", r => r.LastName),
            new("Email", r => r.Email),
            new("Nationality", r => r.NationalityName),
            new("MaritalStatus", r => r.MaritalStatusName),
            new("Gender", r => r.Gender),
            new("Age", r => r.Age),
            new("Phone", r => r.Mobile)
        };
    }
}
