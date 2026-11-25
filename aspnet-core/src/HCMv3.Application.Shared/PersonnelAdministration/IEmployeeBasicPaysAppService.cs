using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeBasicPaysAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeBasicPayDto>> GetAll(GetAllEmployeeBasicPaysInput input);

    Task<GetEmployeeBasicPayForEditOutput> GetEmployeeBasicPayForEdit(EntityDto input);

    /// <summary>Saves the basic pay and returns its id.</summary>
    Task<int> CreateOrEdit(CreateOrEditEmployeeBasicPayDto input);

    Task Delete(EntityDto input);
}
