namespace HCMv3.SystemSecurity;

/// <summary>
/// Kind of bulk-upload operation a <c>BulkUploadLog</c> row records (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSecurity.LogType</c>). Renamed from the legacy bare <c>LogType</c> to
/// avoid a generic/collision-prone name.
/// </summary>
public enum BulkUploadLogType
{
    ProfilePhoto = 1
}
