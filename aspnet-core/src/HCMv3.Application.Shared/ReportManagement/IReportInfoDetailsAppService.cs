using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IReportInfoDetailsAppService : IApplicationService
{
    Task<PagedResultDto<ReportInfoDetailDto>> GetAll(GetAllReportInfoDetailsInput input);

    Task<GetReportInfoDetailForEditOutput> GetReportInfoDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportInfoDetailDto input);

    Task Delete(EntityDto input);
}
