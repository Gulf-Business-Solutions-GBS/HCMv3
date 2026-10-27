using System.Security.Claims;
using HCMv3.Authorization.Users;

namespace HCMv3.Authorization.Impersonation;

public class UserAndIdentity
{
    public User User { get; set; }

    public ClaimsIdentity Identity { get; set; }

    public UserAndIdentity(User user, ClaimsIdentity identity)
    {
        User = user;
        Identity = identity;
    }
}

