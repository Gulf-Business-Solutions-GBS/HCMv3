using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Configuration;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Services;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Tests for the offline (file drop) delivery channel.
///
/// Two concerns, and the second matters more than the first. Writing the file is easy to get right;
/// what needs holding down is that an administrator-entered folder name can never place a payroll
/// spreadsheet outside the drop root, and that two recipients of the same slice never collide -- the
/// whole point of this channel is that their files differ, and an overwrite destroys that evidence.
/// </summary>
// ReSharper disable once InconsistentNaming
public class FileDropReportSliceDeliveryChannel_Tests : IDisposable
{
    private readonly string _root;
    private readonly FileDropReportSliceDeliveryChannel _channel;

    public FileDropReportSliceDeliveryChannel_Tests()
    {
        _root = Path.Combine(Path.GetTempPath(), "hcmv3-filedrop-tests", Guid.NewGuid().ToString("N"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>(
                    FileDropReportSliceDeliveryChannel.RootPathConfigurationKey, _root),
            })
            .Build();

        var accessor = Substitute.For<IAppConfigurationAccessor>();
        accessor.Configuration.Returns(configuration);

        _channel = new FileDropReportSliceDeliveryChannel(accessor);
    }

    [Fact]
    public void Should_Deliver_Through_The_Offline_Method()
    {
        _channel.Method.ShouldBe(ReportDestinationMethod.Offline);
    }

    [Fact]
    public async Task Should_Write_The_Rendered_Bytes_Under_The_Configured_Root()
    {
        var content = new byte[] { 1, 2, 3, 4 };

        await _channel.DeliverAsync(
            Schedule(),
            Destination("attendance/monthly"),
            Slice("Finance"),
            Recipient("head@example.com"),
            Rendered(content));

        var written = Files();
        written.Length.ShouldBe(1);
        File.ReadAllBytes(written[0]).ShouldBe(content);

        // The folder the administrator asked for, beneath the root and the tenant.
        written[0].ShouldContain("tenant-1");
        written[0].ShouldContain("attendance");
        written[0].ShouldContain("monthly");
    }

    [Fact]
    public async Task Should_Give_Each_Recipient_Of_A_Slice_Its_Own_File()
    {
        // Two heads of the same slice can hold different scopes, so their files legitimately differ.
        // Sharing a name would let the second overwrite the first -- silently deleting the evidence
        // that scoping worked at all.
        await _channel.DeliverAsync(
            Schedule(), Destination("drop"), Slice("Finance"),
            Recipient("first@example.com"), Rendered(new byte[] { 1 }));

        await _channel.DeliverAsync(
            Schedule(), Destination("drop"), Slice("Finance"),
            Recipient("second@example.com"), Rendered(new byte[] { 2 }));

        Files().Length.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Overwrite_A_File_From_An_Earlier_Run()
    {
        // Same schedule, same slice, same recipient, same day. Overwriting would read as the earlier
        // run never having happened.
        for (var i = 0; i < 3; i++)
        {
            await _channel.DeliverAsync(
                Schedule(), Destination("drop"), Slice("Finance"),
                Recipient("head@example.com"), Rendered(new byte[] { (byte)i }));
        }

        Files().Length.ShouldBe(3);
    }

    [Theory]
    [InlineData("../../../windows/system32")]
    [InlineData("..\\..\\escape")]
    [InlineData("/etc")]
    [InlineData("C:\\Windows\\Temp")]
    public async Task Should_Never_Write_Outside_The_Drop_Root(string folder)
    {
        // StorageFolder is administrator-entered text reachable through the API. If any of these
        // escaped, anyone who could save a schedule could write a payroll spreadsheet into a web root.
        await _channel.DeliverAsync(
            Schedule(), Destination(folder), Slice("Finance"),
            Recipient("head@example.com"), Rendered(new byte[] { 1 }));

        var written = Files();
        written.Length.ShouldBe(1);
        Path.GetFullPath(written[0]).ShouldStartWith(Path.GetFullPath(_root));
    }

    [Fact]
    public async Task Should_Keep_Tenants_Apart_Even_With_The_Same_Folder_Name()
    {
        await _channel.DeliverAsync(
            Schedule(tenantId: 1), Destination("shared"), Slice("Finance"),
            Recipient("head@example.com"), Rendered(new byte[] { 1 }));

        await _channel.DeliverAsync(
            Schedule(tenantId: 2), Destination("shared"), Slice("Finance"),
            Recipient("head@example.com"), Rendered(new byte[] { 2 }));

        var written = Files();
        written.Length.ShouldBe(2);
        written.Count(p => p.Contains("tenant-1")).ShouldBe(1);
        written.Count(p => p.Contains("tenant-2")).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_To_Write_An_Empty_Slice()
    {
        // The executor skips empty slices, so reaching the channel with no bytes is a renderer bug. A
        // 0-byte file would be recorded as a successful delivery.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _channel.DeliverAsync(
                Schedule(), Destination("drop"), Slice("Finance"),
                Recipient("head@example.com"), Rendered(Array.Empty<byte>())));
    }

    [Fact]
    public async Task Should_Still_Name_A_File_For_A_Recipient_With_No_Email()
    {
        // An org-unit head resolved from the chart may have no user account. E-mail cannot deliver to
        // them; a file drop can, so this must not throw the way the e-mail channel does.
        await _channel.DeliverAsync(
            Schedule(),
            Destination("drop"),
            Slice("Finance"),
            new ReportSliceRecipient { EmployeeId = 42, DisplayName = "Layla Al-Sabah" },
            Rendered(new byte[] { 1 }));

        Files().Single().ShouldContain("Layla");
    }

    private string[] Files() =>
        Directory.Exists(_root)
            ? Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();

    private static ReportSchedule Schedule(int tenantId = 1) =>
        new ReportSchedule { Id = 7, TenantId = tenantId, Name = "Monthly attendance" };

    private static ReportScheduleDestination Destination(string folder) =>
        new ReportScheduleDestination
        {
            Method = ReportDestinationMethod.Offline,
            StorageFolder = folder,
        };

    private static ReportBurstSlice Slice(string display) =>
        new ReportBurstSlice { Key = display, Display = display };

    private static ReportSliceRecipient Recipient(string email) =>
        new ReportSliceRecipient { Email = email, DisplayName = email };

    private static RenderedReportSlice Rendered(byte[] content) =>
        new RenderedReportSlice
        {
            Content = content,
            FileName = "Monthly attendance - Finance.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            RowCount = content.Length,
        };

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temp folder is not worth failing a test run over.
        }
    }
}
