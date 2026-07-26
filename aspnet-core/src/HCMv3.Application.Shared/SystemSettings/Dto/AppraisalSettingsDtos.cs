using Abp.Application.Services.Dto;
using HCMv3.SystemSettings;

namespace HCMv3.SystemSettings.Dto;

public class AppraisalSettingsListDto : EntityDto
{
    public AppraisalCycle AppraisalCycle { get; set; }

    public bool EnablePAPeerReview { get; set; }

    public int SetPeerMinLimit { get; set; }

    public int SetPeerMaxLimit { get; set; }

    public bool EnableMarksInPAReview { get; set; }

    public MarksOn MarksOn { get; set; }
}

public class AppraisalSettingsEditDto : EntityDto
{
    public AppraisalCycle AppraisalCycle { get; set; }

    public bool EnablePAPeerReview { get; set; }

    public int SetPeerMinLimit { get; set; }

    public int SetPeerMaxLimit { get; set; }

    public bool EnableMarksInPAReview { get; set; }

    public MarksOn MarksOn { get; set; }
}
