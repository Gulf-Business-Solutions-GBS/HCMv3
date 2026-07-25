using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICDPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<CDPeriodDto>> GetAll(GetAllCDPeriodsInput input);

    Task<GetCDPeriodForEditOutput> GetCDPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCDPeriodDto input);

    Task Delete(EntityDto input);
}
