using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroup"/>.
/// </summary>
public class EmployeeSubGroupDto : EntityDto<int>
{
    public int EmployeeGroupId { get; set; }

    public string EmployeeGroupName { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? TicketClassId { get; set; }

    public bool PayOvertime { get; set; }

    public AirTicketPayType AirTicketPayType { get; set; }

    public double MaxHealthReimburseAmount { get; set; }

    public CustomStartMonth HealthInsurancePeriod { get; set; }

    public bool AutoPayAirTicket { get; set; }

    public int MaxDependantAirTicketCount { get; set; }

    public int MaxDependantHealthReimburseCount { get; set; }
}
