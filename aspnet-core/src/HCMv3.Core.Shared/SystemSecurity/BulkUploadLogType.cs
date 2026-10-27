namespace HCMv3.SystemSecurity;

/// <summary>
/// Kind of bulk-upload operation a <c>BulkUploadLog</c> row records (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSecurity.LogType</c>). Renamed from the legacy bare <c>LogType</c> to
/// avoid a generic/collision-prone name.
/// </summary>
public enum BulkUploadLogType
{
    /// <summary>
    /// Rows written by the generic bulk import/export engine (legacy
    /// <c>uscBulkImportExport</c>). Legacy's <c>LogType</c> enum had no member for this: that control simply
    /// never assigned <c>LogType</c>, so its rows persisted the CLR default <c>0</c>. This member names that
    /// value rather than inventing a new one, so existing data keeps its meaning.
    /// </summary>
    BulkDataImport = 0,

    ProfilePhoto = 1
}
