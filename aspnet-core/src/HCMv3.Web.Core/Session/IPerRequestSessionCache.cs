using System.Threading.Tasks;
using HCMv3.Sessions.Dto;

namespace HCMv3.Web.Session;

public interface IPerRequestSessionCache
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformationsAsync();
}

