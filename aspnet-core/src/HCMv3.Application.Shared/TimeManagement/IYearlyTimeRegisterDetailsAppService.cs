using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IYearlyTimeRegisterDetailsAppService : IApplicationService
{
    Task<PagedResultDto<YearlyTimeRegisterDetailDto>> GetAll(GetAllYearlyTimeRegisterDetailsInput input);

    Task<GetYearlyTimeRegisterDetailForEditOutput> GetYearlyTimeRegisterDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditYearlyTimeRegisterDetailDto input);

    Task Delete(EntityDto input);
}
