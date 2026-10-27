using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeBenefitExceptionsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeBenefitExceptionDto>> GetAll(GetAllEmployeeBenefitExceptionsInput input);

    Task<GetEmployeeBenefitExceptionForEditOutput> GetEmployeeBenefitExceptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeBenefitExceptionDto input);

    Task Delete(EntityDto input);
}
