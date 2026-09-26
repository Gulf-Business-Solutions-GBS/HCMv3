using System.Threading.Tasks;
using Abp.Dependency;
using HCMv3.ApiClient;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Remembers the signed-in user's ESS context so each mobile screen does not ask the server "who am I"
/// again before loading its own data. The answer is tied to the user and tenant it was fetched for, so
/// signing in as someone else never sees the previous user's answer.
/// </summary>
public class EssContextCache : ISingletonDependency
{
    private readonly IEssSessionAppService _essSessionAppService;
    private readonly IApplicationContext _applicationContext;

    private EssContextDto _context;
    private string _contextOwner;

    public EssContextCache(IEssSessionAppService essSessionAppService, IApplicationContext applicationContext)
    {
        _essSessionAppService = essSessionAppService;
        _applicationContext = applicationContext;
    }

    /// <param name="refresh">True to fetch fresh counts (the home screen); false to reuse what is known.</param>
    public async Task<EssContextDto> GetAsync(bool refresh = false)
    {
        var owner = CurrentOwner();
        if (!refresh && _context != null && _contextOwner == owner)
        {
            return _context;
        }

        var context = await _essSessionAppService.GetEssContext();
        _context = context;
        _contextOwner = owner;
        return context;
    }

    public void Clear()
    {
        _context = null;
        _contextOwner = null;
    }

    private string CurrentOwner()
    {
        var loginInfo = _applicationContext.LoginInfo;
        return $"{loginInfo?.Tenant?.Id}:{loginInfo?.User?.Id}";
    }
}
