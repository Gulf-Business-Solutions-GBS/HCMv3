using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// The ten Payroll JV (Custom 042) analysis grid reports -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom042.ascx(.cs)</c>,
/// one interface per legacy <c>cmbType</c> format (<c>BindType()</c>, line 244), per the porting-wave
/// decision to keep every live format its own application service rather than one combo-driven service.
/// </summary>
public interface IPayrollJVCustom042SalaryReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042SalaryReport(GetPayrollJVCustom042SalaryReportInput input);

    Task<FileDto> GetPayrollJVCustom042SalaryReportToExcel(GetPayrollJVCustom042SalaryReportInput input);
}

public interface IPayrollJVCustom042AdvanceSalaryReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042AdvanceSalaryReport(GetPayrollJVCustom042AdvanceSalaryReportInput input);

    Task<FileDto> GetPayrollJVCustom042AdvanceSalaryReportToExcel(GetPayrollJVCustom042AdvanceSalaryReportInput input);
}

public interface IPayrollJVCustom042AnnualLeaveProvisionReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042AnnualLeaveProvisionReport(GetPayrollJVCustom042AnnualLeaveProvisionReportInput input);

    Task<FileDto> GetPayrollJVCustom042AnnualLeaveProvisionReportToExcel(GetPayrollJVCustom042AnnualLeaveProvisionReportInput input);
}

public interface IPayrollJVCustom042PublicHolidayLeaveProvisionReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042PublicHolidayLeaveProvisionReport(GetPayrollJVCustom042PublicHolidayLeaveProvisionReportInput input);

    Task<FileDto> GetPayrollJVCustom042PublicHolidayLeaveProvisionReportToExcel(GetPayrollJVCustom042PublicHolidayLeaveProvisionReportInput input);
}

public interface IPayrollJVCustom042RamadanTimeBackLeaveProvisionReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042RamadanTimeBackLeaveProvisionReport(GetPayrollJVCustom042RamadanTimeBackLeaveProvisionReportInput input);

    Task<FileDto> GetPayrollJVCustom042RamadanTimeBackLeaveProvisionReportToExcel(GetPayrollJVCustom042RamadanTimeBackLeaveProvisionReportInput input);
}

public interface IPayrollJVCustom042LieuDayLeaveProvisionReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042LieuDayLeaveProvisionReport(GetPayrollJVCustom042LieuDayLeaveProvisionReportInput input);

    Task<FileDto> GetPayrollJVCustom042LieuDayLeaveProvisionReportToExcel(GetPayrollJVCustom042LieuDayLeaveProvisionReportInput input);
}

public interface IPayrollJVCustom042IndemnityProvisionReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042IndemnityProvisionReport(GetPayrollJVCustom042IndemnityProvisionReportInput input);

    Task<FileDto> GetPayrollJVCustom042IndemnityProvisionReportToExcel(GetPayrollJVCustom042IndemnityProvisionReportInput input);
}

public interface IPayrollJVCustom042OvertimeReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042OvertimeReport(GetPayrollJVCustom042OvertimeReportInput input);

    Task<FileDto> GetPayrollJVCustom042OvertimeReportToExcel(GetPayrollJVCustom042OvertimeReportInput input);
}

public interface IPayrollJVCustom042MissingAccountsReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042MissingAccountsRowDto>> GetPayrollJVCustom042MissingAccountsReport(GetPayrollJVCustom042MissingAccountsReportInput input);

    Task<FileDto> GetPayrollJVCustom042MissingAccountsReportToExcel(GetPayrollJVCustom042MissingAccountsReportInput input);
}

public interface IPayrollJVCustom042EmployeeWiseBreakDownReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042EmployeeWiseBreakDownReport(GetPayrollJVCustom042EmployeeWiseBreakDownReportInput input);

    Task<FileDto> GetPayrollJVCustom042EmployeeWiseBreakDownReportToExcel(GetPayrollJVCustom042EmployeeWiseBreakDownReportInput input);
}
