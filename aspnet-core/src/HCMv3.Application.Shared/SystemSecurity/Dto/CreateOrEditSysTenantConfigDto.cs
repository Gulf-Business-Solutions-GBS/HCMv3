using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditSysTenantConfigDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Key { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(1000)]
    public string KeyValue { get; set; }

    [Required]
    public TenantConfigCategory ConfigCategory { get; set; }

    [StringLength(1000)]
    public string ConfigDescription { get; set; }
}
