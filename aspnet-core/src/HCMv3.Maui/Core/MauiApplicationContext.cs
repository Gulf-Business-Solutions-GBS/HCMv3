using HCMv3.ApiClient;

namespace HCMv3.Maui.Core;

public class MauiApplicationContext : ApplicationContext
{
    private TenantInformation _currentTenant;
    public override TenantInformation CurrentTenant
    {
        get => _currentTenant;
        protected set
        {
            if (value == _currentTenant)
            {
                return;
            }
            _currentTenant = value;
            OnTenantChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public static event EventHandler OnTenantChange;
}