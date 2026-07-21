namespace HCMv3.Authorization.Session
{
    public enum SessionFingerprintValidationPolicy
    {
        IpAndUserAgent = 0,

        UserAgentOnly = 1,
        
        IpOnly = 2
    }
}
