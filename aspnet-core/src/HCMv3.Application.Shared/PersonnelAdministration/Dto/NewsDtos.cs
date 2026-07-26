using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class NewsDto : EntityDto<int>
{
    public int? NewsTypeId { get; set; }
    public string NewsTypeNameEn { get; set; }
    public string Title { get; set; }
    public string TitleAr { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int? OrganizationUnitTypeId { get; set; }
    public string OrganizationUnitTypeNameEn { get; set; }
    public long? OrganizationUnitId { get; set; }
    public string OrganizationUnitDisplayName { get; set; }
    public bool SendNotification { get; set; }
    public bool SendEmail { get; set; }
    public string Comments { get; set; }
    public string CommentAr { get; set; }
    public bool NotificationSent { get; set; }
    public string DocumentHtml { get; set; }
}

public class CreateOrEditNewsDto
{
    public int? Id { get; set; }

    public int? NewsTypeId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; }

    [StringLength(256)]
    public string TitleAr { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public int? OrganizationUnitTypeId { get; set; }

    public long? OrganizationUnitId { get; set; }

    public bool SendNotification { get; set; }

    public bool SendEmail { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    [StringLength(500)]
    public string CommentAr { get; set; }

    public bool NotificationSent { get; set; }

    public string DocumentHtml { get; set; }
}

public class GetNewsForEditOutput
{
    public CreateOrEditNewsDto News { get; set; }
}

public class GetAllNewsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "IssueDate DESC";
        }
    }
}
