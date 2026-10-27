using Abp.Application.Services.Dto;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="ProjectRegisterDetail"/>.
/// </summary>
public class ProjectRegisterDetailDto : EntityDto<int>
{
    public int ProjectRegisterId { get; set; }

    public int ProjectId { get; set; }

    public string ProjectNameEn { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeNameEn { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
