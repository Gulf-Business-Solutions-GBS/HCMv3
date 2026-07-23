using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IWageTypesAppService : IApplicationService
{
    Task<PagedResultDto<WageTypeDto>> GetAll(GetAllWageTypesInput input);

    Task<GetWageTypeForEditOutput> GetWageTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWageTypeDto input);

    Task Delete(EntityDto input);
}
