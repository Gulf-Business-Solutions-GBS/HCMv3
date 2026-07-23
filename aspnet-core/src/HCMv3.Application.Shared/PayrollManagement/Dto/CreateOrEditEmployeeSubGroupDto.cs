using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroup"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditEmployeeSubGroupDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeGroupId { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code.
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
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
