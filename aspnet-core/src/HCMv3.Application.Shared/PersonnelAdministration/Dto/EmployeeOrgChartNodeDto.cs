using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// One drawn card on the Employee Org Chart: a person, their job title as of the chart date, and how many
/// people report to them.
///
/// Carries no photo bytes. <see cref="HasPhoto"/> tells the client whether to request
/// <c>GET /api/employee-photo/{id}?size=</c> or go straight to the blank avatar -- a manager with a hundred
/// direct reports would otherwise drag a hundred photo blobs through this response.
/// </summary>
public class EmployeeOrgChartNodeDto : EntityDto
{
    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    /// <summary>Job title from the org assignment in force on the chart date; null when unassigned.</summary>
    public string JobTitleEn { get; set; }

    public string JobTitleAr { get; set; }

    /// <summary>Chooses which blank avatar the card falls back to when there is no photo.</summary>
    public Gender Gender { get; set; }

    /// <summary>True when <c>Employee.Photo</c> holds bytes. Computed without loading the blob.</summary>
    public bool HasPhoto { get; set; }

    /// <summary>People whose manager is this person on the chart date.</summary>
    public int DirectReportCount { get; set; }

    /// <summary>Everyone below this person in the reporting tree, at any depth, on the chart date.</summary>
    public int TotalReportCount { get; set; }
}
