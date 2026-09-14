using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using HCMv3.BackgroundJobs;

namespace HCMv3.Authorization.Users.Password;

public class PasswordExpirationBackgroundWorker : RecordedPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private const int CheckPeriodAsMilliseconds = 1 * 60 * 60 * 1000 * 24; //1 day

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IPasswordExpirationService _passwordExpirationService;

    public PasswordExpirationBackgroundWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IPasswordExpirationService passwordExpirationService)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _passwordExpirationService = passwordExpirationService;

        Timer.Period = CheckPeriodAsMilliseconds;
        Timer.RunOnStart = true;

        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    protected override void DoRecordedWork()
    {
        _unitOfWorkManager.WithUnitOfWork(() =>
        {
            _passwordExpirationService.ForcePasswordExpiredUsersToChangeTheirPassword();
        });
    }
}

