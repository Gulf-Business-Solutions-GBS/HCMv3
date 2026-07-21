using Abp.Application.Services.Dto;

namespace HCMv3.Organizations.Dto;

public class FindOrganizationUnitUsersOutputDto : EntityDto<long>
{
    public string Name { get; set; }

    public string Surname { get; set; }

    public string EmailAddress { get; set; }
}

