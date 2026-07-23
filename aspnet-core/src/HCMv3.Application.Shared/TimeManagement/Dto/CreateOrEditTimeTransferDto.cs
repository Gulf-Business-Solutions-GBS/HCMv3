using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TimeTransfer"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTimeTransferDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime TransferDate { get; set; }

    [Required]
    public int TransferTypeId { get; set; }

    public double TransferCount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
