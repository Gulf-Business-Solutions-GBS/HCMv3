using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TimeAttAPI"/>. <c>Id == null</c> means create; otherwise update.
/// KNOWN CONCERN: <see cref="Password"/> travels in plain text over this DTO, same as the legacy object --
/// see <see cref="TimeAttAPI"/> remarks.
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

    [Required]
    [StringLength(256)]
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
