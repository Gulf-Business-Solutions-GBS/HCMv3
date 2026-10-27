using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAssetsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAssetDto>> GetAll(GetAllEmployeeAssetsInput input);

    Task<GetEmployeeAssetForEditOutput> GetEmployeeAssetForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAssetDto input);

    Task Delete(EntityDto input);
}
