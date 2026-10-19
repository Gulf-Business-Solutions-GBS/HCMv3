namespace HCMv3.GridReports.Dto;

/// <summary>
/// The caption of one legacy <c>OrgLevel1</c>..<c>OrgLevel7</c> report column: the tenant's
/// Organization Unit Type name for that level, in the current language.
/// </summary>
public class OrgLevelCaptionDto
{
    /// <summary>1..7 -- matches the report row fields <c>OrgLevel1</c>..<c>OrgLevel7</c>.</summary>
    public int Level { get; set; }

    /// <summary>The Organization Unit Type code, <c>Level1</c>..<c>Level7</c>.</summary>
    public string Code { get; set; }

    /// <summary>NameAr when the request culture is Arabic, otherwise NameEn.</summary>
    public string Caption { get; set; }
}
