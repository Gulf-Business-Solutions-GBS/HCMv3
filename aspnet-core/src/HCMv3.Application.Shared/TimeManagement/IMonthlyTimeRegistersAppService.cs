using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IMonthlyTimeRegistersAppService : IApplicationService
{
    Task<PagedResultDto<MonthlyTimeRegisterDto>> GetAll(GetAllMonthlyTimeRegistersInput input);

    Task<GetMonthlyTimeRegisterForEditOutput> GetMonthlyTimeRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditMonthlyTimeRegisterDto input);

    Task Delete(EntityDto input);
}
