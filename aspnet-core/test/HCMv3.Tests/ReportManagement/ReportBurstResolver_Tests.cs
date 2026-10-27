using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Tests for the burst resolver -- the component that decides WHO receives WHICH slice.
///
/// These concentrate on the rules that, if wrong, send one department's data to another department.
/// A resolver bug is not a cosmetic defect: the output is an e-mail that has already left the building
/// by the time anyone notices.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReportBurstResolver_Tests : AppTestBase
{
    private readonly IReportBurstResolver _resolver;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ReportBurstResolver_Tests()
    {
        _resolver = Resolve<IReportBurstResolver>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    [Fact]
    public async Task Should_Produce_A_Single_Unkeyed_Slice_When_Not_Burst()
    {
        var schedule = NewSchedule(ReportBurstDimension.None);
        schedule.Recipients.Add(ExplicitRecipient("ops@example.com"));

        var slices = await _resolver.ResolveSlicesAsync(schedule);

        slices.Count.ShouldBe(1);
        slices[0].Key.ShouldBeEmpty();
        slices[0].Recipients.Single().Email.ShouldBe("ops@example.com");
    }

    [Fact]
    public async Task Should_Drop_Slices_That_Have_No_Recipient()
    {
        // A schedule with no recipient rules at all resolves to nothing to send. Rendering files nobody
        // receives would burn time and log misleading successes.
        //
        // Wrapped in a unit of work because this is the one case here that actually queries: bursting by
        // org unit reads the org tree, and a repository-backed domain service needs an ambient UoW.
        var schedule = NewSchedule(ReportBurstDimension.OrganizationUnit);

        List<ReportBurstSlice> slices;
        using (var uow = _unitOfWorkManager.Begin())
        {
            slices = await _resolver.ResolveSlicesAsync(schedule);
            await uow.CompleteAsync();
        }

        slices.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Deduplicate_A_Recipient_Matched_By_Two_Rules()
    {
        // Somebody who is both a department head and on the HR distribution list gets ONE copy.
        var schedule = NewSchedule(ReportBurstDimension.None);
        schedule.Recipients.Add(ExplicitRecipient("dual@example.com"));
        schedule.Recipients.Add(ExplicitRecipient("dual@example.com"));

        var slices = await _resolver.ResolveSlicesAsync(schedule);

        slices.Single().Recipients.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Match_Recipient_Email_Case_Insensitively_When_Deduplicating()
    {
        var schedule = NewSchedule(ReportBurstDimension.None);
        schedule.Recipients.Add(ExplicitRecipient("Head@Example.com"));
        schedule.Recipients.Add(ExplicitRecipient("head@example.com"));

        var slices = await _resolver.ResolveSlicesAsync(schedule);

        slices.Single().Recipients.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Keep_Scoping_When_One_Rule_Requires_It_And_Another_Bypasses_It()
    {
        // THE SAFE READING MUST WIN. If the same address is produced by a scoped rule and by an
        // unscoped one, the copy stays scoped -- otherwise adding a broad "send to the auditor" rule
        // would silently widen what an internal recipient receives.
        var schedule = NewSchedule(ReportBurstDimension.None);
        schedule.Recipients.Add(ExplicitRecipient("mixed@example.com", bypassScope: true));
        schedule.Recipients.Add(ExplicitRecipient("mixed@example.com", bypassScope: false));

        var slices = await _resolver.ResolveSlicesAsync(schedule);

        var recipient = slices.Single().Recipients.Single();
        recipient.ApplyRecipientScope.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Honour_Bypass_Only_When_No_Rule_Requires_Scoping()
    {
        // The external-auditor case: a lone explicit rule with bypass set really does go unscoped,
        // otherwise the auditor -- who has no RLS scope at all -- would receive an empty file forever.
        var schedule = NewSchedule(ReportBurstDimension.None);
        schedule.Recipients.Add(ExplicitRecipient("auditor@example.com", bypassScope: true));

        var slices = await _resolver.ResolveSlicesAsync(schedule);

        slices.Single().Recipients.Single().ApplyRecipientScope.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Ignore_An_Explicit_Rule_With_No_Address()
    {
        var schedule = NewSchedule(ReportBurstDimension.None);
        schedule.Recipients.Add(ExplicitRecipient(null));
        schedule.Recipients.Add(ExplicitRecipient("   "));

        var slices = await _resolver.ResolveSlicesAsync(schedule);

        slices.ShouldBeEmpty();
    }

    private static ReportSchedule NewSchedule(ReportBurstDimension dimension)
    {
        return new ReportSchedule
        {
            Name = "Test schedule",
            ReportKind = ScheduledReportKind.GridReport,
            ReportKey = "EmployeeAttendance",
            Format = ScheduledReportFormat.Excel,
            BurstDimension = dimension,
            OccurrenceType = ReportScheduleOccurrenceType.Once,
            Recipients = new List<ReportScheduleRecipient>(),
        };
    }

    private static ReportScheduleRecipient ExplicitRecipient(string email, bool bypassScope = false)
    {
        return new ReportScheduleRecipient
        {
            Rule = ReportRecipientRule.Explicit,
            EmailAddress = email,
            BypassRecipientScope = bypassScope,
        };
    }
}
