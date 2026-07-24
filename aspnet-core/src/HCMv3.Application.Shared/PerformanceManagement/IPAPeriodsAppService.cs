using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<PAPeriodDto>> GetAll(GetAllPAPeriodsInput input);

    Task<GetPAPeriodForEditOutput> GetPAPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAPeriodDto input);

    Task Delete(EntityDto input);
}
