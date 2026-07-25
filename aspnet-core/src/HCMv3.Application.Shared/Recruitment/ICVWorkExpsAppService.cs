using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface ICVWorkExpsAppService : IApplicationService
{
    Task<PagedResultDto<CVWorkExpDto>> GetAll(GetAllCVWorkExpsInput input);

    Task<GetCVWorkExpForEditOutput> GetCVWorkExpForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCVWorkExpDto input);

    Task Delete(EntityDto input);
}
