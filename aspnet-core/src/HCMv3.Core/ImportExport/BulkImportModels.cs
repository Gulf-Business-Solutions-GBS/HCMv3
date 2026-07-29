using System;
using System.Collections.Generic;

namespace HCMv3.ImportExport;

/// <summary>
/// One entity type that the bulk import/export engine can round-trip.
///
/// <para>Ported from legacy <c>uscBulkImportExport.BindModuleName()</c>, which walked every loaded
/// <c>HCM10*</c> assembly and offered every type whose <c>BaseType</c> was <c>MyBaseObject</c>. The HCMv3
/// equivalent of "is a persistent business object" is "is a concrete <c>IEntity&lt;int&gt;</c> declared in
/// HCMv3.Core" -- see <see cref="IBulkImportExportService.GetImportableEntities"/>.</para>
/// </summary>
public class BulkEntityDescriptor
{
    /// <summary>Short type name, e.g. <c>Employee</c>. Legacy combo <i>text</i>.</summary>
    public string Name { get; set; }

    /// <summary>Assembly-qualified-free full type name, e.g. <c>HCMv3.PersonnelAdministration.Employee</c>.
    /// Legacy combo <i>value</i>, and the identifier every other method on the engine takes.</summary>
    public string FullName { get; set; }

    /// <summary>Feature folder the type lives in (the namespace segment after <c>HCMv3.</c>). No legacy
    /// equivalent -- added so the Angular picker can group ~600 entity names into something usable.</summary>
    public string Area { get; set; }

    /// <summary>The exact header row the template/export will carry, in order.</summary>
    public List<string> Columns { get; set; } = new List<string>();
}

/// <summary>Arguments for one bulk import run. Mirrors legacy's <c>args[0..2]</c> plus the static
/// <c>uscBulkImportExport.OnlyValidate</c> flag.</summary>
public class BulkImportRunInput
{
    /// <summary>Legacy <c>args[0]</c>: the selected entity's full type name.</summary>
    public string EntityFullName { get; set; }

    /// <summary>The uploaded <c>.xlsx</c> bytes. Legacy <c>args[1]</c> was a <i>server file path</i>
    /// (<c>GetFilePath()</c> saved the upload under <c>~/TempExcel</c>); this port never writes the payload
    /// to disk.</summary>
    public byte[] FileContent { get; set; }

    /// <summary>Original file name -- recorded on the <c>ExecutionLog</c> parameters only.</summary>
    public string FileName { get; set; }

    /// <summary>Legacy <c>args[2]</c>.</summary>
    public BulkImportType ImportType { get; set; }

    /// <summary>Legacy static <c>OnlyValidate</c>: when true, rows are validated and logged but nothing is
    /// written and Purge is skipped ("Validate Data" action).</summary>
    public bool ValidateOnly { get; set; }

    /// <summary>Recorded on the <c>ExecutionLog</c> row. No legacy equivalent (legacy was single-user).</summary>
    public string CurrentUserName { get; set; }

    /// <summary>
    /// Tenant the run belongs to. No legacy equivalent (legacy was single-tenant). Required because the run
    /// executes inside an ABP background job, which starts outside any tenant scope, and every row this
    /// engine writes is <c>IMustHaveTenant</c>.
    /// </summary>
    public int TenantId { get; set; }
}

/// <summary>Outcome of one bulk import run -- the numbers legacy's
/// <c>CalculateProcessStatus()</c> recomputed from <c>BulkUploadLog</c> on every timer tick.</summary>
public class BulkImportRunResult
{
    public int ExecutionLogId { get; set; }

    public int TotalRecords { get; set; }

    public int Successfull { get; set; }

    public int Failed { get; set; }

    /// <summary>True when the run stopped early because a <c>BulkUploadLog.StopFlag</c> row was found
    /// (legacy's <c>btnStopUploadingFile</c> handshake).</summary>
    public bool Aborted { get; set; }

    public bool ValidateOnly { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime FinishedAt { get; set; }
}
