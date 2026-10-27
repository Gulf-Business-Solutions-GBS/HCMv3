namespace HCMv3.Configuration;

public interface IAppConfigurationWriter
{
    void Write(string key, string value);
}

