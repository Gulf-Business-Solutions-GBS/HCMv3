using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Abp.Localization;
using Abp.Localization.Sources;
using HCMv3.Navigation;
using HCMv3.Releases;
using HCMv3.SetupChecks;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

/// <summary>
/// The rules every release must keep, checked against the REAL release history and check catalogue: releases in
/// order, feature keys unique, every feature pointing at checks that exist, every check claimed by a feature of the
/// release it came with, and English and Arabic text for all of it. These fail the build when someone cuts a
/// release or adds a check and forgets a piece.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SetupCheckCatalog_Tests : AppTestBase
{
    private static readonly CultureInfo English = new("en");
    private static readonly CultureInfo Arabic = new("ar");

    private readonly SetupCheckRunner _runner;
    private readonly ILocalizationSource _texts;

    public SetupCheckCatalog_Tests()
    {
        _runner = Resolve<SetupCheckRunner>();
        _texts = Resolve<ILocalizationManager>().GetSource(HCMv3Consts.LocalizationSourceName);
    }

    // ---------------------------------------------------------------- releases

    [Fact]
    public void Should_Start_The_Release_History_At_3_0_0()
    {
        HCMv3Releases.All[0].Version.ShouldBe("3.0.0");
    }

    [Fact]
    public void Should_List_Releases_Oldest_First_Without_Repeats()
    {
        var numbers = HCMv3Releases.All.Select(r => r.Number).ToList();

        numbers.ShouldBe(numbers.OrderBy(n => n).ToList());
        numbers.Distinct().Count().ShouldBe(numbers.Count);
        HCMv3Releases.All.ShouldAllBe(r => Regex.IsMatch(r.Version, @"^\d+\.\d+\.\d+$"));
    }

    [Fact]
    public void Should_Make_The_Last_Listed_Release_The_Current_One()
    {
        HCMv3Releases.Current.ShouldBe(HCMv3Releases.All[^1]);
        Resolve<IReleaseCatalog>().Releases[^1].ShouldBe(HCMv3Releases.Current);
    }

    [Fact]
    public void Should_Have_English_And_Arabic_Text_For_Every_Release_And_Feature()
    {
        foreach (var release in HCMv3Releases.All)
        {
            ShouldHaveBothLanguages(release.TitleKey);
            ShouldHaveBothLanguages(release.NotesKey);

            foreach (var feature in release.Features)
            {
                ShouldHaveBothLanguages(feature.Module);
                ShouldHaveBothLanguages(feature.TitleKey);
                ShouldHaveBothLanguages(feature.DescriptionKey);
            }
        }
    }

    [Fact]
    public void Should_Give_Every_Feature_A_Unique_Key()
    {
        var keys = HCMv3Releases.All.SelectMany(r => r.Features).Select(f => f.Key).ToList();

        keys.Distinct().Count().ShouldBe(keys.Count);
    }

    // ---------------------------------------------------------------- checks

    [Fact]
    public void Should_List_Every_Setup_Check_In_The_Catalogue()
    {
        var implemented = typeof(ISetupCheck).Assembly.GetTypes()
            .Where(t => typeof(ISetupCheck).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .OrderBy(t => t.Name);

        SetupCheckCatalog.All.OrderBy(t => t.Name).ShouldBe(implemented);
    }

    [Fact]
    public void Should_Point_Every_Feature_At_Checks_That_Exist()
    {
        var codes = _runner.GetAllChecks().Select(c => c.Code).ToHashSet();

        foreach (var feature in HCMv3Releases.All.SelectMany(r => r.Features))
        {
            feature.SetupCheckCodes.ShouldAllBe(code => codes.Contains(code), feature.Key);
        }
    }

    [Fact]
    public void Should_Have_Every_Check_Claimed_By_A_Feature_Of_The_Release_It_Came_With()
    {
        foreach (var check in _runner.GetAllChecks())
        {
            var release = HCMv3Releases.Find(check.IntroducedIn);
            release.ShouldNotBeNull(check.Code);
            release.Features.ShouldContain(f => f.SetupCheckCodes.Contains(check.Code), check.Code);
        }
    }

    [Fact]
    public void Should_Give_Every_Check_A_Unique_Code_Like_WFL_001()
    {
        var codes = _runner.GetAllChecks().Select(c => c.Code).ToList();

        codes.ShouldAllBe(c => Regex.IsMatch(c, @"^[A-Z]{2,5}-\d{3}$"));
        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    public void Should_Point_Every_Check_At_A_Screen_That_Exists()
    {
        foreach (var check in _runner.GetAllChecks())
        {
            ScreenCatalog.All.ShouldContain(s => s.Code == check.FixScreenCode, check.Code);
        }
    }

    [Fact]
    public void Should_Have_English_And_Arabic_Text_For_Every_Check()
    {
        foreach (var check in _runner.GetAllChecks())
        {
            var key = "SetupCheck_" + check.Code.Replace("-", string.Empty);
            ShouldHaveBothLanguages(key + "_Title");
            ShouldHaveBothLanguages(key + "_Why");
        }
    }

    [Fact]
    public void Should_Have_English_And_Arabic_Text_For_Every_Status()
    {
        foreach (var name in Enum.GetNames<SetupCheckSeverity>())
        {
            ShouldHaveBothLanguages("SetupCheckSeverity_" + name);
        }

        foreach (var name in Enum.GetNames<SetupCheckOutcome>())
        {
            ShouldHaveBothLanguages("SetupCheckOutcome_" + name);
        }

        foreach (var name in Enum.GetNames<FeatureSetupStatus>())
        {
            ShouldHaveBothLanguages("FeatureSetupStatus_" + name);
        }
    }

    private void ShouldHaveBothLanguages(string key)
    {
        _texts.GetStringOrNull(key, English, tryDefaults: false).ShouldNotBeNullOrWhiteSpace($"English text for {key}");
        _texts.GetStringOrNull(key, Arabic, tryDefaults: false).ShouldNotBeNullOrWhiteSpace($"Arabic text for {key}");
    }
}
