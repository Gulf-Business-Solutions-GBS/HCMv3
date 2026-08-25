using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Recruitment.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.Recruitment;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Recruitment;

/// <summary>
/// Shared query for the Vacancy Status and Candidate Onboarding Init analysis grid reports -- their legacy
/// data contracts (<c>Table1()</c>) are byte-for-byte identical (see
/// <see cref="Dto.VacancyStatusReportRowDto"/> remarks), so both app services delegate here instead of
/// duplicating the query and risking drift.
///
/// Ports legacy <c>Table1()</c> (uscRptVacancyStatus.ascx.cs:571-682 / uscRptCandidateOnbInit.ascx.cs:570-681)
/// plus its helpers <c>GetCurrentCompany</c> and <c>GetStatusCountVacancy</c>.
/// </summary>
public class VacancyStatusReportQueryHelper : ITransientDependency
{
    private readonly IRepository<ApplicationStatus> _applicationStatusRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public VacancyStatusReportQueryHelper(
        IRepository<ApplicationStatus> applicationStatusRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _applicationStatusRepository = applicationStatusRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    /// <summary>
    /// Distinct (Vacancy, PublishOn, ClosingDate) rows matching the filters, in legacy column order. The
    /// per-status counts and Company/Department are stamped afterwards by <see cref="StampAsync"/> --
    /// legacy computed them per-row too (<c>GetCurrentCompany</c>/<c>GetStatusCountVacancy</c>), just
    /// inline; separating them here keeps this query a plain, pageable <c>IQueryable</c>.
    /// </summary>
    public IQueryable<VacancyRow> BuildBaseQuery(int? vacancyId, AppStatus? vacancyStatus, bool useArabicNames)
    {
        var query = _applicationStatusRepository.GetAll()
            .Where(r => r.Vacancy != null && r.ApplicantCV != null);

        if (vacancyId.HasValue)
        {
            query = query.Where(r => r.VacancyId == vacancyId.Value);
        }

        if (vacancyStatus.HasValue)
        {
            query = query.Where(r => r.AppStatus == vacancyStatus.Value);
        }

        return query
            .Select(r => new VacancyRow
            {
                VacancyId = r.VacancyId.Value,
                Code = r.Vacancy.JobTitle != null ? r.Vacancy.JobTitle.Code : string.Empty,
                VacancyName = r.Vacancy.JobTitle != null
                    ? (useArabicNames ? r.Vacancy.JobTitle.NameAr : r.Vacancy.JobTitle.NameEn)
                    : string.Empty,
                OrganizationUnitId = r.Vacancy.OrganizationUnitId,
                PublishOn = r.Vacancy.PublishOn,
                ClosedOn = r.Vacancy.ClosingDate
            })
            .Distinct();
    }

    /// <summary>
    /// Stamps Company, Department and the per-status counts onto already-materialized rows -- shared by the
    /// paged read and the export so the two cannot drift.
    /// </summary>
    public async Task StampAsync(List<VacancyStatusReportRowDto> rows, List<VacancyRow> source, bool useArabicNames)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var orgLookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var departmentByUnit = await _orgUnitProfileRepository.GetAll()
            .Select(p => new { p.OrganizationUnitId, Name = useArabicNames ? p.NameAr : p.OrganizationUnitType.NameEn })
            .ToDictionaryAsync(p => p.OrganizationUnitId, p => p.Name);

        var vacancyIds = rows.Select(r => r.VacancyId).Distinct().ToList();
        var statusCounts = await _applicationStatusRepository.GetAll()
            .Where(r => r.Vacancy != null && r.ApplicantCV != null && vacancyIds.Contains(r.VacancyId.Value))
            .GroupBy(r => new { VacancyId = r.VacancyId.Value, r.AppStatus })
            .Select(g => new { g.Key.VacancyId, g.Key.AppStatus, Count = g.Count() })
            .ToListAsync();

        var sourceByVacancy = source.ToDictionary(s => s.VacancyId);

        foreach (var row in rows)
        {
            var orgUnitId = sourceByVacancy.TryGetValue(row.VacancyId, out var s) ? s.OrganizationUnitId : null;

            // Company: ancestor-or-self at Level1 -- see VacancyStatusReportRowDto.Company remarks.
            row.Company = orgLookup.GetLevelNames(orgUnitId)[0];

            row.Department = orgUnitId.HasValue && departmentByUnit.TryGetValue(orgUnitId.Value, out var dept)
                ? dept
                : string.Empty;

            row.StatusCounts = System.Enum.GetValues(typeof(AppStatus))
                .Cast<AppStatus>()
                .Select(status => new VacancyStatusCountDto
                {
                    Status = status,
                    Count = statusCounts.FirstOrDefault(c => c.VacancyId == row.VacancyId && c.AppStatus == status)?.Count ?? 0
                })
                .ToList();
        }
    }

    public class VacancyRow
    {
        public int VacancyId { get; set; }

        public string Code { get; set; }

        public string VacancyName { get; set; }

        public long? OrganizationUnitId { get; set; }

        public System.DateTime PublishOn { get; set; }

        public System.DateTime ClosedOn { get; set; }
    }
}
