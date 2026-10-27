using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement.Dto;

namespace HCMv3.SuccessionManagement;

public interface IEmpPerformanceHistoriesAppService : IApplicationService
{
    Task<PagedResultDto<EmpPerformanceHistoryDto>> GetAll(GetAllEmpPerformanceHistoriesInput input);

    Task<GetEmpPerformanceHistoryForEditOutput> GetEmpPerformanceHistoryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmpPerformanceHistoryDto input);

    Task Delete(EntityDto input);
}
