using System;
using System.Collections.Generic;

namespace HCMv3.ImportExport;

/// <summary>
/// Serialized payload of a queued generic bulk import.
///
/// <para>Mirrors legacy <c>uscBulkImportExport.ExecuteLongOperation</c>'s <c>args[]</c> array
/// (<c>args[0]</c> = entity full name, <c>args[1]</c> = file, <c>args[2]</c> = import type) plus the static
/// <c>OnlyValidate</c> flag, with <c>TenantId</c> added because ABP background jobs start outside any tenant
/// scope.</para>
///
/// <para><b>DEVIATION.</b> Legacy <c>args[1]</c> was a path to a file the upload handler had written under
/// <c>~/TempExcel</c> (<c>GetFilePath()</c>, which also deleted and recreated that whole directory on every
/// upload -- a race between concurrent users). The bytes travel with the job here instead; nothing is
/// written to disk.</para>
/// </summary>
[Serializable]
public class BulkImportJobArgs
{
    public int TenantId { get; set; }

    /// <summary>Legacy <c>args[0]</c>.</summary>
    public string EntityFullName { get; set; }

    /// <summary>Legacy <c>args[1]</c>, as bytes rather than a server path.</summary>
    public byte[] FileContent { get; set; }

    public string FileName { get; set; }

    /// <summary>Legacy <c>args[2]</c> (<c>"I"</c>/<c>"P"</c>).</summary>
    public BulkImportType ImportType { get; set; }

    /// <summary>Legacy static <c>OnlyValidate</c>.</summary>
    public bool ValidateOnly { get; set; }

    public string CurrentUserName { get; set; }
}

/// <summary>
/// Serialized payload of a queued profile-photo import (legacy <c>uscBulkImport</c>). Legacy's equivalent
/// was the single <c>txtFolderPath</c> string; see <c>IProfilePhotoImportService</c> for why the images
/// travel with the job instead.
/// </summary>
[Serializable]
public class ProfilePhotoImportJobArgs
{
    public int TenantId { get; set; }

    public List<ProfilePhotoJobFile> Files { get; set; } = new List<ProfilePhotoJobFile>();
}

[Serializable]
public class ProfilePhotoJobFile
{
    public string FileName { get; set; }

    public byte[] Content { get; set; }
}
