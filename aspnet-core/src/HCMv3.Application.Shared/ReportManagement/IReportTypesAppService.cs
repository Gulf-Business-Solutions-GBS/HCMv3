using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IReportTypesAppService : IApplicationService
{
    Task<PagedResultDto<ReportTypeDto>> GetAll(GetAllReportTypesInput input);

    Task<GetReportTypeForEditOutput> GetReportTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportTypeDto input);

    Task Delete(EntityDto input);
}
