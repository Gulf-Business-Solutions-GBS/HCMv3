using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// The self-service shell's entry point: resolves the signed-in user to an employee and reports what that
/// employee may do.
/// </summary>
public interface IEssSessionAppService : IApplicationService
{
    Task<EssContextDto> GetEssContext();
}
