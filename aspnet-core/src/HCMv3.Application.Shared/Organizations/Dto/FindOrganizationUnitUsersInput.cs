using HCMv3.Dto;

namespace HCMv3.Organizations.Dto;

public class FindOrganizationUnitUsersInput : PagedAndFilteredInputDto
{
    public long OrganizationUnitId { get; set; }
}

