using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeCertificationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeCertificationDto>> GetAll(GetAllEmployeeCertificationsInput input);

    Task<GetEmployeeCertificationForEditOutput> GetEmployeeCertificationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeCertificationDto input);

    Task Delete(EntityDto input);
}
