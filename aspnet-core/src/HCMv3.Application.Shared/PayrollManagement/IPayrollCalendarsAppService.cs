using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IPayrollCalendarsAppService : IApplicationService
{
    Task<PagedResultDto<PayrollCalendarDto>> GetAll(GetAllPayrollCalendarsInput input);

    Task<GetPayrollCalendarForEditOutput> GetPayrollCalendarForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPayrollCalendarDto input);

    Task Delete(EntityDto input);
}
