using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Install.Dto;

namespace HCMv3.Install;

public interface IInstallAppService : IApplicationService
{
    Task Setup(InstallDto input);

    AppSettingsJsonDto GetAppSettingsJson();

    CheckDatabaseOutput CheckDatabase();
}
