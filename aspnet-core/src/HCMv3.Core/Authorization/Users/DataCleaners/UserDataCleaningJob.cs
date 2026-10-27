using System.Threading.Tasks;
using Abp;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;

namespace HCMv3.Authorization.Users.DataCleaners;

public class UserDataCleaningJob : AsyncBackgroundJob<UserIdentifier>, ITransientDependency
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public UserDataCleaningJob(IUnitOfWorkManager unitOfWorkManager)
    {
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override async Task ExecuteAsync(UserIdentifier args)
    {
        await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            using (var scope = IocManager.Instance.CreateScope())
            {
                var providers = scope.ResolveAll<IUserDataCleaner>();

                foreach (var provider in providers)
                {
                    await provider.CleanUserData(args);
                }
            }
        });
    }
}

