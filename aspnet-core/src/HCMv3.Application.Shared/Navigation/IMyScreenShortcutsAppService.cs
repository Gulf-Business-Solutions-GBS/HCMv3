using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Navigation.Dto;

namespace HCMv3.Navigation;

/// <summary>
/// The signed-in user's own screen favourites and recent history.
///
/// "My" is not decoration: every method reads and writes the CALLER's rows, taken from the session and
/// never from the input. A user id parameter here would be an authorization hole -- one person could
/// read another's navigation, which leaks which screens they use and therefore what they work on.
/// </summary>
public interface IMyScreenShortcutsAppService : IApplicationService
{
    Task<MyScreenShortcutsDto> GetMyShortcuts();

    Task SetFavorite(SetScreenFavoriteInput input);

    Task RecordVisit(RecordScreenVisitInput input);
}
