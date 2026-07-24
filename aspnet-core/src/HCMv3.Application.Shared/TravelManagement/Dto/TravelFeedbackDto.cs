using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TravelFeedback"/>.
/// </summary>
public class TravelFeedbackDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int? TravelId { get; set; }

    public string TravelCode { get; set; }

    public DateTime ClaimDate { get; set; }

    public double ClaimAmount { get; set; }

    public string Comments { get; set; }
}
