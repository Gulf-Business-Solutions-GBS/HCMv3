using System;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using HCMv3.ApiClient;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.MobileClient;

public class TrustedServerCertificates_Tests
{
    private static X509Certificate2 CreateSelfSignedCertificate(string subject)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subject}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    private static string Sha256Of(X509Certificate2 certificate)
    {
        return Convert.ToHexString(SHA256.HashData(certificate.RawData));
    }

    [Fact]
    public void Should_Trust_A_Certificate_The_Phone_Already_Trusts()
    {
        using var certificate = CreateSelfSignedCertificate("trusted.example");

        TrustedServerCertificates.IsTrusted(certificate, SslPolicyErrors.None, Array.Empty<string>()).ShouldBeTrue();
    }

    [Fact]
    public void Should_Trust_An_Untrusted_Certificate_Whose_Fingerprint_Is_Pinned()
    {
        using var certificate = CreateSelfSignedCertificate("vps.example");

        TrustedServerCertificates
            .IsTrusted(certificate, SslPolicyErrors.RemoteCertificateChainErrors, new[] { Sha256Of(certificate) })
            .ShouldBeTrue();
    }

    [Fact]
    public void Should_Match_The_Pinned_Fingerprint_Regardless_Of_Letter_Case()
    {
        using var certificate = CreateSelfSignedCertificate("vps.example");

        TrustedServerCertificates
            .IsTrusted(certificate, SslPolicyErrors.RemoteCertificateChainErrors, new[] { Sha256Of(certificate).ToLowerInvariant() })
            .ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_An_Untrusted_Certificate_That_Is_Not_Pinned()
    {
        using var pinned = CreateSelfSignedCertificate("vps.example");
        using var impostor = CreateSelfSignedCertificate("vps.example");

        TrustedServerCertificates
            .IsTrusted(impostor, SslPolicyErrors.RemoteCertificateChainErrors, new[] { Sha256Of(pinned) })
            .ShouldBeFalse();
    }

    [Fact]
    public void Should_Reject_When_There_Is_No_Certificate()
    {
        TrustedServerCertificates
            .IsTrusted(null, SslPolicyErrors.RemoteCertificateNotAvailable, TrustedServerCertificates.PinnedSha256Fingerprints)
            .ShouldBeFalse();
    }

    [Fact]
    public void Should_Pin_Only_Full_Sha256_Fingerprints()
    {
        TrustedServerCertificates.PinnedSha256Fingerprints.ShouldNotBeEmpty();
        TrustedServerCertificates.PinnedSha256Fingerprints.ShouldAllBe(f => f.Length == 64);
    }
}
