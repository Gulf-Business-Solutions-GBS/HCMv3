using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Overtime By Day analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmpOvertimeByDay.ascx</c>).
/// </summary>
public interface IEmpOvertimeByDayReportAppService : IApplicationService
{
    Task<PagedResultDto<EmpOvertimeByDayReportRowDto>> GetEmpOvertimeByDayReport(GetEmpOvertimeByDayReportInput input);

    Task<FileDto> GetEmpOvertimeByDayReportToExcel(GetEmpOvertimeByDayReportInput input);
}
