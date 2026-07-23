using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AXBatch"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditAXBatchDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(128)]
    public string JournalBatchNumber { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Required]
    [StringLength(1024)]
    public string Description { get; set; }
}
