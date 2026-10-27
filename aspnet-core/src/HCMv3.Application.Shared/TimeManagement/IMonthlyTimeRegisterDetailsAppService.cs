using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IMonthlyTimeRegisterDetailsAppService : IApplicationService
{
    Task<PagedResultDto<MonthlyTimeRegisterDetailDto>> GetAll(GetAllMonthlyTimeRegisterDetailsInput input);

    Task<GetMonthlyTimeRegisterDetailForEditOutput> GetMonthlyTimeRegisterDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditMonthlyTimeRegisterDetailDto input);

    Task Delete(EntityDto input);
}
