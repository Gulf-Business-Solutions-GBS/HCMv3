using Abp.Dependency;

namespace HCMv3;

public class AppFolders : IAppFolders, ISingletonDependency
{
    public string SampleProfileImagesFolder { get; set; }

    public string WebLogsFolder { get; set; }
}

