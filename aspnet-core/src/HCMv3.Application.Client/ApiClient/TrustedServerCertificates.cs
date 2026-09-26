using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HCMv3.ApiClient;

/// <summary>
/// Decides whether the mobile app may talk to a server whose certificate the phone does not trust.
/// A certificate the phone already trusts is always accepted. Otherwise only a certificate whose
/// SHA-256 fingerprint is listed here is accepted - never "any certificate".
/// </summary>
public static class TrustedServerCertificates
{
    /* The Webtropia test server (vps2656276.fastwebserver.de, ports 443 and 9443) uses a self-signed
       certificate valid until 2028-09-01. Once it gets a certificate from a public authority
       (e.g. Let's Encrypt) the phone trusts it on its own and this entry is no longer used. */
    public static readonly string[] PinnedSha256Fingerprints =
    {
        "735A53555BB11670655B81F439F589B692F6287809BE347A41639B29DA9DA887"
    };

    public static bool IsTrusted(X509Certificate certificate, SslPolicyErrors errors)
    {
        return IsTrusted(certificate, errors, PinnedSha256Fingerprints);
    }

    public static bool IsTrusted(X509Certificate certificate, SslPolicyErrors errors, IEnumerable<string> pinnedSha256Fingerprints)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        if (certificate == null)
        {
            return false;
        }

        var fingerprint = Convert.ToHexString(SHA256.HashData(certificate.GetRawCertData()));
        return pinnedSha256Fingerprints.Any(pinned => string.Equals(pinned, fingerprint, StringComparison.OrdinalIgnoreCase));
    }
}
