using HCMv3.Net.Emailing;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Net.Emailing;

/// <summary>
/// Covers the rules that decide whether email can be sent, and which settings get named when it
/// cannot.
///
/// These exercise <see cref="EmailSettingsChecker.FindMissingSettings"/> rather than the instance
/// methods on purpose. Those short-circuit to "valid" under a debug build, and the test suite is a
/// debug build, so a test written against them would pass without checking a single rule.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmailSettingsChecker_FindMissingSettings_Tests
{
    private const string ValidFromAddress = "hr@gbskw.com";
    private const string ValidHost = "smtp.gmail.com";
    private const string ValidUserName = "hr@gbskw.com";
    private const string ValidPassword = "app-password";

    [Fact]
    public void Should_Report_Nothing_Missing_When_All_Settings_Are_Supplied()
    {
        var missing = EmailSettingsChecker.FindMissingSettings(
            ValidFromAddress, ValidHost, useDefaultCredentials: false, ValidUserName, ValidPassword);

        missing.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Report_From_Address_When_It_Is_Not_Set(string fromAddress)
    {
        var missing = EmailSettingsChecker.FindMissingSettings(
            fromAddress, ValidHost, useDefaultCredentials: false, ValidUserName, ValidPassword);

        missing.ShouldContain("DefaultFromAddress");
    }

    [Fact]
    public void Should_Report_From_Address_When_It_Is_Still_The_Seeded_Placeholder()
    {
        // The value ASP.NET Zero seeds. It is non-empty, so only an equality check catches it -- and
        // this is the exact case that produced a bare "SMTP settings not provided" with every other
        // field correctly filled in.
        var missing = EmailSettingsChecker.FindMissingSettings(
            EmailSettingsChecker.PlaceholderFromAddress, ValidHost, useDefaultCredentials: false,
            ValidUserName, ValidPassword);

        missing.ShouldContain("DefaultFromAddress");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Report_Host_When_It_Is_Not_Set(string host)
    {
        var missing = EmailSettingsChecker.FindMissingSettings(
            ValidFromAddress, host, useDefaultCredentials: false, ValidUserName, ValidPassword);

        missing.ShouldContain("SmtpHost");
    }

    [Fact]
    public void Should_Report_Host_When_It_Is_Still_The_Seeded_Placeholder()
    {
        var missing = EmailSettingsChecker.FindMissingSettings(
            ValidFromAddress, EmailSettingsChecker.PlaceholderHost, useDefaultCredentials: false,
            ValidUserName, ValidPassword);

        missing.ShouldContain("SmtpHost");
    }

    [Fact]
    public void Should_Report_Credentials_When_Authentication_Is_On_And_They_Are_Blank()
    {
        var missing = EmailSettingsChecker.FindMissingSettings(
            ValidFromAddress, ValidHost, useDefaultCredentials: false, userName: "", password: "");

        missing.ShouldContain("UserName");
        missing.ShouldContain("Password");
    }

    [Fact]
    public void Should_Ignore_Blank_Credentials_When_Default_Credentials_Are_Used()
    {
        // Windows credentials come from the account the host runs as, so there is nothing to type in
        // and blank boxes must not be reported as a problem.
        var missing = EmailSettingsChecker.FindMissingSettings(
            ValidFromAddress, ValidHost, useDefaultCredentials: true, userName: "", password: "");

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Report_Every_Failing_Setting_Rather_Than_Only_The_First()
    {
        // The point of the change: one round trip tells the administrator everything to fix.
        var missing = EmailSettingsChecker.FindMissingSettings(
            EmailSettingsChecker.PlaceholderFromAddress, host: "", useDefaultCredentials: false,
            userName: "", password: "");

        missing.ShouldBe(new[] { "DefaultFromAddress", "SmtpHost", "UserName", "Password" });
    }

    [Fact]
    public void Should_Return_Keys_In_The_Order_They_Appear_On_The_Settings_Screen()
    {
        var missing = EmailSettingsChecker.FindMissingSettings(
            ValidFromAddress, host: "", useDefaultCredentials: false, ValidUserName, password: "");

        missing.ShouldBe(new[] { "SmtpHost", "Password" });
    }
}
