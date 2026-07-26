using System.Text;

namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// Byte-signature file-type sniffer (ported verbatim from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.StorageService.GetFileType</c>). Pure computation, no
/// DB/session dependency -- kept as a plain static class. See <see cref="StorageService"/> for the
/// (stubbed) DB-dependent parts of the legacy <c>StorageService</c>.
/// </summary>
public static class FileTypeDetector
{
    public static string GetFileType(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length < 16)
        {
            return "unknown";
        }

        // PDF
        if (fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46)
        {
            return ".pdf";
        }

        // JPG / JPEG
        if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
        {
            return ".jpg";
        }

        // PNG
        if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
        {
            return ".png";
        }

        // GIF
        if (fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46)
        {
            return ".gif";
        }

        // BMP
        if (fileBytes[0] == 0x42 && fileBytes[1] == 0x4D)
        {
            return ".bmp";
        }

        // TIFF
        if ((fileBytes[0] == 0x49 && fileBytes[1] == 0x49 && fileBytes[2] == 0x2A && fileBytes[3] == 0x00) ||
            (fileBytes[0] == 0x4D && fileBytes[1] == 0x4D && fileBytes[2] == 0x00 && fileBytes[3] == 0x2A))
        {
            return ".tiff";
        }

        // OLD OFFICE FORMATS: DOC / XLS / PPT
        if (fileBytes[0] == 0xD0 && fileBytes[1] == 0xCF && fileBytes[2] == 0x11 && fileBytes[3] == 0xE0)
        {
            return ".office";
        }

        // ZIP-BASED FORMATS: ZIP / DOCX / XLSX / PPTX
        if (fileBytes[0] == 0x50 && fileBytes[1] == 0x4B && fileBytes[2] == 0x03 && fileBytes[3] == 0x04)
        {
            return ".zipcontainer";
        }

        // RAR
        if (fileBytes[0] == 0x52 && fileBytes[1] == 0x61 && fileBytes[2] == 0x72 && fileBytes[3] == 0x21)
        {
            return ".rar";
        }

        // 7Z
        if (fileBytes[0] == 0x37 && fileBytes[1] == 0x7A && fileBytes[2] == 0xBC && fileBytes[3] == 0xAF)
        {
            return ".7z";
        }

        // MP3
        if ((fileBytes[0] == 0x49 && fileBytes[1] == 0x44 && fileBytes[2] == 0x33) ||
            (fileBytes[0] == 0xFF && (fileBytes[1] & 0xE0) == 0xE0))
        {
            return ".mp3";
        }

        // WAV / AVI
        if (fileBytes[0] == 0x52 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x46)
        {
            return ".wav";
        }

        // HEIC / MP4 / MOV
        if (fileBytes.Length > 12 && fileBytes[4] == 0x66 && fileBytes[5] == 0x74 && fileBytes[6] == 0x79 && fileBytes[7] == 0x70)
        {
            var brand = Encoding.ASCII.GetString(fileBytes, 8, 4).ToLower();

            if (brand.StartsWith("hei") || brand.StartsWith("hev") || brand.StartsWith("mif1") || brand.StartsWith("msf1"))
            {
                return ".heic";
            }

            if (brand.StartsWith("qt"))
            {
                return ".mov";
            }

            if (brand.StartsWith("iso") || brand.StartsWith("mp4") || brand.StartsWith("mp"))
            {
                return ".mp4";
            }
        }

        // XML
        if (fileBytes[0] == 0x3C && fileBytes[1] == 0x3F && fileBytes[2] == 0x78 && fileBytes[3] == 0x6D)
        {
            return ".xml";
        }

        // HTML
        if (fileBytes[0] == 0x3C && fileBytes[1] == 0x68 && fileBytes[2] == 0x74 && fileBytes[3] == 0x6D)
        {
            return ".html";
        }

        // EXE / DLL
        if (fileBytes[0] == 0x4D && fileBytes[1] == 0x5A)
        {
            return ".exe";
        }

        return "unknown";
    }
}
