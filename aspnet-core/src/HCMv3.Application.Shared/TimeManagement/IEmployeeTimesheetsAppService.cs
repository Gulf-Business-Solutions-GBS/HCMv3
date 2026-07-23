using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IEmployeeTimesheetsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTimesheetDto>> GetAll(GetAllEmployeeTimesheetsInput input);

    Task<GetEmployeeTimesheetForEditOutput> GetEmployeeTimesheetForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTimesheetDto input);

    Task Delete(EntityDto input);
}
