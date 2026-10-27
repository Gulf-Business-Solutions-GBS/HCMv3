using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IReportDetailsAppService : IApplicationService
{
    Task<PagedResultDto<ReportDetailDto>> GetAll(GetAllReportDetailsInput input);

    Task<GetReportDetailForEditOutput> GetReportDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportDetailDto input);

    Task Delete(EntityDto input);
}
