using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IApplicantsAppService : IApplicationService
{
    Task<PagedResultDto<ApplicantDto>> GetAll(GetAllApplicantsInput input);

    Task<GetApplicantForEditOutput> GetApplicantForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditApplicantDto input);

    Task Delete(EntityDto input);
}
