using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TimeAttAPI"/>. <see cref="Password"/> is deliberately NOT included -- it
/// should never be sent back to the client.
/// </summary>
public class TimeAttAPIDto : EntityDto<int>
{
    public string TokenUrl { get; set; }

    public string Username { get; set; }

    public string TransactionsUrl { get; set; }

    public int NoOfRecords { get; set; }

    public string DevicesUrl { get; set; }

    public string PunchInCode { get; set; }

    public string PunchOutCode { get; set; }

    public string BreakOutCode { get; set; }

    public string BreakInCode { get; set; }

    public string Email { get; set; }

    public string Company { get; set; }
}
