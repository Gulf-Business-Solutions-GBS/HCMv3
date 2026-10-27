using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeSignaturesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSignatureDto>> GetAll(GetAllEmployeeSignaturesInput input);

    Task<GetEmployeeSignatureForEditOutput> GetEmployeeSignatureForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSignatureDto input);

    Task Delete(EntityDto input);
}
