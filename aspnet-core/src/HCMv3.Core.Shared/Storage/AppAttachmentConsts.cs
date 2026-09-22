namespace HCMv3.Storage;

public static class AppAttachmentConsts
{
    public const int MaxEntityTypeLength = 128;

    public const int MaxEntityIdLength = 64;

    public const int MaxFileNameLength = 260;

    public const int MaxContentTypeLength = 128;

    public const int MaxStoragePathLength = 512;

    /// <summary>Legacy's default when the tenant has not set a per-file limit.</summary>
    public const int DefaultMaxFileSizeMB = 5;

    /// <summary>
    /// Used when the tenant has not listed allowed extensions. Legacy allowed anything in that case; a
    /// document store served back to browsers should not accept .html, .svg or .exe by default.
    /// </summary>
    public const string DefaultAllowedExtensions = ".pdf,.jpg,.jpeg,.png,.gif,.bmp,.tif,.tiff,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.csv,.zip";
}
