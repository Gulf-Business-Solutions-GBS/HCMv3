using Microsoft.Extensions.Configuration;

namespace HCMv3.Configuration;

public interface IAppConfigurationAccessor
{
    IConfigurationRoot Configuration { get; }
}

