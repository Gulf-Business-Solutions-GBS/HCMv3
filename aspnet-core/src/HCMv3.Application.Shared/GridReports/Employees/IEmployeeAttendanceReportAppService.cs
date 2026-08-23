using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Attendance analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeAttendance.ascx</c>).
/// Same two-operation shape as the reference <see cref="IEmployeeContractReportAppService"/>.
/// </summary>
public interface IEmployeeAttendanceReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAttendanceReportRowDto>> GetEmployeeAttendanceReport(
        GetEmployeeAttendanceReportInput input);

    Task<FileDto> GetEmployeeAttendanceReportToExcel(GetEmployeeAttendanceReportInput input);
}
