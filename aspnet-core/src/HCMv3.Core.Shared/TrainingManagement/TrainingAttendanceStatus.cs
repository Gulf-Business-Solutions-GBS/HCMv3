namespace HCMv3.TrainingManagement;

/// <summary>
/// Attendance status for a <c>TrainingAttendance</c> row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.Attendance</c>). Renamed from the legacy bare
/// <c>Attendance</c> to avoid any future collision with a generic "attendance" concept in another module
/// (e.g. TimeManagement/PersonnelAdministration).
/// </summary>
public enum TrainingAttendanceStatus
{
    Present = 0,
    Absent = 1
}
