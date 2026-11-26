using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeBasicPayDetailsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeBasicPayDetailDto>> GetAll(GetAllEmployeeBasicPayDetailsInput input);

    Task<GetEmployeeBasicPayDetailForEditOutput> GetEmployeeBasicPayDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeBasicPayDetailDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Wage types a line may use, as in the legacy HRMS: active, earning, included in basic pay, and neither
    /// arrear nor calculated pay. A small id/name list, so the line dialog does not load the full wage type master.
    /// </summary>
    Task<List<BasicPayWageTypeLookupDto>> GetSelectableWageTypes(GetSelectableBasicPayWageTypesInput input);
}
