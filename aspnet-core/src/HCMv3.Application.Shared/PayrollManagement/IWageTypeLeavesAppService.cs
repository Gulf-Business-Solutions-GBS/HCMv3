using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IWageTypeLeavesAppService : IApplicationService
{
    Task<PagedResultDto<WageTypeLeaveDto>> GetAll(GetAllWageTypeLeavesInput input);

    Task<GetWageTypeLeaveForEditOutput> GetWageTypeLeaveForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWageTypeLeaveDto input);

    Task<int> AddAllLeaveTypes(EntityDto input);

    Task Delete(EntityDto input);
}
