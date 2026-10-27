using System.ComponentModel.DataAnnotations;

namespace HCMv3.ReportManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ReportInfoDetail"/>. <c>Id == null</c> means create.
/// </summary>
public class CreateOrEditReportInfoDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int ReportInfoId { get; set; }

    [StringLength(256)]
    public string NodeId { get; set; }

    [StringLength(256)]
    public string EntityName { get; set; }

    [StringLength(256)]
    public string NodeAlias { get; set; }

    [StringLength(256)]
    public string PropertyName { get; set; }

    [StringLength(256)]
    public string PropertyDisplayName { get; set; }

    [StringLength(256)]
    public string PropertyType { get; set; }

    [StringLength(256)]
    public string BaseType { get; set; }

    [StringLength(512)]
    public string AssemblyName { get; set; }

    [StringLength(256)]
    public string ParentEntity { get; set; }

    [StringLength(512)]
    public string ParentAssembly { get; set; }

    public bool IsAssociate { get; set; }

    [StringLength(256)]
    public string AssociateEntity { get; set; }

    [StringLength(256)]
    public string AssociateProperty { get; set; }
}
