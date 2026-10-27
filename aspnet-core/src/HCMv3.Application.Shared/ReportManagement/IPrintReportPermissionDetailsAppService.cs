using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IPrintReportPermissionDetailsAppService : IApplicationService
{
    Task<PagedResultDto<PrintReportPermissionDetailDto>> GetAll(GetAllPrintReportPermissionDetailsInput input);

    Task<GetPrintReportPermissionDetailForEditOutput> GetPrintReportPermissionDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPrintReportPermissionDetailDto input);

    Task Delete(EntityDto input);
}
