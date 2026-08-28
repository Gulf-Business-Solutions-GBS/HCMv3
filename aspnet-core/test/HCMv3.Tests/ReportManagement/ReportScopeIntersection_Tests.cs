using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HCMv3.ReportManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Tests for combining a burst slice's employee set with the recipient's entitlement.
///
/// This is the calculation the customer's department-wise example turns on: the slice says which
/// employees the output is ABOUT, the recipient's scope says which employees that person MAY SEE, and
/// the delivered file must be the overlap. Get it wrong in the obvious ways and you either send one
/// department's data to another department's head, or you send nothing to anybody.
///
/// The null-vs-empty distinction is the part that would silently break: null means "this side imposes
/// no restriction", empty means "nothing qualifies". Reached through reflection because the method is
/// a private implementation detail of the executor -- the behaviour is worth pinning even though the
/// seam is not public.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReportScopeIntersection_Tests
{
    private static HashSet<int> Intersect(HashSet<int> slice, HashSet<int> recipient)
    {
        var method = typeof(ReportScheduleExecutor)
            .GetMethod("IntersectScopes", BindingFlags.NonPublic | BindingFlags.Static);

        method.ShouldNotBeNull();

        return (HashSet<int>)method.Invoke(null, new object[] { slice, recipient });
    }

    [Fact]
    public void Unburst_Schedule_Should_Fall_Back_To_The_Recipient_Scope()
    {
        // Slice null == "no bursting", so the recipient's entitlement alone decides.
        var result = Intersect(null, new HashSet<int> { 1, 2 });

        result.ShouldBe(new HashSet<int> { 1, 2 }, ignoreOrder: true);
    }

    [Fact]
    public void Unrestricted_Recipient_Should_Still_Receive_Only_Their_Slice()
    {
        // Recipient scope null == row-level security is off, or the recipient is exempt. A BURST
        // schedule must still burst: otherwise turning RLS off tenant-wide would quietly send every
        // department head the whole company.
        var result = Intersect(new HashSet<int> { 5, 6 }, null);

        result.ShouldBe(new HashSet<int> { 5, 6 }, ignoreOrder: true);
    }

    [Fact]
    public void Should_Return_Only_The_Overlap()
    {
        // The department-wise case: the head sees employees 2 and 3 of that department, and nothing
        // from outside it (4) or outside their entitlement (1).
        var result = Intersect(new HashSet<int> { 1, 2, 3 }, new HashSet<int> { 2, 3, 4 });

        result.ShouldBe(new HashSet<int> { 2, 3 }, ignoreOrder: true);
    }

    [Fact]
    public void Should_Return_Empty_When_The_Recipient_May_See_Nothing_In_This_Slice()
    {
        // A central-team rule matching a department that recipient cannot see. Empty must SURVIVE the
        // combination -- collapsing it to null here would mean "unrestricted" and deliver everything.
        var result = Intersect(new HashSet<int> { 1, 2 }, new HashSet<int> { 8, 9 });

        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_Preserve_An_Empty_Recipient_Scope()
    {
        var result = Intersect(new HashSet<int> { 1, 2 }, new HashSet<int>());

        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_Preserve_An_Empty_Slice()
    {
        // A department with no employees. Nothing to send, and it must not read as "unrestricted".
        var result = Intersect(new HashSet<int>(), new HashSet<int> { 1, 2 });

        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void Both_Null_Should_Stay_Null()
    {
        // Unburst schedule, unrestricted recipient: genuinely no restriction at all.
        Intersect(null, null).ShouldBeNull();
    }

    [Fact]
    public void Should_Be_Order_Independent()
    {
        // The implementation iterates the smaller side for cost reasons; that must not change the
        // answer depending on which side happens to be larger.
        var big = new HashSet<int>(Enumerable.Range(1, 1000));
        var small = new HashSet<int> { 999, 1000, 1001 };

        Intersect(small, big).ShouldBe(new HashSet<int> { 999, 1000 }, ignoreOrder: true);
        Intersect(big, small).ShouldBe(new HashSet<int> { 999, 1000 }, ignoreOrder: true);
    }
}
