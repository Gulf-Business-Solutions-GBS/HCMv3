using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.Logging.Dto;

namespace HCMv3.Logging;

public interface IWebLogAppService : IApplicationService
{
    GetLatestWebLogsOutput GetLatestWebLogs();

    FileDto DownloadWebLogs();
}

