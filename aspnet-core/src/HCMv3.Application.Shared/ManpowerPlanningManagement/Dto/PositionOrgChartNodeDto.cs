using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// One drawn card on the Position Org Chart: a position, whoever currently holds it, and how many
/// positions sit beneath it.
///
/// A position is drawn as the person in it, because that is what makes the chart readable -- the position
/// name is the small heading and the holder's name is the card's title. A vacant position still gets a
/// card; <see cref="IsVacant"/> tells the screen to label it rather than leave the line blank.
///
/// Carries no photo bytes, for the same reason as the Employee Org Chart: the client asks
/// <c>GET /api/employee-photo/{id}</c> only for holders who have one.
/// </summary>
public class PositionOrgChartNodeDto : EntityDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? ParentPositionId { get; set; }

    /// <summary>True when nobody currently holds the position.</summary>
    public bool IsVacant { get; set; }

    /// <summary>The current holder, or null when vacant. Drives the card's avatar and its action button.</summary>
    public int? HolderEmployeeId { get; set; }

    public string HolderFullNameEn { get; set; }

    public string HolderFullNameAr { get; set; }

    /// <summary>Chooses the blank avatar when the holder has no photo. Meaningless when vacant.</summary>
    public Gender HolderGender { get; set; }

    public bool HolderHasPhoto { get; set; }

    /// <summary>Positions whose parent is this one.</summary>
    public int DirectChildCount { get; set; }

    /// <summary>Every position below this one, at any depth.</summary>
    public int TotalChildCount { get; set; }
}
