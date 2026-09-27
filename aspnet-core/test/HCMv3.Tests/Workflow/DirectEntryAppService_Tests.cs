using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the lookup the admin transaction forms use to decide whether to offer the "record without
/// approval" choice at all.
///
/// <para>
/// The point of every test here is that the service answers with exactly the types the submitter would
/// accept. A screen offering a choice the save then refuses is worse than a screen not offering it, because
/// the user has already decided by the time they find out.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class DirectEntryAppService_Tests : AppTestBase
{
    private readonly IDirectEntryAppService _directEntryAppService;

    public DirectEntryAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _directEntryAppService = Resolve<IDirectEntryAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>Sets the three flags that decide the answer on a catalog row that already exists.</summary>
    private void ConfigureType(string code, bool active, bool workflowEnabled, bool allowDirectEntry)
    {
        UsingDbContext(context =>
        {
            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == code);
            type.IsActive = active;
            type.IsWorkflowEnabled = workflowEnabled;
            type.AllowDirectEntry = allowDirectEntry;
        });
    }

    [Fact]
    public async Task Should_Return_A_Type_That_Is_Active_Routed_And_Flagged()
    {
        ConfigureType("EmployeeDelegation", active: true, workflowEnabled: true, allowDirectEntry: true);

        var codes = await _directEntryAppService.GetDirectEntryRequestTypes();

        codes.ShouldContain("EmployeeDelegation");
    }

    [Fact]
    public async Task Should_Not_Return_A_Type_That_Is_Not_Flagged()
    {
        ConfigureType("EmployeeDelegation", active: true, workflowEnabled: true, allowDirectEntry: false);

        var codes = await _directEntryAppService.GetDirectEntryRequestTypes();

        codes.ShouldNotContain("EmployeeDelegation");
    }

    /// <summary>
    /// With routing off the transaction is already recorded immediately, so there is nothing to skip.
    /// Offering the choice would suggest a decision the user does not have to make -- and the submitter
    /// ignores the flag entirely in this state, so the screen and the server would disagree.
    /// </summary>
    [Fact]
    public async Task Should_Not_Return_A_Type_Whose_Workflow_Is_Switched_Off()
    {
        ConfigureType("EmployeeDelegation", active: true, workflowEnabled: false, allowDirectEntry: true);

        var codes = await _directEntryAppService.GetDirectEntryRequestTypes();

        codes.ShouldNotContain("EmployeeDelegation");
    }

    [Fact]
    public async Task Should_Not_Return_An_Inactive_Type()
    {
        ConfigureType("EmployeeDelegation", active: false, workflowEnabled: true, allowDirectEntry: true);

        var codes = await _directEntryAppService.GetDirectEntryRequestTypes();

        codes.ShouldNotContain("EmployeeDelegation");
    }

    /// <summary>
    /// The seeder registers every type switched off, which is the state a fresh tenant is in. Nothing may be
    /// bypassed until somebody deliberately allows it, one type at a time.
    /// </summary>
    [Fact]
    public async Task Should_Return_Nothing_On_A_Freshly_Seeded_Catalog()
    {
        var codes = await _directEntryAppService.GetDirectEntryRequestTypes();

        codes.ShouldBeEmpty();
    }
}
