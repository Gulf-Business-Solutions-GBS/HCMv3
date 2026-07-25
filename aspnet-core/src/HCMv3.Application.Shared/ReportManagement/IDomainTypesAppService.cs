using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IDomainTypesAppService : IApplicationService
{
    Task<PagedResultDto<DomainTypeDto>> GetAll(GetAllDomainTypesInput input);

    Task<GetDomainTypeForEditOutput> GetDomainTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDomainTypeDto input);

    Task Delete(EntityDto input);
}
