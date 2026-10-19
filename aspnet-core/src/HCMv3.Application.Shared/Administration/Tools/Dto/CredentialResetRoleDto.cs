using Abp.Application.Services.Dto;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>A role the Reset User Passwords tool can filter by, or reset as a whole.</summary>
public class CredentialResetRoleDto : EntityDto
{
    public string DisplayName { get; set; }
}
