namespace HCMv3.Maui.Models.Common;

public class CurrentLoginInformationPersistanceModel
{
    public UserLoginInfoPersistanceModel User { get; set; }

    public TenantLoginInfoPersistanceModel Tenant { get; set; }

    public ApplicationInfoPersistanceModel Application { get; set; }
}