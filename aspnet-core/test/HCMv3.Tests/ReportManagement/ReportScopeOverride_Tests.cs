using System.Collections.Generic;
using HCMv3.ReportManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Tests for the ambient report-scope override.
///
/// This type exists to stop one specific, silent data leak: grid reports resolve row-level security
/// from AbpSession, a scheduled run has no session, and an empty RLS list is this codebase's
/// convention for "no restriction". Left alone, every scheduled report would contain the entire
/// tenant and look completely normal doing it.
///
/// The three states below are therefore genuinely different and must never collapse into each other:
///     not active      -> fall back to the session (interactive behaviour, unchanged)
///     active + null   -> unrestricted on purpose
///     active + empty  -> entitled to NOTHING
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReportScopeOverride_Tests : AppTestBase
{
    private readonly IReportScopeOverride _override;

    public ReportScopeOverride_Tests()
    {
        _override = Resolve<IReportScopeOverride>();
    }

    [Fact]
    public void Should_Be_Inactive_By_Default()
    {
        // Interactive requests must be completely unaffected.
        _override.IsActive.ShouldBeFalse();
        _override.VisibleEmployeeIds.ShouldBeNull();
    }

    [Fact]
    public void Should_Expose_The_Scope_While_Active()
    {
        using (_override.Begin(new HashSet<int> { 1, 2, 3 }))
        {
            _override.IsActive.ShouldBeTrue();
            _override.VisibleEmployeeIds.ShouldBe(new HashSet<int> { 1, 2, 3 }, ignoreOrder: true);
        }
    }

    [Fact]
    public void Should_Distinguish_Entitled_To_Nothing_From_Unrestricted()
    {
        // THE WHOLE POINT. An empty set means "this recipient may see nothing" and must stay
        // distinguishable from null, which means "no restriction at all". If these two ever collapse,
        // a recipient entitled to nothing receives everything.
        using (_override.Begin(new HashSet<int>()))
        {
            _override.IsActive.ShouldBeTrue();
            _override.VisibleEmployeeIds.ShouldNotBeNull();
            _override.VisibleEmployeeIds.Count.ShouldBe(0);
        }

        using (_override.Begin(null))
        {
            _override.IsActive.ShouldBeTrue();
            _override.VisibleEmployeeIds.ShouldBeNull();
        }
    }

    [Fact]
    public void Should_Restore_The_Previous_State_On_Dispose()
    {
        // A stale override left in place would silently mis-scope whatever ran next on this execution
        // context -- including an ordinary interactive request.
        using (_override.Begin(new HashSet<int> { 7 }))
        {
            _override.IsActive.ShouldBeTrue();
        }

        _override.IsActive.ShouldBeFalse();
        _override.VisibleEmployeeIds.ShouldBeNull();
    }

    [Fact]
    public void Should_Restore_The_Outer_Scope_When_Nested()
    {
        using (_override.Begin(new HashSet<int> { 1 }))
        {
            using (_override.Begin(new HashSet<int> { 2 }))
            {
                _override.VisibleEmployeeIds.ShouldContain(2);
            }

            // Restoring rather than clearing is what keeps the outer slice correctly scoped.
            _override.VisibleEmployeeIds.ShouldContain(1);
        }

        _override.IsActive.ShouldBeFalse();
    }
}
