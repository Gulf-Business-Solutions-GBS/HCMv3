using HCMv3.SystemSecurity.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

public class FileTypeDetectorTests
{
    [Fact]
    public void Should_Detect_Pdf_Signature()
    {
        var bytes = new byte[20];
        bytes[0] = 0x25;
        bytes[1] = 0x50;
        bytes[2] = 0x44;
        bytes[3] = 0x46;

        FileTypeDetector.GetFileType(bytes).ShouldBe(".pdf");
    }

    [Fact]
    public void Should_Detect_Png_Signature()
    {
        var bytes = new byte[20];
        bytes[0] = 0x89;
        bytes[1] = 0x50;
        bytes[2] = 0x4E;
        bytes[3] = 0x47;

        FileTypeDetector.GetFileType(bytes).ShouldBe(".png");
    }

    [Fact]
    public void Should_Detect_ZipContainer_Signature_For_Docx()
    {
        var bytes = new byte[20];
        bytes[0] = 0x50;
        bytes[1] = 0x4B;
        bytes[2] = 0x03;
        bytes[3] = 0x04;

        FileTypeDetector.GetFileType(bytes).ShouldBe(".zipcontainer");
    }

    [Fact]
    public void Should_Return_Unknown_For_Too_Short_Byte_Array()
    {
        FileTypeDetector.GetFileType(new byte[] { 1, 2, 3 }).ShouldBe("unknown");
    }

    [Fact]
    public void Should_Return_Unknown_For_Null_Input()
    {
        FileTypeDetector.GetFileType(null).ShouldBe("unknown");
    }

    [Fact]
    public void Should_Return_Unknown_For_Unrecognized_Signature()
    {
        var bytes = new byte[20];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = 0x01;
        }

        FileTypeDetector.GetFileType(bytes).ShouldBe("unknown");
    }
}
