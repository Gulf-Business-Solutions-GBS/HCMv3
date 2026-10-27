using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Overtime analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmpOvertime.ascx</c>).
/// </summary>
public interface IEmpOvertimeReportAppService : IApplicationService
{
    Task<PagedResultDto<EmpOvertimeReportRowDto>> GetEmpOvertimeReport(GetEmpOvertimeReportInput input);

    Task<FileDto> GetEmpOvertimeReportToExcel(GetEmpOvertimeReportInput input);
}
