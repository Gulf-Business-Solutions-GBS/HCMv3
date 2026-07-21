using Xunit;

namespace HCMv3.Tests;

public sealed class MultiTenantFactAttribute : FactAttribute
{
    private readonly bool _multiTenancyEnabled = HCMv3Consts.MultiTenancyEnabled;

    public MultiTenantFactAttribute()
    {
        if (!_multiTenancyEnabled)
        {
            Skip = "MultiTenancy is disabled.";
        }
    }
}
