using Xunit;

namespace HCMv3.Tests;

public sealed class MultiTenantTheoryAttribute : TheoryAttribute
{
    private readonly bool _multiTenancyEnabled = HCMv3Consts.MultiTenancyEnabled;

    public MultiTenantTheoryAttribute()
    {
        if (!_multiTenancyEnabled)
        {
            Skip = "MultiTenancy is disabled.";
        }
    }
}