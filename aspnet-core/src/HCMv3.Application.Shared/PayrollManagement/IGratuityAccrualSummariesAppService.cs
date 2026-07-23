using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IGratuityAccrualSummariesAppService : IApplicationService
{
    Task<PagedResultDto<GratuityAccrualSummaryDto>> GetAll(GetAllGratuityAccrualSummariesInput input);

    Task<GetGratuityAccrualSummaryForEditOutput> GetGratuityAccrualSummaryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGratuityAccrualSummaryDto input);

    Task Delete(EntityDto input);
}
