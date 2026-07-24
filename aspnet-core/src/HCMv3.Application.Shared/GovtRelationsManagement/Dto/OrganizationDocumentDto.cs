using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="OrganizationDocument"/>.
/// </summary>
public class OrganizationDocumentDto : EntityDto<int>
{
    public long OrganizationUnitId { get; set; }

    public string OrganizationUnitName { get; set; }

    public int DocumentTypeId { get; set; }

    public string DocumentTypeName { get; set; }

    public string DocumentNum { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string Comments { get; set; }

    public string DocumentPlace { get; set; }

    public bool NotificationSent { get; set; }

    public int? SponsorId { get; set; }

    public string SponsorName { get; set; }
}
