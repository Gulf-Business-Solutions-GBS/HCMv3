using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The read-only figures legacy's back-office leave form (XAF detail view of <c>EmployeeAbsence</c>) worked out
/// as the user typed: legacy <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeAbsence.cs</c>
/// <c>CalendarDays</c> (<c>UpdateCalendarDays</c>, 2911-2917), <c>AbsenceHours</c> (<c>UpdateAbsenceHours</c>,
/// 2935-2941), <c>TotalLeaveDays</c> (<c>RecalculateTotalLeaveDays</c>, 2942-2969), <c>CurrentBalance</c> (408-437,
/// with <c>GetOtherLeaveQuota</c> 3158-3258), <c>Paid_CalendarDays</c>/<c>DeductFromQuota</c> (820-866),
/// <c>Unpaid_CalendarDays</c>/<c>UnpaidDays</c> (926-972) and <c>SickLeaveUnpaidInfo</c> (980-1100).
///
/// <para>
/// Nothing here is new arithmetic: the day counts are <see cref="IEmployeeAbsenceCalculationService"/>, the sick
/// slabs are <see cref="ISickLeaveDeductionPreview"/>, the accrual period / pro-rata / salary-profile pieces are
/// <c>IAccrualService</c>. This service only puts them together the way the legacy getters did.
/// </para>
/// </summary>
public interface IEmployeeAbsencePreviewService : IDomainService
{
    Task<EmployeeAbsencePreview> CalculateAsync(EmployeeAbsencePreviewRequest request);
}

/// <summary>What the form currently holds.</summary>
public class EmployeeAbsencePreviewRequest
{
    public int EmployeeId { get; set; }

    public int AbsenceTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public DateTime ReturnDate { get; set; }

    public DateTime? PaidStartDate { get; set; }

    public DateTime? PaidEndDate { get; set; }

    public DateTime? UnpaidStartDate { get; set; }

    public DateTime? UnpaidEndDate { get; set; }
}

/// <summary>The figures, each named after the legacy property it reproduces.</summary>
public class EmployeeAbsencePreview
{
    public double CalendarDays { get; set; }

    public double AbsenceHours { get; set; }

    public double TotalLeaveDays { get; set; }

    public double CurrentBalance { get; set; }

    /// <summary>Legacy <c>Paid_CalendarDays</c>.</summary>
    public double PaidCalendarDays { get; set; }

    public double DeductFromQuota { get; set; }

    /// <summary>Legacy <c>Unpaid_CalendarDays</c>.</summary>
    public double UnpaidCalendarDays { get; set; }

    public double UnpaidDays { get; set; }

    /// <summary>Legacy <c>SickLeaveUnpaidInfo</c>, line for line in legacy's own wording; empty when it does not apply.</summary>
    public string SickLeaveUnpaidInfo { get; set; } = string.Empty;

    /// <summary>The same breakdown as data, for a screen that wants to lay it out itself.</summary>
    public List<SickLeaveDeductionSegment> SickLeaveSegments { get; set; } = new();
}
