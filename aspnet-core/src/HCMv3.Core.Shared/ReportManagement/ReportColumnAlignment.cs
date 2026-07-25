namespace HCMv3.ReportManagement;

/// <summary>
/// Grid column horizontal alignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.ReportSettingDetail</c> nested <c>HAlignment</c> enum --
/// renamed here for clarity since the legacy's commented-out <c>VAlignment</c> counterpart was never ported).
/// </summary>
public enum ReportColumnAlignment
{
    Left = 0,
    Center = 1,
    Right = 2,
}
