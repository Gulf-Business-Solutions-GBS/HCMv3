using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IDisciplinaryDeductionTypesAppService : IApplicationService
{
    Task<PagedResultDto<DisciplinaryDeductionTypeDto>> GetAll(GetAllDisciplinaryDeductionTypesInput input);

    Task<GetDisciplinaryDeductionTypeForEditOutput> GetDisciplinaryDeductionTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDisciplinaryDeductionTypeDto input);

    Task Delete(EntityDto input);
}
