using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface ICVQualificationsAppService : IApplicationService
{
    Task<PagedResultDto<CVQualificationDto>> GetAll(GetAllCVQualificationsInput input);

    Task<GetCVQualificationForEditOutput> GetCVQualificationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCVQualificationDto input);

    Task Delete(EntityDto input);
}
