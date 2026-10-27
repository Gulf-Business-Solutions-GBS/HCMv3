using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ITempBenefitTypesAppService : IApplicationService
{
    Task<PagedResultDto<TempBenefitTypeDto>> GetAll(GetAllTempBenefitTypesInput input);

    Task<GetTempBenefitTypeForEditOutput> GetTempBenefitTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTempBenefitTypeDto input);

    Task Delete(EntityDto input);
}
