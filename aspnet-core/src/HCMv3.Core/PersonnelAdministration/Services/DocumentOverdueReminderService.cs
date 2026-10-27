using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Notifications;
using HCMv3.Notifications;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>One overdue movement and what is to be said about it, to whom.</summary>
public class DocumentOverdueReminder
{
    public int MovementId { get; set; }

    public int TenantId { get; set; }

    public int EmployeeId { get; set; }

    /// <summary>The employee's login. Null when they have none, and so cannot be reached by the bell.</summary>
    public long? EmployeeUserId { get; set; }

    public int? GuarantorId { get; set; }

    public long? GuarantorUserId { get; set; }

    /// <summary>The document as a person would name it: its type, then its number.</summary>
    public string DocumentLabel { get; set; }

    public DateTime DueDate { get; set; }

    public int DaysOverdue { get; set; }

    public string EmployeeMessage { get; set; }

    /// <summary>Null when the movement has no guarantor.</summary>
    public string GuarantorMessage { get; set; }
}

/// <summary>
/// Reminds the holder of a document that is out of company custody past the day it was due back, and the
/// colleague who guaranteed it.
///
/// <para>
/// Kept apart from the timer that runs it. Which movements are overdue, what is said and the once-a-day rule
/// are all decided here, so they can be tested by calling a method with a date instead of waiting for a
/// worker to fire.
/// </para>
/// </summary>
public interface IDocumentOverdueReminderService : IDomainService
{
    /// <summary>
    /// The reminders owed on <paramref name="today"/>: movements in effect, not returned, due back before
    /// today, and not already reminded today. Sends nothing.
    /// </summary>
    Task<List<DocumentOverdueReminder>> GetRemindersDueAsync(DateTime today);

    /// <summary>
    /// Sends the reminders owed on <paramref name="today"/> to the bell and stamps each movement so it is
    /// not reminded again that day. Returns how many movements were reminded.
    /// </summary>
    Task<int> SendOverdueRemindersAsync(DateTime today);
}

/// <inheritdoc cref="IDocumentOverdueReminderService"/>
[UnitOfWork]
public class DocumentOverdueReminderService : HCMv3DomainServiceBase, IDocumentOverdueReminderService
{
    private readonly IRepository<EmployeeDocumentMvmt> _movementRepository;
    private readonly IAppNotifier _appNotifier;

    public DocumentOverdueReminderService(
        IRepository<EmployeeDocumentMvmt> movementRepository,
        IAppNotifier appNotifier)
    {
        _movementRepository = movementRepository;
        _appNotifier = appNotifier;
    }

    public virtual async Task<List<DocumentOverdueReminder>> GetRemindersDueAsync(DateTime today)
    {
        var movements = await OverdueQuery(today).AsNoTracking().ToListAsync();

        return movements.Select(m => BuildReminder(m, today.Date)).ToList();
    }

    public virtual async Task<int> SendOverdueRemindersAsync(DateTime today)
    {
        today = today.Date;

        var movements = await OverdueQuery(today).ToListAsync();
        var reminded = 0;

        foreach (var movement in movements)
        {
            var reminder = BuildReminder(movement, today);

            try
            {
                var sent = false;

                if (reminder.EmployeeUserId.HasValue)
                {
                    await SendAsync(reminder, reminder.EmployeeUserId.Value, reminder.EmployeeMessage);
                    sent = true;
                }

                // A guarantor who is also the holder (it should not happen, but nothing forbids it) is told
                // once, as the holder.
                if (reminder.GuarantorUserId.HasValue && reminder.GuarantorUserId != reminder.EmployeeUserId)
                {
                    await SendAsync(reminder, reminder.GuarantorUserId.Value, reminder.GuarantorMessage);
                    sent = true;
                }

                // Stamped only when somebody was actually told. A holder with no login is tried again on the
                // next run, so linking them during the day is enough for the reminder to reach them.
                if (sent)
                {
                    movement.OverdueReminderSentOn = today;
                    await _movementRepository.UpdateAsync(movement);
                    reminded++;
                }
            }
            catch (Exception exception)
            {
                // One movement's failure must not cost the others their reminder.
                Logger.Error(
                    $"Could not send the overdue reminder for document movement {movement.Id}: {exception.Message}",
                    exception);
            }
        }

        return reminded;
    }

    /// <summary>
    /// In effect, still out, past due, and not reminded today. The <c>ApprovedOnly</c> filter would already
    /// hide a pending or rejected row; the status is spelled out as well so the rule does not depend on which
    /// filters the caller happens to have on.
    /// </summary>
    private IQueryable<EmployeeDocumentMvmt> OverdueQuery(DateTime today)
    {
        today = today.Date;
        var notReturned = default(DateTime);

        return _movementRepository.GetAll()
            .Include(m => m.Employee)
            .Include(m => m.Guarantor)
            .Include(m => m.EmployeeDocument)
            .ThenInclude(d => d.DocumentType)
            .Where(m => m.WorkflowStatus == null
                        || m.WorkflowStatus == RequestStatus.Approved
                        || m.WorkflowStatus == RequestStatus.Closed)
            .Where(m => m.ActualReturnDate == notReturned && m.ReturnDate < today)
            .Where(m => m.OverdueReminderSentOn == null || m.OverdueReminderSentOn < today)
            .OrderBy(m => m.Id);
    }

    private DocumentOverdueReminder BuildReminder(EmployeeDocumentMvmt movement, DateTime today)
    {
        var typeName = movement.EmployeeDocument?.DocumentType?.NameEn;
        var number = movement.EmployeeDocument?.DocumentNum;

        var label = string.Join(" ", new[] { typeName, number }.Where(part => !string.IsNullOrWhiteSpace(part)));
        if (label.Length == 0)
        {
            label = L("Document");
        }

        var dueDate = movement.ReturnDate.Date;
        var daysOverdue = (today - dueDate).Days;
        var due = dueDate.ToString("dd MMM yyyy");

        return new DocumentOverdueReminder
        {
            MovementId = movement.Id,
            TenantId = movement.TenantId,
            EmployeeId = movement.EmployeeId,
            EmployeeUserId = movement.Employee?.UserId,
            GuarantorId = movement.GuarantorId,
            GuarantorUserId = movement.Guarantor?.UserId,
            DocumentLabel = label,
            DueDate = dueDate,
            DaysOverdue = daysOverdue,
            EmployeeMessage = L("DocumentOverdueReminderForEmployee", label, due, daysOverdue),
            GuarantorMessage = movement.GuarantorId.HasValue
                ? L("DocumentOverdueReminderForGuarantor", label, movement.Employee?.FullNameEn ?? string.Empty, due, daysOverdue)
                : null
        };
    }

    private Task SendAsync(DocumentOverdueReminder reminder, long userId, string message)
    {
        // The movement id rides along so the bell has something to navigate on, as the workflow
        // notifications carry their request id.
        var data = new Dictionary<string, object>
        {
            ["employeeDocumentMvmtId"] = reminder.MovementId
        };

        return _appNotifier.SendMessageAsync(
            new UserIdentifier(reminder.TenantId, userId),
            message,
            data,
            NotificationSeverity.Warn);
    }
}
