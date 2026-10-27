using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditSysObjectDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(500)]
    public string FullName { get; set; }

    public bool IsPayrollSensitive { get; set; }

    public bool RespectPermission { get; set; }

    public bool MustRetroRun { get; set; }

    public bool IsTimeSensitive { get; set; }
}
