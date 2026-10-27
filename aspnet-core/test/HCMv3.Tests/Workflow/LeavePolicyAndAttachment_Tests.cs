using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The company leave policy (legacy <c>ValidationService.ValidateLeavePolicy</c>) and the required-document rule
/// (legacy "Please Attach Document."), both of which the first pass of this port left out.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeavePolicyAndAttachment_Tests : EssLeaveWorkflowTestBase
{
    private int _meId;

    private int GivenAnEmployee(string code)
    {
        _meId = CreateCurrentUserEmployee(code);
        EnableLeaveWorkflowWithApprover(code + "APP");
        return _meId;
    }

    private static DateTime Soon => DateTime.Today.AddDays(30);

    private Task<ApplyForLeaveResultDto> ApplyAsync(int typeId, DateTime start, int days = 3, string comments = null) =>
        _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            Comments = comments
        });

    // ------------------------------------------------------------------------------- advance-notice policy

    [Fact]
    public async Task A_Leave_Breaking_The_Advance_Policy_Should_Be_Refused_When_The_Type_Says_Error()
    {
        GivenAnEmployee("POL100");
        var typeId = CreateAbsenceType(t =>
        {
            t.ApplyLeaveInAdvance = true;
            t.ApplyLeaveInAdvanceDays = 30;
            t.PolicyRestrictType = RestrictType.Error;
        });
        CreateQuota(_meId, typeId, accrued: 30);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            ApplyAsync(typeId, DateTime.Today.AddDays(5), comments: "Family matter"));

        exception.Message.ShouldContain("Company policy");
    }

    [Fact]
    public async Task A_Warning_Type_Should_Let_The_Leave_Through_Once_A_Comment_Explains_It()
    {
        // Legacy's Warning has exactly one consequence: the comment becomes mandatory.
        GivenAnEmployee("POL110");
        var typeId = CreateAbsenceType(t =>
        {
            t.ApplyLeaveInAdvance = true;
            t.ApplyLeaveInAdvanceDays = 30;
            t.PolicyRestrictType = RestrictType.Warning;
        });
        CreateQuota(_meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, DateTime.Today.AddDays(5)));

        var result = await ApplyAsync(typeId, DateTime.Today.AddDays(5), comments: "Family emergency");
        result.EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task A_Leave_Inside_The_Advance_Policy_Should_Need_No_Explanation()
    {
        GivenAnEmployee("POL120");
        var typeId = CreateAbsenceType(t =>
        {
            t.ApplyLeaveInAdvance = true;
            t.ApplyLeaveInAdvanceDays = 10;
            t.PolicyRestrictType = RestrictType.Error;
        });
        CreateQuota(_meId, typeId, accrued: 30);

        (await ApplyAsync(typeId, Soon)).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task The_Policy_Should_Be_Silent_When_The_Type_Does_Not_Ask_For_Advance_Notice()
    {
        GivenAnEmployee("POL130");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);

        (await ApplyAsync(typeId, DateTime.Today.AddDays(1))).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    // ------------------------------------------------------------------------------- required document

    [Fact]
    public async Task A_Leave_Type_That_Requires_A_Document_Should_Be_Refused_Without_One()
    {
        GivenAnEmployee("ATT100");
        var typeId = CreateAbsenceType(t => t.IsAttachmentRequired = true, nameEn: "Sick");
        CreateQuota(_meId, typeId, accrued: 30);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon));

        exception.Message.ShouldContain("document");

        // The leave never reaches an approver: it is still a Draft nobody can act on. (In production the whole
        // call rolls back; the harness runs non-transactional, so the row survives here -- see
        // EssLeavePayroll_Tests for the transactional version of that check.)
        LeaveRowsOf(_meId).Single().WorkflowStatus.ShouldBe(HCMv3.Workflow.RequestStatus.Draft);
    }

    [Fact]
    public async Task A_Leave_With_The_Document_Attached_Should_Go_Through()
    {
        GivenAnEmployee("ATT110");
        var typeId = CreateAbsenceType(t => t.IsAttachmentRequired = true, nameEn: "Sick");
        CreateQuota(_meId, typeId, accrued: 30);

        // The employee's upload, as the apply form sends it: already in the attachment store against the leave
        // by the time the rules run. Here it is attached directly, since the upload endpoint is an HTTP concern.
        var applied = await ApplyWithAttachmentAsync(typeId, Soon);

        applied.EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task A_Type_That_Does_Not_Require_A_Document_Should_Not_Ask_For_One()
    {
        GivenAnEmployee("ATT120");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);

        (await ApplyAsync(typeId, Soon)).EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Applies for leave whose type requires a document, attaching one the way the ESS does: the leave is created
    /// first, the file is stored against it, and only then is it submitted.
    /// </summary>
    private async Task<ApplyForLeaveResultDto> ApplyWithAttachmentAsync(int typeId, DateTime start)
    {
        var attachments = Resolve<IAttachmentManager>();

        // Switch the requirement off for the moment the leave is created, attach the file, then switch it back
        // on and submit -- the sequence the real upload endpoint produces.
        SetAttachmentRequired(typeId, false);

        var applied = await ApplyAsync(typeId, start);

        // A repository call needs a unit of work of its own here; the app services open their own.
        using (var unitOfWork = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>().Begin())
        {
            await attachments.AddAsync(
                LeaveRuleValidator.LeaveAttachmentEntityType,
                applied.EmployeeAbsenceId.ToString(),
                "sick-note.pdf",
                new byte[] { 1, 2, 3 });

            await unitOfWork.CompleteAsync();
        }

        SetAttachmentRequired(typeId, true);

        return applied;
    }

    private void SetAttachmentRequired(int typeId, bool required) =>
        UsingDbContext(context => context.Set<AbsenceType>().Single(t => t.Id == typeId).IsAttachmentRequired = required);
}
