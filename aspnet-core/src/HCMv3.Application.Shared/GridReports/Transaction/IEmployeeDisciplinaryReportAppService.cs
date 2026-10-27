using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Disciplinary analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeDisciplinary.ascx</c>).
/// </summary>
public interface IEmployeeDisciplinaryReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDisciplinaryReportRowDto>> GetEmployeeDisciplinaryReport(GetEmployeeDisciplinaryReportInput input);

    Task<FileDto> GetEmployeeDisciplinaryReportToExcel(GetEmployeeDisciplinaryReportInput input);
}
