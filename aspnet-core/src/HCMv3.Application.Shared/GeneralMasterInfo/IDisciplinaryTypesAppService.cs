using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IDisciplinaryTypesAppService : IApplicationService
{
    Task<PagedResultDto<DisciplinaryTypeDto>> GetAll(GetAllDisciplinaryTypesInput input);

    Task<GetDisciplinaryTypeForEditOutput> GetDisciplinaryTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDisciplinaryTypeDto input);

    Task Delete(EntityDto input);

    Task<ListResultDto<ViolationWageTypeDto>> GetViolationWageTypes(NullableIdDto input);
}
