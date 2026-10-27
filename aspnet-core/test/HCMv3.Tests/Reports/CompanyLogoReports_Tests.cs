using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using Abp.Threading;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.UI;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Services;
using HCMv3.Reports;
using HCMv3.Reports.Bank;
using HCMv3.Reports.EmployeeInfo;
using HCMv3.Reports.Others;
using HCMv3.Reports.Payroll;
using HCMv3.Reports.QuotaProvision;
using HCMv3.Reports.Status;
using HCMv3.Reports.Time;
using HCMv3.Reports.Transaction;
using HCMv3.Storage;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace HCMv3.Tests.Reports;

/// <summary>
/// The company logo on the printed reports: every report whose legacy layout printed one is wired to
/// <see cref="HcmReportBase.CompanyLogoLoader"/> (which the web host maps to <see cref="ICompanyLogoResolver"/>),
/// its SQL returns what the resolver needs, and a missing logo leaves the box empty instead of failing the report.
/// All in one class because <see cref="HcmReportBase.CompanyLogoLoader"/> is static: xUnit runs one class's tests
/// one after another.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CompanyLogoReports_Tests : AppTestBase
{
    /// <summary>
    /// Every HCMv3 report whose legacy layout binds a picture box to the company logo (42 legacy layouts; the 7
    /// not listed here were never ported: rptPayslipEn_Old, srptSalaryCertificateLogo (an unused subreport),
    /// rptTravelDeskReport, rptAnnualLeaveRequest and the three EmployeeInfo request forms).
    /// </summary>
    public static readonly IReadOnlyDictionary<Type, CompanyLogoSource> LogoReports = new Dictionary<Type, CompanyLogoSource>
    {
        [typeof(rptBankTransfer)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptBankTransferBySponsor)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptBankAndCashTransfer)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptBankAndCashTransferBySponsor)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptFinalSettlement)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptFinalSettlementAr)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptSettlementAcknowledgement_DischargeForm)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptAnnualLeaveSheet)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmployeeConfirmationLetter)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmployeeWarningNotice)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmploymentContract)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptFinalClearance_EOS)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptMonthlyIndemnityAccrual)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptMonthlyLeaveAccrual)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptSalaryContinuousCertificate)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptSalaryIncrementLetter)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptSickLeaveMedicalForm)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptLeaveEncashment)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptLeaveSettlement)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptLeaveSettlementAr)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptPayrollSheetBySponsor)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptPayslipEn)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptSalaryByBudgetingGroup)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptIndemnityBreakdown)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptLeaveIndemnityStatement)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptHireDeparture)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmployeeAttendance)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmployeeOvertime)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmployeeDeduction)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmployeeLeave)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptEmployeeLoan)] = CompanyLogoSource.EmployeeCompany,
        [typeof(rptPayrollSheet)] = CompanyLogoSource.FirstLevel1Unit,
        [typeof(rptPayslipAr)] = CompanyLogoSource.FirstCompanyUnit,
        [typeof(rptSubordPayslipAr)] = CompanyLogoSource.FirstCompanyUnit,
        [typeof(rptSubordPayslipEn)] = CompanyLogoSource.FirstCompanyUnit
    };

    /// <summary>
    /// Legacy commented out <c>B.Companylogo</c> in this report's final SELECT, so its picture box never had a value
    /// to show. Kept that way: the box is wired, but the SQL deliberately does not return the org unit.
    /// </summary>
    private static readonly Type[] ReportsWhereLegacyPrintedNoLogo = { typeof(rptBankAndCashTransferBySponsor) };

    private readonly ICompanyLogoResolver _resolver;
    private readonly IEntityImageStore _images;

    public CompanyLogoReports_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _resolver = Resolve<ICompanyLogoResolver>();
        _images = Resolve<IEntityImageStore>();
        HcmReportBase.CompanyLogoLoader = null;
    }

    public override void Dispose()
    {
        HcmReportBase.CompanyLogoLoader = null;
        base.Dispose();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    public static IEnumerable<object[]> LogoReportCases() => LogoReports.Select(r => new object[] { r.Key.FullName, r.Value });

    [Theory]
    [MemberData(nameof(LogoReportCases))]
    public void Should_Wire_The_Company_Logo_Of_Every_Report_That_Printed_One_In_Legacy(string reportTypeName, CompanyLogoSource expectedSource)
    {
        var reportType = LogoReports.Keys.Single(t => t.FullName == reportTypeName);
        using var report = (HcmReportBase)Activator.CreateInstance(reportType)!;

        report.CompanyLogoBindings.Count.ShouldBe(1, "picture boxes: " + DescribePictureBoxes(report));
        var binding = report.CompanyLogoBindings.Single();
        binding.Source.ShouldBe(expectedSource);
        FindPictureBox(report, binding.PictureBoxName).ShouldNotBeNull();

        if (expectedSource != CompanyLogoSource.EmployeeCompany)
        {
            binding.OrgUnitIdColumn.ShouldBeNull();
            return;
        }

        binding.OrgUnitIdColumn.ShouldEndWith(HcmReportBase.LogoOrgUnitIdSuffix);
        var sql = DataMemberSql(report);
        if (ReportsWhereLegacyPrintedNoLogo.Contains(reportType))
        {
            sql.ShouldNotContain(binding.OrgUnitIdColumn);
        }
        else
        {
            // Case-sensitive on purpose: the column is named after the layout's own binding ([Companylogo] vs
            // [CompanyLogo]), so the row lookup cannot depend on how DevExpress compares names.
            sql.ShouldContain(binding.OrgUnitIdColumn, Case.Sensitive);
        }
    }

    [Fact]
    public void Should_Leave_No_Report_With_An_Unwired_Logo_Binding()
    {
        var constructed = new List<Type>();
        var withLogo = new List<Type>();
        var unwired = new List<string>();

        var reportTypes = typeof(HcmReportBase).Assembly.GetTypes()
            .Where(t => typeof(HcmReportBase).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var reportType in reportTypes)
        {
            HcmReportBase report;
            try
            {
                report = (HcmReportBase)Activator.CreateInstance(reportType)!;
            }
            catch (Exception)
            {
                continue; // Constructing unrelated reports is not what this test checks; the listed ones are below.
            }

            using (report)
            {
                constructed.Add(reportType);
                if (report.CompanyLogoBindings.Count > 0)
                {
                    withLogo.Add(reportType);
                }

                foreach (var box in AllControls(report).OfType<XRPictureBox>())
                {
                    var stillBound =
                        box.ExpressionBindings.Cast<ExpressionBinding>().Any(b =>
                            (b.PropertyName == "Image" || b.PropertyName == "ImageSource") && (b.Expression ?? "").TrimEnd(']').EndsWith("logo", StringComparison.OrdinalIgnoreCase))
                        || box.DataBindings.Cast<XRBinding>().Any(b =>
                            (b.PropertyName == "Image" || b.PropertyName == "ImageSource") && (b.DataMember ?? "").EndsWith("logo", StringComparison.OrdinalIgnoreCase));
                    if (stillBound)
                    {
                        unwired.Add(reportType.Name + "." + box.Name);
                    }
                }
            }
        }

        constructed.ShouldContain(t => LogoReports.ContainsKey(t));
        LogoReports.Keys.Where(t => !constructed.Contains(t)).ShouldBeEmpty();
        unwired.ShouldBeEmpty();
        withLogo.OrderBy(t => t.FullName).ShouldBe(LogoReports.Keys.OrderBy(t => t.FullName));
    }

    [Fact]
    public async Task Should_Print_The_Employees_Company_Logo_From_The_File_Store()
    {
        var logo = Png();
        var path = await _images.ReplaceAsync(EntityImageKinds.OrgUnitLogo, TenantId, 5151, null, logo);
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var department = CreateUnitType("Department");
        CreateUnit("80001", company, path);
        var employeeUnit = CreateUnit("80001.00001", department);

        var calls = new List<(int TenantId, CompanyLogoSource Source, long? UnitId)>();
        HcmReportBase.CompanyLogoLoader = (tenantId, source, unitId) =>
        {
            calls.Add((tenantId, source, unitId));
            return AsyncHelper.RunSync(() => _resolver.ReadEmployeeCompanyLogoAsync(tenantId, unitId));
        };

        using var report = RowReport(new LogoRow { CompanylogoOrgUnitId = employeeUnit });
        report.CreateDocument();

        calls.ShouldContain((TenantId, CompanyLogoSource.EmployeeCompany, (long?)employeeUnit));
        var box = FindPictureBox(report, report.CompanyLogoBindings.Single().PictureBoxName);
        box.Image.ShouldNotBeNull();
        box.Image.Width.ShouldBe(3);
    }

    [Fact]
    public void Should_Print_The_Report_Wide_Logo_For_A_Layout_Bound_To_Another_Query()
    {
        var calls = new List<(int TenantId, CompanyLogoSource Source, long? UnitId)>();
        HcmReportBase.CompanyLogoLoader = (tenantId, source, unitId) =>
        {
            calls.Add((tenantId, source, unitId));
            return Png();
        };

        using var report = new rptPayrollSheet { RequestParameters = false };
        report.ApplyTenant(TenantId);
        report.DataSource = new List<LogoRow> { new() };
        report.DataMember = string.Empty;
        report.CreateDocument();

        calls.ShouldContain((TenantId, CompanyLogoSource.FirstLevel1Unit, (long?)null));
        FindPictureBox(report, report.CompanyLogoBindings.Single().PictureBoxName).Image.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Leave_The_Box_Empty_When_There_Is_No_Logo()
    {
        HcmReportBase.CompanyLogoLoader = (_, _, _) => null;

        using var report = RowReport(new LogoRow { CompanylogoOrgUnitId = 12345 });
        Should.NotThrow(() => report.CreateDocument());

        FindPictureBox(report, report.CompanyLogoBindings.Single().PictureBoxName).Image.ShouldBeNull();
    }

    [Fact]
    public void Should_Not_Ask_For_A_Logo_When_The_Employee_Has_No_Unit()
    {
        var called = false;
        HcmReportBase.CompanyLogoLoader = (_, _, _) =>
        {
            called = true;
            return Png();
        };

        using var report = RowReport(new LogoRow { CompanylogoOrgUnitId = null });
        report.CreateDocument();

        called.ShouldBeFalse();
        FindPictureBox(report, report.CompanyLogoBindings.Single().PictureBoxName).Image.ShouldBeNull();
    }

    [Fact]
    public void Should_Print_The_Report_When_The_Logo_Cannot_Be_Loaded_Or_Drawn()
    {
        HcmReportBase.CompanyLogoLoader = (_, _, _) => throw new InvalidOperationException("file store offline");
        using (var failing = RowReport(new LogoRow { CompanylogoOrgUnitId = 1 }))
        {
            Should.NotThrow(() => failing.CreateDocument());
            FindPictureBox(failing, failing.CompanyLogoBindings.Single().PictureBoxName).Image.ShouldBeNull();
        }

        HcmReportBase.CompanyLogoLoader = (_, _, _) => new byte[] { 1, 2, 3 }; // not an image
        using var corrupt = RowReport(new LogoRow { CompanylogoOrgUnitId = 1 });
        Should.NotThrow(() => corrupt.CreateDocument());
        FindPictureBox(corrupt, corrupt.CompanyLogoBindings.Single().PictureBoxName).Image.ShouldBeNull();
    }

    [Fact]
    public void Should_Leave_The_Box_Empty_When_No_Loader_Is_Installed()
    {
        HcmReportBase.CompanyLogoLoader = null;

        using var report = RowReport(new LogoRow { CompanylogoOrgUnitId = 1 });
        Should.NotThrow(() => report.CreateDocument());

        FindPictureBox(report, report.CompanyLogoBindings.Single().PictureBoxName).Image.ShouldBeNull();
    }

    /// <summary>
    /// rptEmployeeConfirmationLetter fed from memory instead of SQL Server (the test database is SQLite, which
    /// cannot run the report's T-SQL). The row carries just the column the logo needs.
    /// </summary>
    private rptEmployeeConfirmationLetter RowReport(LogoRow row)
    {
        var report = new rptEmployeeConfirmationLetter { RequestParameters = false };
        report.ApplyTenant(TenantId);
        report.DataSource = new List<LogoRow> { row };
        report.DataMember = string.Empty;
        return report;
    }

    public class LogoRow
    {
        public long? CompanylogoOrgUnitId { get; set; }
    }

    private static byte[] Png()
    {
        using var bitmap = new SKBitmap(3, 2);
        bitmap.Erase(SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private int CreateUnitType(string nameEn)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var type = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = TenantId,
                Code = OrganizationUnitType.Level1Code,
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
            context.SaveChanges();
            id = type.Id;
        });

        return id;
    }

    private long CreateUnit(string code, int unitTypeId, string logoPath = null)
    {
        long id = 0;
        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, "OU " + code) { Code = code }).Entity;
            context.SaveChanges();
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId,
                OrganizationUnitId = unit.Id,
                Code = "P" + unit.Id,
                NameAr = "OU " + code,
                OrganizationUnitTypeId = unitTypeId,
                LogoPath = logoPath
            });
            context.SaveChanges();
            id = unit.Id;
        });

        return id;
    }

    private static string DataMemberSql(XtraReport report)
    {
        var dataSource = report.DataSource.ShouldBeOfType<SqlDataSource>();
        var query = dataSource.Queries.Cast<SqlQuery>().Single(q => q.Name == report.DataMember);
        return query.ShouldBeOfType<CustomSqlQuery>().Sql;
    }

    private static string DescribePictureBoxes(XtraReport report) => string.Join("; ",
        AllControls(report).OfType<XRPictureBox>().Select(p =>
            p.Name + " [" + string.Join(",", p.ExpressionBindings.Cast<ExpressionBinding>().Select(b => b.PropertyName + "=" + b.Expression))
            + "|" + string.Join(",", p.DataBindings.Cast<XRBinding>().Select(b => b.PropertyName + "=" + b.DataMember)) + "]"));

    private static XRPictureBox FindPictureBox(XtraReport report, string name) =>
        AllControls(report).OfType<XRPictureBox>().Single(p => p.Name == name);

    private static IEnumerable<XRControl> AllControls(XtraReport report)
    {
        foreach (Band band in report.Bands)
        {
            foreach (var control in Descend(band))
            {
                yield return control;
            }
        }
    }

    private static IEnumerable<XRControl> Descend(XRControl root)
    {
        foreach (XRControl child in root.Controls)
        {
            yield return child;
            foreach (var grandChild in Descend(child))
            {
                yield return grandChild;
            }
        }
    }
}
