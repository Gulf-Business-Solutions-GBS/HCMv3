using Abp.Domain.Services;

namespace HCMv3.Authorization.Users.Password;

public interface IPasswordExpirationService : IDomainService
{
    void ForcePasswordExpiredUsersToChangeTheirPassword();
}

