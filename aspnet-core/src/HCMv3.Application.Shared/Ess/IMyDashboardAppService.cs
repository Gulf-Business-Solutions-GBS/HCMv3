using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>The signed-in person's dashboard summary, in one call.</summary>
public interface IMyDashboardAppService : IApplicationService
{
    Task<MyDashboardSummaryDto> GetSummary();
}
