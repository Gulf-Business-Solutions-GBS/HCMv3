using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Asset analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeAsset.ascx</c>).
/// Same two-operation shape as the reference <see cref="IEmployeeContractReportAppService"/>.
/// </summary>
public interface IEmployeeAssetReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAssetReportRowDto>> GetEmployeeAssetReport(GetEmployeeAssetReportInput input);

    Task<FileDto> GetEmployeeAssetReportToExcel(GetEmployeeAssetReportInput input);
}
