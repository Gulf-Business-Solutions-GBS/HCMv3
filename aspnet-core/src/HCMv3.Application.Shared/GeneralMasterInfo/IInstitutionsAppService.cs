using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IInstitutionsAppService : IApplicationService
{
    Task<PagedResultDto<InstitutionDto>> GetAll(GetAllInstitutionsInput input);

    Task<GetInstitutionForEditOutput> GetInstitutionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditInstitutionDto input);

    Task Delete(EntityDto input);
}
