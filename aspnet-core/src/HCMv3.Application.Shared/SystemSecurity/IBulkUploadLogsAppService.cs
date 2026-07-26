using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IBulkUploadLogsAppService : IApplicationService
{
    Task<PagedResultDto<BulkUploadLogDto>> GetAll(GetAllBulkUploadLogsInput input);

    Task<GetBulkUploadLogForEditOutput> GetBulkUploadLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditBulkUploadLogDto input);

    Task Delete(EntityDto input);
}
