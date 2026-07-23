using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TicketPaxType"/>.
/// </summary>
public class TicketPaxTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public double AgeFrom { get; set; }

    public double AgeTo { get; set; }
}
