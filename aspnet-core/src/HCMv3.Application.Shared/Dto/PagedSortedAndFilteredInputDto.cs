using Abp.Application.Services.Dto;

namespace HCMv3.Dto;

public class PagedSortedAndFilteredInputDto : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}

