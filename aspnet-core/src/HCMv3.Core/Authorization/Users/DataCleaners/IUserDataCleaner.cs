using Abp;
using System.Threading.Tasks;

namespace HCMv3.Authorization.Users.DataCleaners;

public interface IUserDataCleaner
{
    Task CleanUserData(UserIdentifier userIdentifier);
}

