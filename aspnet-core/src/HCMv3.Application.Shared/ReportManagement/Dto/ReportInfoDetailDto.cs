using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="ReportInfoDetail"/>.</summary>
public class ReportInfoDetailDto : EntityDto<int>
{
    public int ReportInfoId { get; set; }

    public string NodeId { get; set; }

    public string EntityName { get; set; }

    public string NodeAlias { get; set; }

    public string PropertyName { get; set; }

    public string PropertyDisplayName { get; set; }

    public string PropertyType { get; set; }

    public string BaseType { get; set; }

    public string AssemblyName { get; set; }

    public string ParentEntity { get; set; }

    public string ParentAssembly { get; set; }

    public bool IsAssociate { get; set; }

    public string AssociateEntity { get; set; }

    public string AssociateProperty { get; set; }
}
