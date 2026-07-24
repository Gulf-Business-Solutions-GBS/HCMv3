using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAPeerNomineeDto : EntityDto<int>
{
    public int PAPeriodId { get; set; }

    public string PAPeriodName { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int PeerId { get; set; }

    public string PeerFullNameEn { get; set; }

    public bool Submitted { get; set; }
}
