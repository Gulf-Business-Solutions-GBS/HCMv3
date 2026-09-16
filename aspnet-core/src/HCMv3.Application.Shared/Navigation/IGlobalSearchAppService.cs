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

    /// <summary>The current user's recently opened results, newest first; people who no longer exist are left out.</summary>
    Task<GlobalSearchRecentResultDto> GetRecentSearches();

    /// <summary>Records a result the current user opened, moving it to the top if it is already there.</summary>
    Task AddRecentSearch(GlobalSearchRecentItemInput input);

    /// <summary>Removes one result from the current user's recent list.</summary>
    Task RemoveRecentSearch(GlobalSearchRecentItemInput input);

    /// <summary>Empties the current user's recent list.</summary>
    Task ClearRecentSearches();
}
