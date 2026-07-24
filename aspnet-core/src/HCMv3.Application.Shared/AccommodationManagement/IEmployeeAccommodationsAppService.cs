using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IEmployeeAccommodationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAccommodationDto>> GetAll(GetAllEmployeeAccommodationsInput input);

    Task<GetEmployeeAccommodationForEditOutput> GetEmployeeAccommodationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAccommodationDto input);

    Task Delete(EntityDto input);
}
