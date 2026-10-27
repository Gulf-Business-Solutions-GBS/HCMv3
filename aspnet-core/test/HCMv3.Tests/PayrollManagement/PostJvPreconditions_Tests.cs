using System;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.UI;
using HCMv3.Integrations.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The server-side gate before a JV goes to D365, and the preview fingerprint it relies on. The balance test is
/// legacy's, pinned exactly: both totals rounded to WHOLE units with <c>Math.Round</c> (banker's rounding), then
/// compared (<c>uscCostingFile.ascx.cs</c> lines 1626-1630).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PostJvPreconditions_Tests : FinancePostingTestBase
{
    private static string Fingerprint(JournalVoucherResult result) => JournalVoucherFingerprint.Compute(Input(), result);

    private static string Refusal(JournalVoucherResult result) =>
        PostJvPreconditions.FindRefusal(Input(), result, Fingerprint(result));

    // ---------------------------------------------------------------- Legacy whole-unit balance test

    [Fact]
    public void Should_Accept_A_Balanced_Jv()
    {
        Refusal(Voucher(Line("E1", 100.125, 0), Line("", 0, 100.125))).ShouldBeNull();
    }

    /// <summary>
    /// LEGACY BEHAVIOUR, PINNED (looks like a defect for KWD, kept on purpose): 0.400 KWD -- 400 fils -- of
    /// difference passes, because both sides round to 100.
    /// </summary>
    [Fact]
    public void Should_Accept_A_Difference_Of_Up_To_Half_A_Dinar_Because_Legacy_Rounds_To_Whole_Units()
    {
        Refusal(Voucher(Line("E1", 100.400, 0), Line("", 0, 100.000))).ShouldBeNull();
    }

    /// <summary>LEGACY BEHAVIOUR, PINNED: a two-fils difference that straddles .5 is refused (100 vs 101).</summary>
    [Fact]
    public void Should_Refuse_A_Two_Fils_Difference_That_Straddles_Half_A_Dinar()
    {
        Refusal(Voucher(Line("E1", 100.499, 0), Line("", 0, 100.501))).ShouldBe("DebitCreditNotMatched");
    }

    /// <summary>LEGACY BEHAVIOUR, PINNED: Math.Round is banker's rounding, so 100.5 rounds to 100 but 101.5 to 102.</summary>
    [Theory]
    [InlineData(100.5, 100.0, true)]
    [InlineData(101.5, 101.0, false)]
    [InlineData(100.6, 100.4, false)]
    public void Should_Round_Half_To_Even_As_Legacy_Math_Round_Did(double debit, double credit, bool accepted)
    {
        var refusal = Refusal(Voucher(Line("E1", debit, 0), Line("", 0, credit)));

        if (accepted)
        {
            refusal.ShouldBeNull();
        }
        else
        {
            refusal.ShouldBe("DebitCreditNotMatched");
        }
    }

    // ---------------------------------------------------------------- Other refusals

    [Fact]
    public void Should_Refuse_An_Empty_Jv()
    {
        Refusal(Voucher()).ShouldBe("NoDataFoundToPostJV");
    }

    [Fact]
    public void Should_Refuse_Without_A_Preview()
    {
        var voucher = Voucher(Line("E1", 10, 0), Line("", 0, 10));

        PostJvPreconditions.FindRefusal(Input(), voucher, null).ShouldBe("JournalVoucherPreviewRequired");
        PostJvPreconditions.FindRefusal(Input(), voucher, "  ").ShouldBe("JournalVoucherPreviewRequired");
    }

    [Fact]
    public void Should_Refuse_A_Jv_That_Is_Not_The_Previewed_One()
    {
        var previewed = Voucher(Line("E1", 10, 0), Line("", 0, 10));
        var current = Voucher(Line("E1", 11, 0), Line("", 0, 11));

        PostJvPreconditions.FindRefusal(Input(), current, Fingerprint(previewed)).ShouldBe("JournalVoucherPreviewChanged");
    }

    // ---------------------------------------------------------------- Fingerprint

    [Fact]
    public void Should_Give_The_Same_Fingerprint_For_The_Same_Jv_And_Inputs()
    {
        Fingerprint(Voucher(Line("E1", 10, 0))).ShouldBe(Fingerprint(Voucher(Line("E1", 10, 0))));
    }

    [Fact]
    public void Should_Change_The_Fingerprint_When_An_Amount_Changes()
    {
        Fingerprint(Voucher(Line("E1", 10, 0))).ShouldNotBe(Fingerprint(Voucher(Line("E1", 10.001, 0))));
    }

    [Fact]
    public void Should_Change_The_Fingerprint_When_The_Inputs_Change()
    {
        var voucher = Voucher(Line("E1", 10, 0));
        var otherDate = Input();
        otherDate.PostingDate = otherDate.PostingDate.AddDays(1);
        var otherArea = Input();
        otherArea.PayrollAreaName = "Other";
        var otherCodes = Input(employeeCode: "E1");

        var original = JournalVoucherFingerprint.Compute(Input(), voucher);
        JournalVoucherFingerprint.Compute(otherDate, voucher).ShouldNotBe(original);
        JournalVoucherFingerprint.Compute(otherArea, voucher).ShouldNotBe(original);
        JournalVoucherFingerprint.Compute(otherCodes, voucher).ShouldNotBe(original);
    }

    [Fact]
    public void Should_Match_Fingerprints_Ignoring_Case_And_Spaces_But_Never_A_Blank_One()
    {
        JournalVoucherFingerprint.Matches(" ABC ", "abc").ShouldBeTrue();
        JournalVoucherFingerprint.Matches("", "").ShouldBeFalse();
        JournalVoucherFingerprint.Matches(null, "abc").ShouldBeFalse();
    }

    // ---------------------------------------------------------------- Area and switch

    private PostJvPreconditions Create(bool switchOn)
    {
        var postingSwitch = Substitute.For<ID365FinancePostingSwitch>();
        postingSwitch.IsPostingAllowedAsync().Returns(switchOn);

        return new PostJvPreconditions(Repo<PayrollArea>(), Substitute.For<IJournalVoucherBuilder>(), postingSwitch)
        {
            LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>()
        };
    }

    [Fact]
    public async Task Should_Refuse_When_Dynamics_365_Is_Switched_Off()
    {
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => Create(false).EnsurePostingSwitchedOnAsync());
        ex.Message.ShouldBe(Text("D365FinancePostingSwitchedOff"));
    }

    [Fact]
    public async Task Should_Refuse_An_Area_Without_Its_Fi_Links()
    {
        SeedArea(withFiLinks: false);

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => Create(true).RequirePostableAreaAsync(AreaName));
        ex.Message.ShouldBe(Text("PayrollFICompanyNull"));
    }

    [Fact]
    public async Task Should_Return_An_Area_With_Both_Fi_Links()
    {
        SeedArea();

        (await Create(true).RequirePostableAreaAsync(AreaName)).Id.ShouldBe(PayrollAreaId);
    }
}
