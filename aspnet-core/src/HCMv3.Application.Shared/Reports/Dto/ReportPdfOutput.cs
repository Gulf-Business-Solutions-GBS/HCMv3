namespace HCMv3.Reports.Dto;

/// <summary>
/// Output of a report-generation app service method. The PDF bytes are returned as a base64 string
/// (same convention as <c>GetProfilePictureOutput.ProfilePicture</c>) rather than a raw binary
/// response, since ABP dynamic app-service actions serialize return values as JSON. The Angular
/// client decodes <see cref="Base64Content"/> into a Blob and opens it in a new tab.
/// </summary>
public class ReportPdfOutput
{
    public string FileName { get; set; }

    public string Base64Content { get; set; }

    public ReportPdfOutput(string fileName, string base64Content)
    {
        FileName = fileName;
        Base64Content = base64Content;
    }
}
