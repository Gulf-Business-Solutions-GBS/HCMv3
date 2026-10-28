namespace HCMv3.Workflow.Dto;

/// <summary>
/// One choice for a "Person on Request" step's Request Field: a <c>WorkflowRequestPersonFields</c> code, its
/// label, and the transaction types that fill it. A chain used by a type not listed skips the step.
/// </summary>
public class RequestPersonFieldOptionDto
{
    public string Code { get; set; }

    public string DisplayName { get; set; }

    /// <summary>The field alone ("Guarantor"), shown under its transaction type in the grouped dropdown.</summary>
    public string FieldName { get; set; }

    /// <summary>The transaction type the field belongs to, which the dropdown groups by.</summary>
    public string AppliesTo { get; set; }
}
