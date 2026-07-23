using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TicketType"/>.
/// </summary>
public class TicketTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public LeaveBasisPeriod TicketBasisPeriod { get; set; }

    public double TicketBasisFixedDays { get; set; }

    public bool Lapse { get; set; }

    public int LapseAfter { get; set; }

    public int StopDependantAccrualAfter { get; set; }

    public double MinCompensation { get; set; }

    public DeductionPeriod DeductionPeriod { get; set; }

    public QuotaEntitlement QuotaEntitlement { get; set; }

    public int? WageTypeId { get; set; }

    public string WageTypeName { get; set; }
}
