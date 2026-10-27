using System;
using System.Collections.Generic;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Where one disciplinary record lands on its type's escalation ladder: how many offences of the same kind
/// the employee already has inside the repeat window, and therefore which penalty tier applies.
///
/// <para>
/// The whole point of separating this from the record itself is that the answer changes with time. The same
/// offence is a first warning for someone with a clean six months behind them and a suspension for someone
/// on their third in a fortnight, and neither the record nor the type can say which on its own.
/// </para>
/// </summary>
public class DisciplinaryEscalation
{
    public int EmployeeId { get; set; }

    public int DisciplinaryTypeId { get; set; }

    public string DisciplinaryTypeNameEn { get; set; }

    public string DisciplinaryTypeNameAr { get; set; }

    /// <summary>How far back the type looks when counting repeats. Zero means only the date itself.</summary>
    public int RepeatDurationInDays { get; set; }

    /// <summary>Inclusive start of the repeat window: the disciplinary date minus <see cref="RepeatDurationInDays"/>.</summary>
    public DateTime WindowStartDate { get; set; }

    /// <summary>Inclusive end of the repeat window: the disciplinary date itself.</summary>
    public DateTime WindowEndDate { get; set; }

    /// <summary>
    /// Legacy <c>CurrentDeductionCount</c>: prior records of the SAME type for the SAME employee whose
    /// disciplinary date falls inside the window, excluding the record being assessed.
    /// </summary>
    public int PriorDisciplinaryCount { get; set; }

    /// <summary>
    /// Which offence this one is -- <see cref="PriorDisciplinaryCount"/> plus itself. This is the number the
    /// ladder is read with, and it starts at 1, never 0.
    /// </summary>
    public int OffenceNumber => PriorDisciplinaryCount + 1;

    /// <summary>
    /// The resolved penalty: the <c>DisciplinaryTypeDetail</c> tier with the highest
    /// <c>DisciplinaryCount</c> that is still less than or equal to <see cref="OffenceNumber"/>.
    /// <c>null</c> when the type has no tier low enough -- a type whose ladder starts at 2 says nothing
    /// about a first offence, and inventing a penalty for it would be worse than leaving it unset.
    /// </summary>
    public int? DisciplinaryDeductionTypeId { get; set; }

    public string DisciplinaryDeductionTypeNameEn { get; set; }

    public string DisciplinaryDeductionTypeNameAr { get; set; }

    /// <summary>
    /// The employee's earlier records of this type, most recent first, so whoever raises the record can see
    /// the offences that produced the count rather than just its total.
    /// </summary>
    public List<DisciplinaryEscalationHistoryEntry> History { get; set; } = new List<DisciplinaryEscalationHistoryEntry>();
}

/// <summary>One earlier disciplinary record of the same type, as shown next to the resolved penalty.</summary>
public class DisciplinaryEscalationHistoryEntry
{
    public int Id { get; set; }

    public DateTime DisciplinaryDate { get; set; }

    public Severity Severity { get; set; }

    public int? DisciplinaryDeductionTypeId { get; set; }

    public string DisciplinaryDeductionTypeNameEn { get; set; }

    /// <summary>
    /// Whether this one is inside the repeat window and so contributed to
    /// <see cref="DisciplinaryEscalation.PriorDisciplinaryCount"/>. Older offences are still listed, because
    /// "you had three of these last year but they have aged out" is exactly what an approver wants to know.
    /// </summary>
    public bool IsWithinRepeatWindow { get; set; }

    /// <summary>
    /// <c>null</c> for a record that was never routed. A record still awaiting approval shows as
    /// <c>Open</c> and counts -- see <see cref="IDisciplinaryEscalationService"/>.
    /// </summary>
    public RequestStatus? WorkflowStatus { get; set; }
}
