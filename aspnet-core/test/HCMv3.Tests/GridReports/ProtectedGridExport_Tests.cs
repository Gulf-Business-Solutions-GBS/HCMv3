using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.MasterReport;
using HCMv3.GridReports.MasterReport.Dto;
using HCMv3.ReportManagement.Services;
using HCMv3.Storage;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Legacy <c>ManageSecuritySettings.EnableReportExportPwd</c>: grid exports ask for the signed-in user's user
/// name before they open.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProtectedGridExport_Tests : AppTestBase
{
    private int TenantId => AbpSession.TenantId ?? 1;

    public ProtectedGridExport_Tests()
    {
        LoginAsDefaultTenantAdmin();
    }

    private sealed class RecordingWriter : IEncryptedWorkbookWriter
    {
        public static readonly byte[] Output = { 0xD0, 0xCF, 0x11, 0xE0 };
        public string Password;
        public DataTable Table;
        public string NumberFormat;

        public byte[] WriteXlsx(DataTable table, ICollection<int> numericColumnIndexes, string numberFormat, string password)
        {
            Table = table;
            NumberFormat = numberFormat;
            Password = password;
            return Output;
        }
    }

    private RecordingWriter RegisterWriter()
    {
        var writer = new RecordingWriter();
        LocalIocManager.IocContainer.Register(
            Component.For<IEncryptedWorkbookWriter>().Instance(writer).IsDefault());
        return writer;
    }

    private void SetExportPassword(bool enabled)
    {
        UsingDbContext(context =>
        {
            var security = context.Set<ManageSecuritySettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManageSecuritySettings>().Add(new ManageSecuritySettings { TenantId = TenantId }).Entity;
            security.EnableReportExportPwd = enabled;

            var report = context.Set<ManageReportSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                         ?? context.Set<ManageReportSettings>().Add(new ManageReportSettings { TenantId = TenantId }).Entity;
            report.DecimalInReports = 2;
        });
    }

    private byte[] ReadFile(string token) => Resolve<ITempFileCacheManager>().GetFile(token);

    [Fact]
    public async Task Should_Protect_The_Export_With_The_User_Name_When_The_Setting_Is_On()
    {
        var writer = RegisterWriter();
        SetExportPassword(true);

        var file = await Resolve<IAssetReportAppService>().GetAssetReportToExcel(new GetAssetReportInput());

        writer.Password.ShouldBe("admin");
        writer.NumberFormat.ShouldBe("#,##0.00");
        writer.Table.Columns.Count.ShouldBeGreaterThan(0);
        ReadFile(file.FileToken).ShouldBe(RecordingWriter.Output);
    }

    [Fact]
    public async Task Should_Export_A_Plain_Workbook_When_The_Setting_Is_Off()
    {
        var writer = RegisterWriter();
        SetExportPassword(false);

        var file = await Resolve<IAssetReportAppService>().GetAssetReportToExcel(new GetAssetReportInput());

        writer.Password.ShouldBeNull();
        var bytes = ReadFile(file.FileToken);
        bytes[0].ShouldBe((byte)'P');
        bytes[1].ShouldBe((byte)'K');
    }

    [Fact]
    public async Task Should_Not_Password_A_Scheduled_Distribution()
    {
        var writer = RegisterWriter();
        SetExportPassword(true);

        using (Resolve<IReportScopeOverride>().Begin(null))
        {
            await Resolve<IAssetReportAppService>().GetAssetReportToExcel(new GetAssetReportInput());
        }

        writer.Password.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_Rather_Than_Hand_Out_An_Unprotected_File_When_No_Writer_Is_Available()
    {
        SetExportPassword(true);

        await Should.ThrowAsync<UserFriendlyException>(
            () => Resolve<IAssetReportAppService>().GetAssetReportToExcel(new GetAssetReportInput()));
    }
}
