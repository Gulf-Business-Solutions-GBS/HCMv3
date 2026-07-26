using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditRetroReasonLogDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime ChangedOn { get; set; }

    [Required]
    public DateTime RetroDate { get; set; }

    [StringLength(256)]
    public string EntityObjectName { get; set; }

    public int EntityObjectId { get; set; }

    [StringLength(4000)]
    public string RetroReason { get; set; }
}
