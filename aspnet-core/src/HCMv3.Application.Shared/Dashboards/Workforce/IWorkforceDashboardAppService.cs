using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Workforce.Dto;
using HCMv3.Dto;

namespace HCMv3.Dashboards.Workforce;

/// <summary>
/// Workforce Dashboard: headcount, age, gender, nationality, years of service, qualification, job title,
/// department, and joiners, leavers and turnover over a period. Replaces the legacy ESS Workforce dashboard;
/// turnover is new (legacy's never worked).
///
/// Whose employees it shows comes from <c>IDashboardScopeService</c>, never from the client.
/// </summary>
public interface IWorkforceDashboardAppService : IApplicationService
{
    Task<WorkforceDashboardDto> GetDashboard(WorkforceDashboardInput input);

    Task<PagedResultDto<WorkforceEmployeeRowDto>> GetEmployees(GetWorkforceEmployeesInput input);

    /// <summary>The employees behind one of the movement counts.</summary>
    Task<PagedResultDto<WorkforceMovementRowDto>> GetMovements(GetWorkforceMovementsInput input);

    /// <summary>The documents expired or about to expire, soonest first.</summary>
    Task<PagedResultDto<WorkforceDocumentRowDto>> GetDocuments(GetWorkforceDocumentsInput input);

    Task<FileDto> ExportToExcel(ExportWorkforceDashboardInput input);
}
