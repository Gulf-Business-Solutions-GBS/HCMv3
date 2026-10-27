using Abp.Application.Services.Dto;

namespace HCMv3.Notifications.Dto;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}

