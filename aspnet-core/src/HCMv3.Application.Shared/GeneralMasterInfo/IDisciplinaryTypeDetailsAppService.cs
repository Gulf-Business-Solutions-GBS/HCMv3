using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IDisciplinaryTypeDetailsAppService : IApplicationService
{
    Task<PagedResultDto<DisciplinaryTypeDetailDto>> GetAll(GetAllDisciplinaryTypeDetailsInput input);

    Task<GetDisciplinaryTypeDetailForEditOutput> GetDisciplinaryTypeDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDisciplinaryTypeDetailDto input);

    Task Delete(EntityDto input);
}
