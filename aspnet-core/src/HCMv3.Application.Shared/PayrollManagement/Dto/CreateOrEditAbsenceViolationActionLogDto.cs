using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsenceViolationActionLog"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditAbsenceViolationActionLogDto
{
    public int? Id { get; set; }

    [Required]
    public int AbsenceViolationActionRecordId { get; set; }

    public int Step { get; set; }

    public AbsenceViolationAction Action { get; set; }

    public DateTime ActionDate { get; set; }

    public int ObjectOId { get; set; }
}
