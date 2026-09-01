using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

/// <summary>
/// Create/edit input for an appraisal.
///
/// <para>
/// The legacy <c>RequestComments</c>/<c>WorkflowComments</c> fields are deliberately absent. Both columns
/// still exist on <c>PAEmployee</c> so appraisals migrated from HRMS keep the text their approvers typed,
/// and they are still returned on <see cref="PAEmployeeDto"/> so that text stays readable -- but no screen
/// may write them again. New approval comments belong to the <c>WorkflowComment</c> thread on the governing
/// request, which keeps one comment per row with an author, a timestamp and replies, instead of
/// concatenating an entire conversation into a single 1000-character string.
/// </para>
/// </summary>
public class CreateOrEditPAEmployeeDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int PACycleId { get; set; }

    [Required]
    public int FormTypeId { get; set; }

    [StringLength(256)]
    public string ProposedIncrement { get; set; }

    [StringLength(256)]
    public string ProposedJobTitle { get; set; }

    public string AreaOfImprovement { get; set; }
}
