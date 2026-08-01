using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Careers.Dto;

namespace HCMv3.Careers;

/// <summary>
/// The AUTHENTICATED candidate surface of the careers portal (wave R3a): apply to a vacancy, maintain a
/// profile and CVs, track application status. Ported from legacy
/// <c>HCM10.Recruiting/Controllers/UserAccountController</c> plus the apply path of
/// <c>VacancyController.QuickApplyForJob</c>.
///
/// THE OWNERSHIP CONTRACT -- every method on this interface obeys it:
/// 1. The applicant row is resolved from <c>Applicant.UserId == AbpSession.UserId</c>. NO METHOD TAKES AN
///    APPLICANT ID.
/// 2. The only client-supplied row ids accepted anywhere are CV ids (<c>UpdateCareersCvInput.Id</c>,
///    <c>CareersApplyInput.ApplicantCvId</c>, <c>GetMyCv</c>/<c>DeleteMyCv</c>) and a vacancy id. Every CV
///    id is re-read under <c>ApplicantId == (caller's applicant)</c> before it is used, and "not yours" is
///    reported identically to "does not exist".
/// 3. CV child rows (qualification/experience/skill/language) have no client-addressable id at all; they
///    are replaced wholesale through their parent CV.
///
/// <c>[AbpAuthorize]</c> on its own would satisfy none of the above: a logged-in candidate IS authorized,
/// just not for another candidate's rows, and the tenant filter is no help because both candidates are in
/// the same tenant.
///
/// There is no login method here -- candidates authenticate through the stock
/// <c>/api/TokenAuth/Authenticate</c>, which understands careers subdomains via
/// <c>CareersTenantResolveContributor</c> (wave R2).
/// </summary>
public interface ICareersCandidateAppService : IApplicationService
{
    Task<CareersProfileDto> GetMyProfile();

    Task<CareersProfileDto> UpdateMyProfile(UpdateCareersProfileInput input);

    Task<ListResultDto<CareersCvListDto>> GetMyCvs();

    Task<CareersCvDetailDto> GetMyCv(EntityDto input);

    Task<CareersCvDetailDto> CreateMyCv(CreateCareersCvInput input);

    Task<CareersCvDetailDto> UpdateMyCv(UpdateCareersCvInput input);

    Task DeleteMyCv(EntityDto input);

    Task<CareersApplyOutput> ApplyToVacancy(CareersApplyInput input);

    Task<ListResultDto<CareersApplicationDto>> GetMyApplications();
}
