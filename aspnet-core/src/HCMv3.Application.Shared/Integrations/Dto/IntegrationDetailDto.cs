using System.Collections.Generic;

namespace HCMv3.Integrations.Dto;

/// <summary>What the Configure dialog needs: the card, its form, and its recent attempts.</summary>
public class IntegrationDetailDto
{
    public IntegrationTileDto Tile { get; set; }

    public List<IntegrationFieldDto> Fields { get; set; }

    public List<IntegrationAttemptDto> RecentAttempts { get; set; }
}
