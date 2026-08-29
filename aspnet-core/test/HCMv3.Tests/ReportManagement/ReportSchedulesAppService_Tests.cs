using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.UI;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Tests for the Report Distribution app service.
///
/// The emphasis is on what the service REFUSES. A schedule that saves in a bad shape does not fail
/// visibly -- it fails weeks later as an empty run log, or as a report delivered to somebody who should
/// not have received it.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReportSchedulesAppService_Tests : AppTestBase
{
    private readonly IReportSchedulesAppService _appService;

    public ReportSchedulesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IReportSchedulesAppService>();
    }

    [Fact]
    public async Task Should_Refuse_A_Schedule_With_No_Recipients()
    {
        // Would render files nobody receives. Better refused now than discovered as a silent empty run.
        var input = NewSchedule();
        input.Recipients.Clear();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Refuse_A_Schedule_With_No_Destinations()
    {
        var input = NewSchedule();
        input.Destinations.Clear();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Refuse_Org_Unit_Bursting_Without_A_Level()
    {
        // Bursting by org unit with no level chosen has no defined meaning -- every unit at every depth
        // would become a slice, mailing overlapping copies up and down the tree.
        var input = NewSchedule();
        input.BurstDimension = ReportBurstDimension.OrganizationUnit;
        input.BurstOrganizationUnitTypeId = null;

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Refuse_Recurring_Without_A_Pattern()
    {
        var input = NewSchedule();
        input.OccurrenceType = ReportScheduleOccurrenceType.Recurring;
        input.RecurringPattern = null;

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Refuse_Scope_Bypass_On_A_Role_Derived_Recipient()
    {
        // THE IMPORTANT ONE. Bypassing scope is for external addresses with no entitlement of their own.
        // Allowing it on a role-derived recipient would quietly hand an internal person every row in the
        // company, which is exactly what this module exists to prevent.
        var input = NewSchedule();
        input.Recipients = new List<ReportScheduleRecipientDto>
        {
            new ReportScheduleRecipientDto
            {
                Rule = ReportRecipientRule.RoleScoped,
                RoleId = 1,
                BypassRecipientScope = true,
            },
        };

        // Refused either way: without the elevated permission it is an authorization failure, with it
        // the rule-shape check still rejects a non-explicit recipient.
        await Assert.ThrowsAnyAsync<Exception>(() => _appService.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Save_And_Read_Back_A_Valid_Schedule()
    {
        var input = NewSchedule();

        await _appService.CreateOrEdit(input);

        var list = await _appService.GetAll(new GetAllReportSchedulesInput { MaxResultCount = 10 });
        list.TotalCount.ShouldBeGreaterThan(0);

        var saved = list.Items[0];
        saved.Name.ShouldBe(input.Name);
        saved.RecipientRuleCount.ShouldBe(1);

        var forEdit = await _appService.GetForEdit(new EntityDto(saved.Id));
        forEdit.ReportSchedule.Destinations.Count.ShouldBe(1);
        forEdit.ReportSchedule.Recipients.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Duplicate_Should_Create_An_Inactive_Copy()
    {
        // A copy must not start mailing people before anybody has adjusted its filters.
        await _appService.CreateOrEdit(NewSchedule());

        var list = await _appService.GetAll(new GetAllReportSchedulesInput { MaxResultCount = 10 });
        var originalId = list.Items[0].Id;

        var copyId = await _appService.Duplicate(new EntityDto(originalId));

        var copy = await _appService.GetForEdit(new EntityDto(copyId));
        copy.ReportSchedule.IsActive.ShouldBeFalse();
        copy.ReportSchedule.Name.ShouldEndWith("(copy)");
    }

    [Theory]
    [InlineData(ReportDestinationMethod.Ftp)]
    [InlineData(ReportDestinationMethod.Offline)]
    public async Task Should_Refuse_A_Destination_No_Channel_Implements(ReportDestinationMethod method)
    {
        // Only EmailReportSliceDeliveryChannel exists. Saved, these would look configured and then fail
        // at delivery time with "no delivery channel registered" -- long after anyone was watching.
        var input = NewSchedule();
        input.Destinations[0].Method = method;

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    [Theory]
    [InlineData(ScheduledReportFormat.Pdf)]
    [InlineData(ScheduledReportFormat.Csv)]
    public async Task Should_Refuse_A_Format_The_Renderer_Does_Not_Produce(ScheduledReportFormat format)
    {
        // The renderer returns each report's own Get*ToExcel bytes, so a schedule saved as PDF or CSV
        // would deliver a spreadsheet under a format it never produced -- silently, and forever.
        var input = NewSchedule();
        input.Format = format;

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    private static CreateOrEditReportScheduleDto NewSchedule()
    {
        return new CreateOrEditReportScheduleDto
        {
            Name = "Monthly attendance by department",
            ReportKind = ScheduledReportKind.GridReport,
            ReportKey = "EmployeeAttendance",
            Format = ScheduledReportFormat.Excel,
            BurstDimension = ReportBurstDimension.None,
            OccurrenceType = ReportScheduleOccurrenceType.Once,
            FirstOccurrence = new DateTime(2026, 1, 1),
            IsActive = true,
            Recipients = new List<ReportScheduleRecipientDto>
            {
                new ReportScheduleRecipientDto
                {
                    Rule = ReportRecipientRule.Explicit,
                    EmailAddress = "hr@example.com",
                },
            },
            Destinations = new List<ReportScheduleDestinationDto>
            {
                new ReportScheduleDestinationDto
                {
                    Method = ReportDestinationMethod.Email,
                    EmailSubjectTemplate = "{ReportName} - {SliceName}",
                },
            },
        };
    }
}
