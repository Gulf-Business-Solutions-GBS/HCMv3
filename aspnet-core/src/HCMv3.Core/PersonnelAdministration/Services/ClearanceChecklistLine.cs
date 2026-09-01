namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// One line of a clearance checklist as a particular approver sees it: the question their department owns,
/// plus the answer already recorded against it, if any.
/// </summary>
public class ClearanceChecklistLine
{
    public int ClearanceRequestFieldId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>The role that owns this line -- the reason it is on this approver's screen at all.</summary>
    public int WorkflowRoleId { get; set; }

    /// <summary>Null until somebody answers it.</summary>
    public string Value { get; set; }

    /// <summary>Who answered it, when it has been answered.</summary>
    public int? AnsweredByEmployeeId { get; set; }
}

/// <summary>An answer being saved against one checklist line.</summary>
public class ClearanceChecklistAnswer
{
    public int ClearanceRequestFieldId { get; set; }

    public string Value { get; set; }
}
