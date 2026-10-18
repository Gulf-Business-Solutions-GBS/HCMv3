using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.OrganizationManagement.Dto;

public class JobTitleDetailDto : EntityDto<int>
{
    public int JobTitleId { get; set; }

    public string JobTitleName { get; set; }

    public int WorkflowRoleId { get; set; }

    public string WorkflowRoleName { get; set; }
}

public class CreateOrEditJobTitleDetailDto
{
    public int? Id { get; set; }

    public int JobTitleId { get; set; }

    /// <summary>Required; legacy refused a row without one ("WorkflowRole cannot be blank").</summary>
    public int WorkflowRoleId { get; set; }
}

public class GetJobTitleDetailForEditOutput
{
    public CreateOrEditJobTitleDetailDto JobTitleDetail { get; set; }
}

public class GetAllJobTitleDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by job title.</summary>
    public int? JobTitleId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
