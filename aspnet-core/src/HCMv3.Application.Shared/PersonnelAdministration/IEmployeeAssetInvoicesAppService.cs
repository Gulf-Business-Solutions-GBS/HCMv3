using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAssetInvoicesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAssetInvoiceDto>> GetAll(GetAllEmployeeAssetInvoicesInput input);

    Task<GetEmployeeAssetInvoiceForEditOutput> GetEmployeeAssetInvoiceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAssetInvoiceDto input);

    Task Delete(EntityDto input);
}
