using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Authorization.Users.Dto;

namespace HCMv3.Authorization.Users;

public interface IUserLoginAppService : IApplicationService
{
    Task<PagedResultDto<UserLoginAttemptDto>> GetUserLoginAttempts(GetLoginAttemptsInput input);
    Task<string> GetExternalLoginProviderNameByUser(long userId);
}

