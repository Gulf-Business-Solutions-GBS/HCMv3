using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbAppTaskComments"/>.
/// </summary>
public class OnbAppTaskCommentsDto : EntityDto<int>
{
    public int OnbApplicantTasksId { get; set; }

    public string Comments { get; set; }

    public bool IsByAdmin { get; set; }
}
