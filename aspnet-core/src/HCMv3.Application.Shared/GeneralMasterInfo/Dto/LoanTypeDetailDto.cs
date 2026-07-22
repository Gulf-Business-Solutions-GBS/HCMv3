using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="LoanTypeDetail"/>.
/// </summary>
public class LoanTypeDetailDto : EntityDto<int>
{
    public int? LoanTypeId { get; set; }

    public string LoanTypeName { get; set; }

    public double Percent { get; set; }
}
