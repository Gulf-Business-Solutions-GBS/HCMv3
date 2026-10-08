using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.Notifications;
using HCMv3.Notifications;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Tests.Ess;
using HCMv3.Workflow;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The overdue reminder, tested without its timer: which movements are overdue, what is said to whom, and
/// that a movement is reminded at most once a day.
///
/// <para>
/// The service is built by hand around a substitute notifier so the tests can read exactly what was sent.
/// That means no interceptor opens a unit of work for it, so each call is wrapped in one here -- which is
/// also what the worker does around it.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class DocumentOverdueReminderService_Tests : PassportWithdrawalTestBase
{
    private const long HolderUserId = 991001;
    private const long GuarantorUserId = 991002;

    private readonly IAppNotifier _notifier = Substitute.For<IAppNotifier>();
    private readonly List<(long UserId, string Message, IDictionary<string, object> Data)> _sent = new();
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly DocumentOverdueReminderService _service;

    public DocumentOverdueReminderService_Tests()
    {
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        _notifier
            .SendMessageAsync(Arg.Any<UserIdentifier>(), Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<NotificationSeverity>())
            .Returns(call =>
            {
                _sent.Add((call.Arg<UserIdentifier>().UserId, call.Arg<string>(), call.Arg<IDictionary<string, object>>()));
                return Task.CompletedTask;
            });

        _service = new DocumentOverdueReminderService(Resolve<IRepository<EmployeeDocumentMvmt>>(), _notifier)
        {
            UnitOfWorkManager = _unitOfWorkManager,
            LocalizationManager = Resolve<ILocalizationManager>()
        };
    }

    private async Task<List<DocumentOverdueReminder>> DueAsync(DateTime today)
    {
        using (var unitOfWork = _unitOfWorkManager.Begin())
        {
            var due = await _service.GetRemindersDueAsync(today);
            await unitOfWork.CompleteAsync();
            return due;
        }
    }

    private async Task<int> SendAsync(DateTime today)
    {
        using (var unitOfWork = _unitOfWorkManager.Begin())
        {
            var reminded = await _service.SendOverdueRemindersAsync(today);
            await unitOfWork.CompleteAsync();
            return reminded;
        }
    }

    [Fact]
    public async Task Should_Select_Only_Movements_In_Effect_Not_Returned_And_Past_Due()
    {
        var employeeId = CreateEmployee("OD100", HolderUserId);
        var documentId = CreatePassportFor(employeeId);

        var approvedOverdueId = InsertMovement(employeeId, documentId, RequestStatus.Approved, -20, -3);
        var recordedOverdueId = InsertMovement(employeeId, documentId, null, -20, -1);
        var closedOverdueId = InsertMovement(employeeId, documentId, RequestStatus.Closed, -20, -2);

        var dueToday = InsertMovement(employeeId, documentId, RequestStatus.Approved, -20, 0);
        var dueLater = InsertMovement(employeeId, documentId, RequestStatus.Approved, -20, 5);
        var returned = InsertMovement(employeeId, documentId, RequestStatus.Approved, -20, -3, actualReturnDate: DateTime.Today.AddDays(-1));
        var pending = InsertMovement(employeeId, documentId, RequestStatus.Open, -20, -3);
        var rejected = InsertMovement(employeeId, documentId, RequestStatus.Rejected, -20, -3);
        var draft = InsertMovement(employeeId, documentId, RequestStatus.Draft, -20, -3);

        var due = await DueAsync(DateTime.Today);

        due.Select(r => r.MovementId).OrderBy(id => id)
            .ShouldBe(new[] { approvedOverdueId, recordedOverdueId, closedOverdueId }.OrderBy(id => id));

        var ids = due.Select(r => r.MovementId).ToList();
        foreach (var id in new[] { dueToday, dueLater, returned, pending, rejected, draft })
        {
            ids.ShouldNotContain(id);
        }

        due.Single(r => r.MovementId == approvedOverdueId).DaysOverdue.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Name_The_Document_And_The_Due_Date_To_The_Holder_And_The_Guarantor()
    {
        var employeeId = CreateEmployee("OD200", HolderUserId);
        var guarantorId = CreateEmployee("OD200GUAR", GuarantorUserId);
        var documentId = CreateDocument(employeeId, CreateDocumentType("OD200T", "Passport"), "P-778899");

        var movementId = InsertMovement(employeeId, documentId, RequestStatus.Approved, -20, -3, guarantorId: guarantorId);
        var dueDate = DateTime.Today.AddDays(-3).ToString("dd MMM yyyy");

        var reminded = await SendAsync(DateTime.Today);

        reminded.ShouldBe(1);
        _sent.Count.ShouldBe(2);

        var toHolder = _sent.Single(s => s.UserId == HolderUserId);
        toHolder.Message.ShouldContain("Passport P-778899");
        toHolder.Message.ShouldContain(dueDate);
        toHolder.Data["employeeDocumentMvmtId"].ShouldBe(movementId);

        var toGuarantor = _sent.Single(s => s.UserId == GuarantorUserId);
        toGuarantor.Message.ShouldContain("Passport P-778899");
        toGuarantor.Message.ShouldContain(dueDate);
        toGuarantor.Message.ShouldContain("OD200 T");

        MovementRow(movementId).OverdueReminderSentOn.ShouldBe(DateTime.Today);
    }

    [Fact]
    public async Task Should_Send_Only_To_The_Holder_When_There_Is_No_Guarantor()
    {
        var employeeId = CreateEmployee("OD210", HolderUserId);
        InsertMovement(employeeId, CreatePassportFor(employeeId), RequestStatus.Approved, -20, -3);

        await SendAsync(DateTime.Today);

        _sent.ShouldHaveSingleItem().UserId.ShouldBe(HolderUserId);
    }

    [Fact]
    public async Task Should_Remind_A_Movement_At_Most_Once_A_Day()
    {
        var employeeId = CreateEmployee("OD300", HolderUserId);
        var movementId = InsertMovement(employeeId, CreatePassportFor(employeeId), RequestStatus.Approved, -20, -3);

        (await SendAsync(DateTime.Today)).ShouldBe(1);

        // The worker polls every few hours: the later runs of the same day find nothing owed.
        (await DueAsync(DateTime.Today)).ShouldBeEmpty();
        (await SendAsync(DateTime.Today)).ShouldBe(0);
        (await SendAsync(DateTime.Today)).ShouldBe(0);

        _sent.Count.ShouldBe(1);

        // The next day it is owed again, and the stamp moves on.
        var tomorrow = DateTime.Today.AddDays(1);

        (await DueAsync(tomorrow)).Select(r => r.MovementId).ShouldBe(new[] { movementId });
        (await SendAsync(tomorrow)).ShouldBe(1);

        _sent.Count.ShouldBe(2);
        MovementRow(movementId).OverdueReminderSentOn.ShouldBe(tomorrow);
    }

    [Fact]
    public async Task Should_Not_Stamp_A_Movement_When_Nobody_Could_Be_Told()
    {
        // A holder with no login cannot be reached by the bell. Leaving the stamp empty means the next run
        // tries again, so linking them during the day is enough.
        var employeeId = CreateEmployee("OD400");
        var movementId = InsertMovement(employeeId, CreatePassportFor(employeeId), RequestStatus.Approved, -20, -3);

        (await SendAsync(DateTime.Today)).ShouldBe(0);

        _sent.ShouldBeEmpty();
        MovementRow(movementId).OverdueReminderSentOn.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Stop_Reminding_Once_The_Document_Is_Back()
    {
        var employeeId = CreateEmployee("OD500", HolderUserId);
        var movementId = InsertMovement(employeeId, CreatePassportFor(employeeId), RequestStatus.Approved, -20, -3);

        await SendAsync(DateTime.Today);

        await Resolve<IEmployeeDocumentMvmtsAppService>().RecordReturn(new HCMv3.PersonnelAdministration.Dto.RecordDocumentReturnInput
        {
            Id = movementId,
            ActualReturnDate = DateTime.Today
        });

        (await SendAsync(DateTime.Today.AddDays(1))).ShouldBe(0);

        _sent.Count.ShouldBe(1);
    }

    [Fact]
    public void The_Registered_Service_Should_Resolve_For_The_Worker()
    {
        // The daily worker takes the service from the container; a registration that failed would only show
        // up as a logged error every four hours.
        Resolve<IDocumentOverdueReminderService>().ShouldNotBeNull();
        Resolve<HCMv3.Notifications.DailyNotificationWorker>().ShouldNotBeNull();
    }
}
