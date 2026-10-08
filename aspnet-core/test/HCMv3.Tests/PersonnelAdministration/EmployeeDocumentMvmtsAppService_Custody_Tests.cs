using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Tests.Ess;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// HR's half of the custody trail: recording that a document was physically handed over, and recording it
/// as taken back at the counter -- each stamped with the user who did it.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDocumentMvmtsAppService_Custody_Tests : PassportWithdrawalTestBase
{
    private readonly IEmployeeDocumentMvmtsAppService _appService;

    public EmployeeDocumentMvmtsAppService_Custody_Tests()
    {
        _appService = Resolve<IEmployeeDocumentMvmtsAppService>();
    }

    /// <summary>An approved movement, out ten days ago and due back in four.</summary>
    private int ApprovedMovement(string code, RequestStatus? status = RequestStatus.Approved, DateTime? actualReturnDate = null)
    {
        var employeeId = CreateEmployee(code);

        return InsertMovement(employeeId, CreatePassportFor(employeeId), status, actualReturnDate: actualReturnDate);
    }

    private string CurrentUserName()
    {
        return UsingDbContext(context => context.Users.Single(u => u.Id == AbpSession.UserId).UserName);
    }

    // ---------------------------------------------------------------- hand-over

    [Fact]
    public async Task Should_Record_The_HandOver_And_Who_Recorded_It()
    {
        var movementId = ApprovedMovement("HO100");
        var handedOver = DateTime.Today.AddDays(-8);

        await _appService.RecordHandOver(new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = handedOver });

        var row = MovementRow(movementId);
        row.ActualWithdrawDate.ShouldBe(handedOver);
        row.HandedOverByUserId.ShouldBe(AbpSession.UserId);

        // The requested date is a different fact and is left alone.
        row.WithdrawDate.ShouldBe(DateTime.Today.AddDays(-10));

        var details = (await _appService.GetEmployeeDocumentMvmtForEdit(new EntityDto(movementId))).Details;
        details.ActualWithdrawDate.ShouldBe(handedOver);
        details.HandedOverByUserName.ShouldBe(CurrentUserName());
        details.ReceivedByUserName.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Record_The_HandOver_On_A_Movement_HR_Entered_Directly()
    {
        // No workflow status at all is in effect, the same as approved.
        var movementId = ApprovedMovement("HO110", status: null);

        await _appService.RecordHandOver(new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = DateTime.Today });

        MovementRow(movementId).ActualWithdrawDate.ShouldBe(DateTime.Today);
    }

    [Fact]
    public async Task Recording_The_HandOver_Again_Should_Correct_The_Date()
    {
        var movementId = ApprovedMovement("HO120");

        await _appService.RecordHandOver(new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = DateTime.Today.AddDays(-8) });
        await _appService.RecordHandOver(new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = DateTime.Today.AddDays(-7) });

        MovementRow(movementId).ActualWithdrawDate.ShouldBe(DateTime.Today.AddDays(-7));
    }

    [Fact]
    public async Task Should_Refuse_A_HandOver_Date_In_The_Future()
    {
        var movementId = ApprovedMovement("HO130");

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordHandOver(
            new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = DateTime.Today.AddDays(1) }));

        MovementRow(movementId).ActualWithdrawDate.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_A_HandOver_With_No_Date()
    {
        var movementId = ApprovedMovement("HO140");

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordHandOver(
            new RecordDocumentHandOverInput { Id = movementId }));
    }

    [Theory]
    [InlineData(RequestStatus.Draft)]
    [InlineData(RequestStatus.Open)]
    [InlineData(RequestStatus.Rejected)]
    public async Task Should_Refuse_A_HandOver_On_A_Movement_That_Is_Not_In_Effect(RequestStatus status)
    {
        // Nobody has agreed to release the document, so there is no hand-over to record.
        var movementId = ApprovedMovement("HO150" + (int)status, status);

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordHandOver(
            new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = DateTime.Today }));

        MovementRow(movementId).ActualWithdrawDate.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_A_HandOver_On_A_Movement_Already_Returned()
    {
        var movementId = ApprovedMovement("HO160", actualReturnDate: DateTime.Today.AddDays(-1));

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordHandOver(
            new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = DateTime.Today.AddDays(-5) }));

        MovementRow(movementId).ActualWithdrawDate.ShouldBeNull();
    }

    // ---------------------------------------------------------------- return at the counter

    [Fact]
    public async Task Should_Record_The_Return_With_Remarks_And_Who_Received_It()
    {
        var movementId = ApprovedMovement("RT100");

        await _appService.RecordReturn(new RecordDocumentReturnInput
        {
            Id = movementId,
            ActualReturnDate = DateTime.Today,
            ReturnRemarks = "  Good condition  "
        });

        var row = MovementRow(movementId);
        row.ActualReturnDate.ShouldBe(DateTime.Today);
        row.ReturnRemarks.ShouldBe("Good condition");
        row.ReceivedByUserId.ShouldBe(AbpSession.UserId);

        var details = (await _appService.GetEmployeeDocumentMvmtForEdit(new EntityDto(movementId))).Details;
        details.ReturnRemarks.ShouldBe("Good condition");
        details.ReceivedByUserName.ShouldBe(CurrentUserName());
        details.IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_A_Return_Before_The_HandOver()
    {
        // Asked for ten days ago, actually handed over five days ago: a return dated seven days ago is after
        // the request and before the hand-over, and it is the hand-over that counts.
        var movementId = ApprovedMovement("RT110");

        await _appService.RecordHandOver(new RecordDocumentHandOverInput { Id = movementId, ActualWithdrawDate = DateTime.Today.AddDays(-5) });

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordReturn(
            new RecordDocumentReturnInput { Id = movementId, ActualReturnDate = DateTime.Today.AddDays(-7) }));

        MovementRow(movementId).ActualReturnDate.ShouldBe(default(DateTime));

        // On the day of the hand-over itself is allowed.
        await _appService.RecordReturn(new RecordDocumentReturnInput { Id = movementId, ActualReturnDate = DateTime.Today.AddDays(-5) });

        MovementRow(movementId).ActualReturnDate.ShouldBe(DateTime.Today.AddDays(-5));
    }

    [Fact]
    public async Task Should_Refuse_A_Return_Before_The_Withdraw_Date_When_No_HandOver_Is_Recorded()
    {
        var movementId = ApprovedMovement("RT120");

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordReturn(
            new RecordDocumentReturnInput { Id = movementId, ActualReturnDate = DateTime.Today.AddDays(-11) }));

        MovementRow(movementId).ActualReturnDate.ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task Should_Refuse_A_Return_Date_In_The_Future()
    {
        var movementId = ApprovedMovement("RT130");

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordReturn(
            new RecordDocumentReturnInput { Id = movementId, ActualReturnDate = DateTime.Today.AddDays(1) }));

        MovementRow(movementId).ActualReturnDate.ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task Should_Refuse_A_Return_With_No_Date()
    {
        var movementId = ApprovedMovement("RT140");

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordReturn(
            new RecordDocumentReturnInput { Id = movementId }));
    }

    [Fact]
    public async Task Should_Refuse_A_Return_On_A_Movement_Already_Returned()
    {
        var firstReturn = DateTime.Today.AddDays(-2);
        var movementId = ApprovedMovement("RT150", actualReturnDate: firstReturn);

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordReturn(
            new RecordDocumentReturnInput { Id = movementId, ActualReturnDate = DateTime.Today }));

        // The first return's date is the one that stands.
        MovementRow(movementId).ActualReturnDate.ShouldBe(firstReturn);
    }

    [Fact]
    public async Task Should_Refuse_A_Return_On_A_Movement_That_Is_Not_In_Effect()
    {
        var movementId = ApprovedMovement("RT160", RequestStatus.Open);

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.RecordReturn(
            new RecordDocumentReturnInput { Id = movementId, ActualReturnDate = DateTime.Today }));

        MovementRow(movementId).ActualReturnDate.ShouldBe(default(DateTime));
    }

    // ---------------------------------------------------------------- the list

    [Fact]
    public async Task The_List_Should_Show_The_Guarantor_And_Whether_The_Document_Is_Overdue()
    {
        var employeeId = CreateEmployee("LS100");
        var guarantorId = CreateEmployee("LS100GUAR");
        var documentId = CreatePassportFor(employeeId);

        var overdueId = InsertMovement(employeeId, documentId, RequestStatus.Approved, outInDays: -20, backInDays: -3, guarantorId: guarantorId);
        var returnedId = InsertMovement(employeeId, documentId, RequestStatus.Approved, outInDays: -40, backInDays: -30,
            actualReturnDate: DateTime.Today.AddDays(-29));
        var pendingId = InsertMovement(employeeId, documentId, RequestStatus.Open, outInDays: -20, backInDays: -3);
        var notDueId = InsertMovement(employeeId, documentId, null, outInDays: -1, backInDays: 0);

        var items = (await _appService.GetAll(new GetAllEmployeeDocumentMvmtsInput { EmployeeDocumentId = documentId, MaxResultCount = 50 })).Items;

        var overdue = items.Single(i => i.Id == overdueId);
        overdue.GuarantorId.ShouldBe(guarantorId);
        overdue.GuarantorNameEn.ShouldBe("LS100GUAR T");
        overdue.GuarantorNameAr.ShouldNotBeNullOrWhiteSpace();
        overdue.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        overdue.IsOverdue.ShouldBeTrue();
        overdue.DaysOverdue.ShouldBe(3);
        overdue.Extensions.ShouldBeEmpty();

        // Back already, never approved, and due today: none of them is overdue.
        foreach (var id in new[] { returnedId, pendingId, notDueId })
        {
            var item = items.Single(i => i.Id == id);
            item.IsOverdue.ShouldBeFalse();
            item.DaysOverdue.ShouldBe(0);
        }
    }
}
