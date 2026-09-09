using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Navigation.Dto;

namespace HCMv3.Navigation;

/// <summary>
/// Backs the "search for actions or people" bar in the header.
///
/// Only the people half lives on the server. The actions half is the menu the client already holds, so
/// searching it needs no round trip -- see <c>menu-search-bar.component.ts</c>.
/// </summary>
public interface IGlobalSearchAppService : IApplicationService
{
    Task<GlobalSearchPeopleResultDto> SearchPeople(SearchPeopleInput input);
}
