using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="DisciplinaryTypeDetail"/>.
/// </summary>
public class DisciplinaryTypeDetailDto : EntityDto<int>
{
    public int? DisciplinaryTypeId { get; set; }

    public string DisciplinaryTypeName { get; set; }

    public int DisciplinaryCount { get; set; }

    public int? DisciplinaryDeductionTypeId { get; set; }

    public string DisciplinaryDeductionTypeName { get; set; }
}
