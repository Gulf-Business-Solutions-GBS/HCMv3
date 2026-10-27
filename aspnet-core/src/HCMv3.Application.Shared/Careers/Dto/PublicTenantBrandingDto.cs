namespace HCMv3.Careers.Dto;

/// <summary>
/// Just enough tenant identity for wave R3 to theme the careers page.
///
/// Logo bytes are intentionally NOT returned here and the underlying <c>BinaryObject</c> Guids are NOT
/// exposed. <c>HCMv3.Web.Controllers.TenantCustomizationController.GetLogo</c> is already
/// <c>[AllowAnonymous]</c> and keyed by tenant id, so the portal fetches
/// <c>/TenantCustomization/GetLogo?tenantId={TenantId}</c> (light) or
/// <c>/TenantCustomization/GetTenantLogo/{skin}/{tenantId}</c>. Nothing subscription-, edition-,
/// payment- or connection-string-related from <c>HCMv3.MultiTenancy.Tenant</c> is exposed.
/// </summary>
public class PublicTenantBrandingDto
{
    public int TenantId { get; set; }

    public string TenancyName { get; set; }

    /// <summary>Tenant display name (<c>Tenant.Name</c>).</summary>
    public string Name { get; set; }

    public bool HasLightLogo { get; set; }

    public bool HasDarkLogo { get; set; }
}
