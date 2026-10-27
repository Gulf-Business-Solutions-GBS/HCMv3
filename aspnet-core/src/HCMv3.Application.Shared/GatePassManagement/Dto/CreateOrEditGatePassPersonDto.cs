using System.ComponentModel.DataAnnotations;

namespace HCMv3.GatePassManagement.Dto;

public class CreateOrEditGatePassPersonDto
{
    public int? Id { get; set; }

    [Required]
    public int GatePassId { get; set; }

    [Required]
    public int PersonTypeId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(256)]
    public string PersonFullName { get; set; }

    [StringLength(64)]
    public string PersonCivildID { get; set; }

    [StringLength(64)]
    public string PersonPassportNumber { get; set; }

    [Required]
    public int GatePassVendorId { get; set; }
}
