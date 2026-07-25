using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IPrintReportPermissionsAppService : IApplicationService
{
    Task<PagedResultDto<PrintReportPermissionDto>> GetAll(GetAllPrintReportPermissionsInput input);

    Task<GetPrintReportPermissionForEditOutput> GetPrintReportPermissionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPrintReportPermissionDto input);

    Task Delete(EntityDto input);
}
