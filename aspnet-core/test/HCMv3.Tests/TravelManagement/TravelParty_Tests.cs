using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

/// <summary>
/// The employee-or-applicant rule shared by trips, claims and the approval handler. Legacy refused only
/// "both blank"; HCMv3 also refuses both being set.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TravelParty_Tests
{
    [Theory]
    [InlineData(5, null)]
    [InlineData(null, 7)]
    public void Should_Accept_Exactly_One_Party(int? employeeId, int? applicantId)
    {
        TravelParty.HasExactlyOne(employeeId, applicantId).ShouldBeTrue();
    }

    [Fact]
    public void Should_Refuse_Neither_Party()
    {
        TravelParty.HasExactlyOne(null, null).ShouldBeFalse();
    }

    [Fact]
    public void Should_Refuse_Both_Parties()
    {
        TravelParty.HasExactlyOne(5, 7).ShouldBeFalse();
    }

    [Fact]
    public void Should_Treat_A_Zero_Id_As_Not_Chosen()
    {
        // A cleared drop-down can post 0; it must not count as a party.
        TravelParty.HasExactlyOne(0, null).ShouldBeFalse();
        TravelParty.HasExactlyOne(0, 7).ShouldBeTrue();
        TravelParty.Normalize(0).ShouldBeNull();
        TravelParty.Normalize(5).ShouldBe(5);
    }
}
