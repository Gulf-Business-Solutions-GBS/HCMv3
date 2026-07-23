using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDocumentMvmtsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDocumentMvmtDto>> GetAll(GetAllEmployeeDocumentMvmtsInput input);

    Task<GetEmployeeDocumentMvmtForEditOutput> GetEmployeeDocumentMvmtForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDocumentMvmtDto input);

    Task Delete(EntityDto input);
}
