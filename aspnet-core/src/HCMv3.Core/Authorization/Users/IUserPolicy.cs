using System.Threading.Tasks;
using Abp.Domain.Policies;

namespace HCMv3.Authorization.Users;

public interface IUserPolicy : IPolicy
{
    Task CheckMaxUserCountAsync(int tenantId);
}

