using System.Linq;
using HCMv3.SetupChecks;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

/// <summary>Reading the change log the build writes (tools/release-log/build-release-log.ps1).</summary>
// ReSharper disable once InconsistentNaming
public class ReleaseLog_Tests
{
    private static readonly ReleaseLog Log = new(TenantVersionCheckTestBase.LogJson);

    [Fact]
    public void Should_Read_The_Version_And_Build_Number()
    {
        Log.Version.ShouldBe("3.0.5");
        Log.Number.ShouldBe(5);
        Log.BuiltOn.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Read_Every_Change_Newest_First_With_Its_Setup_Notes()
    {
        Log.Changes.Select(c => c.Number).ShouldBe(new[] { 5, 4, 3, 2, 1 });
        var newest = Log.Changes[0];
        newest.Subject.ShouldBe("Add geo punch");
        newest.Module.ShouldBe("Ess");
        newest.Setup.Select(s => s.Kind).ShouldBe(new[] { SetupItemKind.Permission, SetupItemKind.Screen, SetupItemKind.Database });
        newest.Setup[0].Text.ShouldBe("Pages.Administration.TenantVersionCheck");
    }

    [Fact]
    public void Should_Turn_Build_Numbers_Into_Versions_And_Back()
    {
        Log.VersionOf(498).ShouldBe("3.0.498");
        Log.NumberOf("3.0.498").ShouldBe(498);
        Log.NumberOf("3.0.0").ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3.1.498")]
    [InlineData("3.0.x")]
    [InlineData("15.2.0")]
    public void Should_Not_Take_Another_Products_Version_For_A_Build(string version)
    {
        Log.NumberOf(version).ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{ \"baseVersion\": \"3.0\" }")]
    public void Should_Report_The_Base_Version_And_No_Changes_When_There_Is_No_Log(string json)
    {
        var empty = new ReleaseLog(json);

        empty.Version.ShouldBe("3.0.0");
        empty.Changes.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Carry_A_Log_In_The_Real_Build()
    {
        // The Core project's build generates and embeds it; a clone without git history would leave it empty.
        ReleaseLog.Embedded.Number.ShouldBeGreaterThan(0);
        ReleaseLog.Embedded.Changes.Count.ShouldBe(ReleaseLog.Embedded.Changes.Select(c => c.Hash).Distinct().Count());
        ReleaseLog.Embedded.Changes.Max(c => c.Number).ShouldBe(ReleaseLog.Embedded.Number);
    }
}

