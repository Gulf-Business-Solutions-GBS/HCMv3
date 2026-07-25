using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IApplicantCVsAppService : IApplicationService
{
    Task<PagedResultDto<ApplicantCVDto>> GetAll(GetAllApplicantCVsInput input);

    Task<GetApplicantCVForEditOutput> GetApplicantCVForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditApplicantCVDto input);

    Task Delete(EntityDto input);
}
