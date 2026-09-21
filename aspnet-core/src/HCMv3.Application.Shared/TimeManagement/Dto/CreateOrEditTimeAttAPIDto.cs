using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TimeAttAPI"/>. <c>Id == null</c> means create; otherwise update.
/// <see cref="Password"/> is write-only: the server never fills it in on the way out. Blank on an edit
/// means "keep the saved password"; it is required only when nothing is saved yet (checked in the service).
/// </summary>
public class CreateOrEditTimeAttAPIDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(500)]
    public string TokenUrl { get; set; }

    [Required]
    [StringLength(256)]
    public string Username { get; set; }

    /// <summary>
    /// Capped at 128 so the encrypted value still fits the 256-character column.
    /// </summary>
    [StringLength(128)]
    public string Password { get; set; }

    [Required]
    [StringLength(500)]
    public string TransactionsUrl { get; set; }

    [Required]
    public int NoOfRecords { get; set; }

    [Required]
    [StringLength(500)]
    public string DevicesUrl { get; set; }

    [StringLength(50)]
    public string PunchInCode { get; set; }

    [StringLength(50)]
    public string PunchOutCode { get; set; }

    [StringLength(50)]
    public string BreakOutCode { get; set; }

    [StringLength(50)]
    public string BreakInCode { get; set; }

    [StringLength(256)]
    public string Email { get; set; }

    [StringLength(256)]
    public string Company { get; set; }
}
