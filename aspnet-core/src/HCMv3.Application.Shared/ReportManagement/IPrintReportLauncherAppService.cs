using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

/// <summary>
/// Read-only feed for the "Print Reports" launcher screen (legacy HCM10's Print Reports list).
/// Separate from <see cref="IPrintReportsAppService"/>, which is the ADMIN CRUD screen for the
/// catalog itself: running a report and maintaining the catalog are different permissions.
/// </summary>
public interface IPrintReportLauncherAppService : IApplicationService
{
    /// <summary>
    /// Active catalog rows the current user is allowed to run, ordered by code then English name.
    /// </summary>
    Task<List<RunnableReportDto>> GetRunnableReports();
}
