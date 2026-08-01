using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Careers.Dto;

namespace HCMv3.Careers;

/// <summary>
/// The PUBLIC (anonymous) read surface of the careers portal. Ported from legacy
/// <c>HCM10.Recruiting.Controllers.VacancyController</c>'s read actions (<c>Index</c>, <c>GetVacancy</c>,
/// <c>FilterVacancy</c>, <c>VacancyDetails</c>).
///
/// Every method resolves its tenant from the request subdomain via <see cref="ICareersTenantResolver"/> and
/// fails closed if it cannot -- there is no authenticated session to fall back on. Candidate-side write
/// operations (register, upload CV, apply) are wave R2 and are deliberately absent here.
/// </summary>
public interface ICareersAppService : IApplicationService
{
    Task<PagedResultDto<PublicVacancyListDto>> GetPublishedVacancies(GetPublishedVacanciesInput input);

    Task<PublicVacancyDetailDto> GetPublishedVacancy(EntityDto input);

    Task<PublicVacancyFiltersDto> GetVacancyFilters();

    Task<PublicTenantBrandingDto> GetTenantBranding();
}
