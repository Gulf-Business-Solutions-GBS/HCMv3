using System.Linq;
using Abp.Configuration.Startup;
using HCMv3.EntityHistory;
using HCMv3.Recruitment;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Auditing;

/// <summary>
/// The application page's Status History tab reads ABP entity history, so ApplicationStatus must be one of the
/// types ABP records changes for. Organization units and roles (the ASP.NET Zero defaults) must stay tracked.
/// </summary>
// ReSharper disable once InconsistentNaming
public class HcmEntityHistoryTypes_Tests : AppTestBase
{
    [Fact]
    public void Should_Track_Application_Status_Through_The_Hook()
    {
        EntityHistoryHelper.TrackedTypes.ShouldContain(typeof(ApplicationStatus));
    }

    [Fact]
    public void Should_Keep_The_Zero_Default_Tracked_Types()
    {
        EntityHistoryHelper.TrackedTypes.ShouldContain(typeof(Abp.Organizations.OrganizationUnit));
        EntityHistoryHelper.TrackedTypes.ShouldContain(typeof(HCMv3.Authorization.Roles.Role));
    }

    [Fact]
    public void Should_Have_An_Entity_History_Selector_That_Matches_Application_Status()
    {
        var configuration = Resolve<IAbpStartupConfiguration>();

        configuration.EntityHistory.Selectors.Any(s => s.Predicate(typeof(ApplicationStatus))).ShouldBeTrue();
    }
}
