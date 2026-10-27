using HCMv3.Dto;

namespace HCMv3.Common.Dto;

public class FindUsersInput : PagedAndFilteredInputDto
{
    public int? TenantId { get; set; }

    public bool ExcludeCurrentUser { get; set; }
}

