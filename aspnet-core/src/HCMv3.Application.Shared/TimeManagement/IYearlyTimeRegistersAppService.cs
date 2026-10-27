using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IYearlyTimeRegistersAppService : IApplicationService
{
    Task<PagedResultDto<YearlyTimeRegisterDto>> GetAll(GetAllYearlyTimeRegistersInput input);

    Task<GetYearlyTimeRegisterForEditOutput> GetYearlyTimeRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditYearlyTimeRegisterDto input);

    Task Delete(EntityDto input);
}
