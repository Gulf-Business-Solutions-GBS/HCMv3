namespace HCMv3.Chat;

public interface IChatFeatureChecker
{
    void CheckChatFeatures(int? sourceTenantId, int? targetTenantId);
}

