using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// News analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscNews.ascx</c>).
/// </summary>
public interface INewsReportAppService : IApplicationService
{
    Task<PagedResultDto<NewsReportRowDto>> GetNewsReport(GetNewsReportInput input);

    Task<FileDto> GetNewsReportToExcel(GetNewsReportInput input);
}
