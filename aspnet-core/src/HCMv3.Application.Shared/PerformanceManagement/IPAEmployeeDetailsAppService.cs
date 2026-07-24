using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAEmployeeDetailsAppService : IApplicationService
{
    Task<PagedResultDto<PAEmployeeDetailDto>> GetAll(GetAllPAEmployeeDetailsInput input);

    Task<GetPAEmployeeDetailForEditOutput> GetPAEmployeeDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAEmployeeDetailDto input);

    Task Delete(EntityDto input);
}
