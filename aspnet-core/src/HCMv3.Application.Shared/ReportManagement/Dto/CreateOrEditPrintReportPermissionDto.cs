using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PrintReportPermission"/>. <c>Id == null</c> means create.
/// </summary>
public class CreateOrEditPrintReportPermissionDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    public int RoleId { get; set; }
}
