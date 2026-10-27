using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IPrintReportsAppService : IApplicationService
{
    Task<PagedResultDto<PrintReportDto>> GetAll(GetAllPrintReportsInput input);

    Task<GetPrintReportForEditOutput> GetPrintReportForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPrintReportDto input);

    Task Delete(EntityDto input);
}
