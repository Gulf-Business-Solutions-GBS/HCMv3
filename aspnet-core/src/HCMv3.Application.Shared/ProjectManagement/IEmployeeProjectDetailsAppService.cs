using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

public interface IEmployeeProjectDetailsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeProjectDetailDto>> GetAll(GetAllEmployeeProjectDetailsInput input);

    Task<GetEmployeeProjectDetailForEditOutput> GetEmployeeProjectDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeProjectDetailDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Computes the on-demand WorkDays figure for a persisted <see cref="EmployeeProjectDetail"/> by
    /// querying <c>TimeManagement.TimeRegister</c> -- see the implementation's XML doc for details/
    /// simplifications made vs. the legacy XAF non-persistent property.
    /// </summary>
    Task<CalculateWorkDaysOutput> CalculateWorkDaysAsync(CalculateWorkDaysInput input);
}
