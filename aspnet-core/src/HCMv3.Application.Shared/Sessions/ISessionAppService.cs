using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Sessions.Dto;

namespace HCMv3.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();

    Task<UpdateUserSignInTokenOutput> UpdateUserSignInToken();
}

