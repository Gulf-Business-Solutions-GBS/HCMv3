namespace HCMv3.Maui.Services.Tenants;

public interface ITenantCustomizationService
{
    Task<string> GetTenantLogo();
}