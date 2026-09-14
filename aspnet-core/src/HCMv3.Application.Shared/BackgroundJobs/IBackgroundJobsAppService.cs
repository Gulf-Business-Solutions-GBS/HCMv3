using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.BackgroundJobs.Dto;

namespace HCMv3.BackgroundJobs;

/// <summary>
/// The Background Jobs screen: the scheduled tasks and when each last ran, the queued jobs still waiting or stuck, and
/// the recent failures of queued jobs.
///
/// <para>
/// The host sees every company. A company sees its own queued jobs and failures, and when each system-wide task ran,
/// but not those tasks' errors -- a task runs across all companies, so its error can name any of them.
/// </para>
/// </summary>
public interface IBackgroundJobsAppService : IApplicationService
{
    Task<BackgroundJobsOverviewDto> GetOverview();
}
