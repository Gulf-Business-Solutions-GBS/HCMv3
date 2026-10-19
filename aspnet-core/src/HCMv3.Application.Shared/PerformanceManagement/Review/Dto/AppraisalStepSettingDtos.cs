using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Review.Dto;

/// <summary>One "Appraisal WF Setup" row as the list shows it.</summary>
public class AppraisalStepSettingDto : EntityDto
{
    public int RequestMasterId { get; set; }

    public string RequestMasterNameEn { get; set; }

    public string RequestMasterNameAr { get; set; }

    public int Step { get; set; }

    public bool CanAddKpi { get; set; }

    public bool CanAddPeer { get; set; }

    public bool CanAddRatings { get; set; }

    public bool CanViewPeerRatings { get; set; }

    public bool CanViewRatings { get; set; }

    public bool CanPeerAddRatings { get; set; }

    public bool CanViewOverallAverageRating { get; set; }

    public bool CanEmployeeSelfRate { get; set; }

    public bool AutoAddManager { get; set; }

    public bool AutoAddRelationShip { get; set; }

    public bool AutoSubmitOnLastFill { get; set; }

    public int AllowedPeerCount { get; set; }

    /// <summary>The relationship types, comma-separated, in the reader's language.</summary>
    public string RelationshipTypeNames { get; set; }
}

/// <summary>Create or edit one "Appraisal WF Setup" row, with its relationship types.</summary>
public class CreateOrEditAppraisalStepSettingDto
{
    public int? Id { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RequestMasterId { get; set; }

    [Range(1, 999)]
    public int Step { get; set; }

    public bool CanAddKpi { get; set; }

    public bool CanAddPeer { get; set; }

    public bool CanAddRatings { get; set; }

    public bool CanViewPeerRatings { get; set; }

    public bool CanViewRatings { get; set; }

    public bool CanPeerAddRatings { get; set; }

    public bool CanViewOverallAverageRating { get; set; }

    public bool CanEmployeeSelfRate { get; set; }

    public bool AutoAddManager { get; set; }

    public bool AutoAddRelationShip { get; set; }

    public bool AutoSubmitOnLastFill { get; set; }

    [Range(0, 999)]
    public int AllowedPeerCount { get; set; }

    /// <summary>The relationship types "Auto Add Relationship" reads. Each at most once.</summary>
    public List<int> RelationshipTypeIds { get; set; } = new();
}

public class GetAppraisalStepSettingForEditOutput
{
    public CreateOrEditAppraisalStepSettingDto AppraisalStepSetting { get; set; }
}

public class GetAllAppraisalStepSettingsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? RequestMasterId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "RequestMasterId ASC, Step ASC";
        }
    }
}

/// <summary>A request master, with the steps it defines, for the setup screen's pickers.</summary>
public class AppraisalStepSettingRequestMasterDto : EntityDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public List<int> Steps { get; set; } = new();
}

/// <summary>A relationship type for the setup screen's picker.</summary>
public class AppraisalStepSettingRelationshipTypeDto : EntityDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

/// <summary>Everything the setup dialog's pickers need, in one call.</summary>
public class AppraisalStepSettingLookupsDto
{
    public List<AppraisalStepSettingRequestMasterDto> RequestMasters { get; set; } = new();

    public List<AppraisalStepSettingRelationshipTypeDto> RelationshipTypes { get; set; } = new();
}
