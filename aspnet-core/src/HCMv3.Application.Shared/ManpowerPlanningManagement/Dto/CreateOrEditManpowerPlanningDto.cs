using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ManpowerPlanning"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditManpowerPlanningDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    [Required]
    public int PeriodId { get; set; }

    [StringLength(1000)]
    public string RequestComments { get; set; }

    /// <summary>
    /// The planned lines, optional, CREATE ONLY (ignored on update -- lines are maintained by
    /// <c>IManpowerPlanningDetailsAppService</c> after that).
    ///
    /// <para>
    /// It exists because approval is now the engine's, and the engine submits a plan at the moment it is
    /// created. A handler that refuses an empty plan -- and it must, since approval is what mints the
    /// positions -- would otherwise make a routed plan impossible to raise at all: the header would be
    /// submitted before any line could reference it. Sending the lines with the header closes that window;
    /// the whole aggregate is inserted and submitted in one transaction.
    /// </para>
    ///
    /// <para>
    /// With workflow switched off for this type (the seeded default) this may be left empty and lines added
    /// afterwards exactly as before -- the header is simply recorded.
    /// </para>
    /// </summary>
    public List<CreateOrEditManpowerPlanningDetailDto> Details { get; set; }
}
