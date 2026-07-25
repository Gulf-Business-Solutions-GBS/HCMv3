using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IReportMastersAppService : IApplicationService
{
    Task<PagedResultDto<ReportMasterDto>> GetAll(GetAllReportMastersInput input);

    Task<GetReportMasterForEditOutput> GetReportMasterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportMasterDto input);

    Task Delete(EntityDto input);
}
