using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Authorization.Session;

[Table("AppUserSessions")]
public class UserSession : FullAuditedEntity<long>, IMayHaveTenant
{
    public const int MaxIpAddressLength = 64;
    public const int MaxUserAgentLength = 512;
    public const int MaxClientFingerprintLength = 128;
    public const int MaxSessionTokenLength = 128;
    public const int MaxDeviceInfoLength = 256;

    public long UserId { get; set; }

    public int? TenantId { get; set; }

    [MaxLength(MaxIpAddressLength)]
    public string IpAddress { get; set; }

    [MaxLength(MaxUserAgentLength)]
    public string UserAgent { get; set; }

    [MaxLength(MaxClientFingerprintLength)]
    public string ClientFingerprint { get; set; }

    [Required]
    [MaxLength(MaxSessionTokenLength)]
    public string SessionToken { get; set; }

    [MaxLength(MaxDeviceInfoLength)]
    public string DeviceInfo { get; set; }

    public DateTime SignInTime { get; set; }

    public DateTime LastActivityTime { get; set; }

    public bool IsActive { get; set; }

    protected UserSession()
    {
    }

    public UserSession(
        long userId,
        int? tenantId,
        string ipAddress,
        string userAgent,
        string deviceInfo = null)
    {
        UserId = userId;
        TenantId = tenantId;
        IpAddress = Truncate(ipAddress, MaxIpAddressLength);
        UserAgent = Truncate(userAgent, MaxUserAgentLength);
        DeviceInfo = Truncate(deviceInfo, MaxDeviceInfoLength);
        SessionToken = Guid.NewGuid().ToString("N");
        ClientFingerprint = ComputeFingerprint(ipAddress, userAgent);
        SignInTime = DateTime.UtcNow;
        LastActivityTime = DateTime.UtcNow;
        IsActive = true;
    }

    public bool MatchesFingerprint(string fingerprint)
    {
        return string.Equals(ClientFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateLastActivity()
    {
        LastActivityTime = DateTime.UtcNow;
    }

    public void Invalidate()
    {
        IsActive = false;
    }

    public static string ComputeFingerprint(string ipAddress, string userAgent)
    {
        var raw = $"{ipAddress ?? ""}|{userAgent ?? ""}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash);
    }

    public static string ComputeFingerprintByPolicy(string ipAddress, string userAgent, SessionFingerprintValidationPolicy policy)
    {
        var raw = policy switch
        {
            SessionFingerprintValidationPolicy.IpOnly => ipAddress ?? "",
            SessionFingerprintValidationPolicy.UserAgentOnly => userAgent ?? "",
            _ => $"{ipAddress ?? ""}|{userAgent ?? ""}" // IpAndUserAgent (default)
        };

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
