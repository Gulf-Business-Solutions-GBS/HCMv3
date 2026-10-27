namespace HCMv3.ReportManagement;

/// <summary>
/// Grid column data type (ported from legacy XAF <c>HCM10.Module.BusinessObjects.ReportManagement.ReportSettingDetail</c>
/// nested <c>DataType</c> enum -- renamed here to avoid ambiguity with
/// <see cref="System.ComponentModel.DataAnnotations.DataType"/>).
/// </summary>
public enum ReportColumnDataType
{
    Int = 0,
    Double = 1,
    String = 2,
    DateTime = 3,
    Boolean = 4,
}
