using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TravelFeedback"/>.
/// </summary>
public class TravelFeedbackDto : EntityDto<int>
{
    /// <summary>Null for an applicant's row.</summary>
    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    /// <summary>Set instead of the employee fields when the row belongs to a job applicant.</summary>
    public int? ApplicantId { get; set; }

    public string ApplicantCode { get; set; }

    public string ApplicantFullNameEn { get; set; }

    public int? TravelId { get; set; }

    public string TravelCode { get; set; }

    public DateTime ClaimDate { get; set; }

    public double ClaimAmount { get; set; }

    public string Comments { get; set; }
}
