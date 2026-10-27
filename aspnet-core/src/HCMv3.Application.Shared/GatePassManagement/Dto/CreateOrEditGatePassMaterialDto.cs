using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GatePassManagement.Dto;

public class CreateOrEditGatePassMaterialDto
{
    public int? Id { get; set; }

    [Required]
    public int GatePassId { get; set; }

    [Required]
    [StringLength(128)]
    public string ItemCode { get; set; }

    [Required]
    [StringLength(256)]
    public string ItemName { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public DateTime ExpectedReturnDate { get; set; }

    public int ReturnedQuantity { get; set; }
}
