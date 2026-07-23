using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDocumentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDocumentDto>> GetAll(GetAllEmployeeDocumentsInput input);

    Task<GetEmployeeDocumentForEditOutput> GetEmployeeDocumentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDocumentDto input);

    Task Delete(EntityDto input);
}
