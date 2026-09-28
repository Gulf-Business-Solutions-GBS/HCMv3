using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Tests for <see cref="EmployeeBasicPayImportAppService"/> and the per-row domain service behind it -- the port
/// of legacy <c>ImportExcelController</c>'s <c>EmployeeBasicPay</c> branch, including
/// <c>ManagePayrollSettings.CarryAllowanceOnSalaryImport</c>. Each test builds a small workbook in memory.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeBasicPayImportAppService_Tests : AppTestBase
{
    private static readonly string[] Headers =
    {
        "Employee Code", "EffectiveDate", "PayType", "PayInterval", "WageType", "Amount", "Currency"
    };

    private static readonly DateTime Infinite = new DateTime(9999, 12, 31);

    private readonly IEmployeeBasicPayImportAppService _appService;
    private readonly string _suffix = Guid.NewGuid().ToString("N").Substring(0, 6);

    public EmployeeBasicPayImportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBasicPayImportAppService>();
    }

    private int TenantId => AbpSession.TenantId.Value;

    #region << Import >>

    [Fact]
    public async Task Should_Create_Header_And_Detail_For_A_New_Effective_Date()
    {
        //Arrange
        var employee = CreateEmployee("BPI100");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");

        //Act
        var output = await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 1500.5, kwd.NameEn));

        //Assert
        output.TotalRecords.ShouldBe(1);
        output.SuccessfulRecords.ShouldBe(1);
        output.ErrorRecords.ShouldBe(0);
        output.Status.ShouldBe("Data has been imported successfully.");

        var header = GetBasicPays(employee.Id).ShouldHaveSingleItem();
        header.TenantId.ShouldBe(TenantId);
        header.StartDate.ShouldBe(new DateTime(2024, 1, 1));
        header.EndDate.ShouldBe(Infinite);
        header.PayType.ShouldBe(PayType.Regular);
        header.PayInterval.ShouldBe(PayInterval.Monthly);
        header.Amount.ShouldBe(1500.5);

        var detail = GetDetails(header.Id).ShouldHaveSingleItem();
        detail.TenantId.ShouldBe(TenantId);
        detail.WageTypeId.ShouldBe(basic.Id);
        detail.CurrencyId.ShouldBe(kwd.Id);
        detail.Amount.ShouldBe(1500.5);
    }

    [Fact]
    public async Task Should_Add_Line_To_Existing_Header_And_Keep_Its_PayType_And_PayInterval()
    {
        //Arrange
        var employee = CreateEmployee("BPI110");
        var basic = CreateWageType("1000", "Basic");
        var housing = CreateWageType("1001", "Housing");
        var kwd = CreateCurrency("KWD");
        var headerId = SeedBasicPay(employee.Id, new DateTime(2024, 1, 1), Infinite, PayType.None, PayInterval.Daily,
            (basic.Id, 1000, kwd.Id));

        //Act
        var output = await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", housing.NameEn, 250, kwd.NameEn));

        //Assert
        output.SuccessfulRecords.ShouldBe(1);
        var header = GetBasicPays(employee.Id).ShouldHaveSingleItem();
        header.Id.ShouldBe(headerId);
        header.PayType.ShouldBe(PayType.None);
        header.PayInterval.ShouldBe(PayInterval.Daily);
        header.Amount.ShouldBe(1250);
        GetDetails(headerId).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Count_Existing_WageType_As_Success_Without_Changing_The_Amount()
    {
        //Arrange
        var employee = CreateEmployee("BPI120");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");
        var headerId = SeedBasicPay(employee.Id, new DateTime(2024, 1, 1), Infinite, PayType.Regular, PayInterval.Monthly,
            (basic.Id, 1000, kwd.Id));

        //Act
        var output = await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 9999, kwd.NameEn));

        //Assert
        output.SuccessfulRecords.ShouldBe(1);
        output.ErrorRecords.ShouldBe(0);
        GetDetails(headerId).ShouldHaveSingleItem().Amount.ShouldBe(1000);
        GetBasicPays(employee.Id).ShouldHaveSingleItem().Amount.ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Report_Unknown_Employee_WageType_And_Currency_In_One_Line()
    {
        //Arrange
        CreateWageType("1000", "Basic");
        CreateCurrency("KWD");
        var unknownEmployee = "NOPE" + _suffix;

        //Act
        var output = await ImportAsync(Row(unknownEmployee, new DateTime(2024, 1, 1), "Regular", "Monthly", "NoSuchWage", 100, "NoSuchCurrency"));

        //Assert
        output.TotalRecords.ShouldBe(1);
        output.SuccessfulRecords.ShouldBe(0);
        output.ErrorRecords.ShouldBe(1);
        output.Errors.ShouldHaveSingleItem().ShouldBe(
            $"Excel RowNo : 2 - Employee Code : '{unknownEmployee}' Not exists., " +
            "WageType : 'NoSuchWage' Not exists., Currency : 'NoSuchCurrency' Not exists.");
        output.Status.ShouldBe(output.Errors[0]);
    }

    [Fact]
    public async Task Should_Report_Invalid_Date_PayType_And_PayInterval()
    {
        //Arrange
        var employee = CreateEmployee("BPI130");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");

        //Act
        var output = await ImportAsync(Row(employee.Code, "2024/03/15", "Bogus", "9", basic.NameEn, 100, kwd.NameEn));

        //Assert
        output.Errors.ShouldHaveSingleItem().ShouldBe(
            "Excel RowNo : 2 - EffectiveDate is not valid., PayType : 'Bogus' Not exists., PayInterval : '9' Not exists.");
        GetBasicPays(employee.Id).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Report_Blank_EffectiveDate()
    {
        //Arrange
        var employee = CreateEmployee("BPI135");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");

        //Act
        var output = await ImportAsync(Row(employee.Code, null, "Regular", "Monthly", basic.NameEn, 100, kwd.NameEn));

        //Assert
        output.Errors.ShouldHaveSingleItem().ShouldBe("Excel RowNo : 2 - EffectiveDate is blank.");
    }

    [Fact]
    public async Task Should_Accept_Text_Date_Numeric_PayType_And_Legacy_Yealy_Spelling()
    {
        //Arrange
        var employee = CreateEmployee("BPI140");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");

        //Act
        var output = await ImportAsync(Row(employee.Code, "15-03-2024", "1", "Yealy", basic.NameEn, "750", kwd.NameEn));

        //Assert
        output.SuccessfulRecords.ShouldBe(1);
        var header = GetBasicPays(employee.Id).ShouldHaveSingleItem();
        header.StartDate.ShouldBe(new DateTime(2024, 3, 15));
        header.PayType.ShouldBe(PayType.Regular);
        header.PayInterval.ShouldBe(PayInterval.Yearly);
        header.Amount.ShouldBe(750);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(null)]
    public async Task Should_Fail_Zero_Or_Blank_Amount_And_Write_Nothing(double? amount)
    {
        //Arrange
        var employee = CreateEmployee("BPI150");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");

        //Act
        var output = await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, amount, kwd.NameEn));

        //Assert
        output.ErrorRecords.ShouldBe(1);
        output.Errors.ShouldHaveSingleItem().ShouldBe("Excel RowNo : 2 - Amount cannot be blank Or Zero.");
        // Legacy rolled the new header back; here the refusal comes before anything is written.
        GetBasicPays(employee.Id).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Skip_Blank_Employee_Code_Counting_It_In_Total_Only()
    {
        //Arrange
        var employee = CreateEmployee("BPI160");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");

        //Act
        var output = await ImportAsync(
            Row("", new DateTime(2024, 1, 1), "Regular", "Monthly", "whatever", 0, "whatever"),
            Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 100, kwd.NameEn));

        //Assert
        output.TotalRecords.ShouldBe(2);
        output.SuccessfulRecords.ShouldBe(1);
        output.ErrorRecords.ShouldBe(0);
        output.Errors.ShouldBeEmpty();
        output.Status.ShouldBe("Employee Basic Pay has been imported successfully.");
    }

    [Fact]
    public async Task Should_Fail_Every_Row_When_A_Column_Is_Missing()
    {
        //Arrange
        var employee = CreateEmployee("BPI170");
        var basic = CreateWageType("1000", "Basic");
        var headersWithoutCurrency = Headers.Where(h => h != "Currency").ToArray();

        //Act
        var output = await ImportAsync(headersWithoutCurrency,
            new object[] { employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 100 },
            new object[] { "", new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 100 });

        //Assert
        output.TotalRecords.ShouldBe(2);
        output.ErrorRecords.ShouldBe(1);
        output.Errors.ShouldHaveSingleItem().ShouldBe("Excel RowNo : 2 - Column 'Currency' does not belong to table.");
        GetBasicPays(employee.Id).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Match_Headers_Ignoring_Case_And_Spaces()
    {
        //Arrange
        var employee = CreateEmployee("BPI175");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");
        var headers = Headers.Select(h => "  " + h.ToUpperInvariant() + " ").ToArray();

        //Act
        var output = await ImportAsync(headers,
            new object[] { employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 100, kwd.NameEn });

        //Assert
        output.SuccessfulRecords.ShouldBe(1);
    }

    #endregion

    #region << CarryAllowanceOnSalaryImport >>

    [Fact]
    public async Task Should_Carry_Allowances_From_Previous_Record_On_Basic_Salary_When_Setting_Is_On()
    {
        //Arrange
        SetCarryAllowanceOnSalaryImport(true);
        var employee = CreateEmployee("BPI200");
        var basic = CreateWageType("1000", "Basic");
        var housing = CreateWageType("1001", "Housing");
        var transport = CreateWageType("1002", "Transport");
        var kwd = CreateCurrency("KWD");
        var usd = CreateCurrency("USD");
        SeedBasicPay(employee.Id, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31), PayType.Regular, PayInterval.Monthly,
            (basic.Id, 400, kwd.Id), (housing.Id, 999, kwd.Id));
        SeedBasicPay(employee.Id, new DateTime(2023, 1, 1), Infinite, PayType.Regular, PayInterval.Monthly,
            (basic.Id, 500, kwd.Id), (housing.Id, 200, kwd.Id), (transport.Id, 50, usd.Id));

        //Act
        var output = await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 600, kwd.NameEn));

        //Assert
        output.SuccessfulRecords.ShouldBe(1);
        var header = GetBasicPays(employee.Id).Single(p => p.StartDate == new DateTime(2024, 1, 1));
        var lines = GetDetails(header.Id);
        lines.Count.ShouldBe(3);
        lines.Single(l => l.WageTypeId == basic.Id).Amount.ShouldBe(600);
        lines.Single(l => l.WageTypeId == housing.Id).Amount.ShouldBe(200);
        var carriedTransport = lines.Single(l => l.WageTypeId == transport.Id);
        carriedTransport.Amount.ShouldBe(50);
        carriedTransport.CurrencyId.ShouldBe(usd.Id);
        header.Amount.ShouldBe(850);
    }

    [Fact]
    public async Task Should_Not_Carry_Allowances_When_Setting_Is_Off()
    {
        //Arrange
        SetCarryAllowanceOnSalaryImport(false);
        var employee = CreateEmployee("BPI210");
        var basic = CreateWageType("1000", "Basic");
        var housing = CreateWageType("1001", "Housing");
        var kwd = CreateCurrency("KWD");
        SeedBasicPay(employee.Id, new DateTime(2023, 1, 1), Infinite, PayType.Regular, PayInterval.Monthly,
            (basic.Id, 500, kwd.Id), (housing.Id, 200, kwd.Id));

        //Act
        await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 600, kwd.NameEn));

        //Assert
        var header = GetBasicPays(employee.Id).Single(p => p.StartDate == new DateTime(2024, 1, 1));
        GetDetails(header.Id).ShouldHaveSingleItem().WageTypeId.ShouldBe(basic.Id);
        header.Amount.ShouldBe(600);
    }

    [Fact]
    public async Task Should_Not_Carry_Allowances_On_A_Wage_Type_Other_Than_1000()
    {
        //Arrange
        SetCarryAllowanceOnSalaryImport(true);
        var employee = CreateEmployee("BPI220");
        var basic = CreateWageType("1000", "Basic");
        var housing = CreateWageType("1001", "Housing");
        var kwd = CreateCurrency("KWD");
        SeedBasicPay(employee.Id, new DateTime(2023, 1, 1), Infinite, PayType.Regular, PayInterval.Monthly,
            (basic.Id, 500, kwd.Id), (housing.Id, 200, kwd.Id));

        //Act
        await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", housing.NameEn, 300, kwd.NameEn));

        //Assert
        var header = GetBasicPays(employee.Id).Single(p => p.StartDate == new DateTime(2024, 1, 1));
        GetDetails(header.Id).ShouldHaveSingleItem().WageTypeId.ShouldBe(housing.Id);
    }

    [Fact]
    public async Task Should_Not_Overwrite_An_Allowance_Already_On_The_New_Record_When_Carrying()
    {
        //Arrange -- legacy row order: an allowance row BEFORE the 1000 row keeps its Excel value; one AFTER it
        // finds the carried line already there and is skipped as a success.
        SetCarryAllowanceOnSalaryImport(true);
        var employee = CreateEmployee("BPI230");
        var basic = CreateWageType("1000", "Basic");
        var housing = CreateWageType("1001", "Housing");
        var transport = CreateWageType("1002", "Transport");
        var kwd = CreateCurrency("KWD");
        SeedBasicPay(employee.Id, new DateTime(2023, 1, 1), Infinite, PayType.Regular, PayInterval.Monthly,
            (basic.Id, 500, kwd.Id), (housing.Id, 200, kwd.Id), (transport.Id, 50, kwd.Id));

        //Act
        var output = await ImportAsync(
            Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", housing.NameEn, 300, kwd.NameEn),
            Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 600, kwd.NameEn),
            Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", transport.NameEn, 75, kwd.NameEn));

        //Assert
        output.SuccessfulRecords.ShouldBe(3);
        var header = GetBasicPays(employee.Id).Single(p => p.StartDate == new DateTime(2024, 1, 1));
        var lines = GetDetails(header.Id);
        lines.Count.ShouldBe(3);
        lines.Single(l => l.WageTypeId == housing.Id).Amount.ShouldBe(300);
        lines.Single(l => l.WageTypeId == transport.Id).Amount.ShouldBe(50);
        header.Amount.ShouldBe(950);
    }

    [Fact]
    public async Task Should_Fail_The_Row_When_A_Carried_Line_Has_Zero_Amount()
    {
        //Arrange -- legacy's detail save refuses a zero amount on the copied line too, failing the whole row.
        SetCarryAllowanceOnSalaryImport(true);
        var employee = CreateEmployee("BPI240");
        var basic = CreateWageType("1000", "Basic");
        var housing = CreateWageType("1001", "Housing");
        var kwd = CreateCurrency("KWD");
        SeedBasicPay(employee.Id, new DateTime(2023, 1, 1), Infinite, PayType.Regular, PayInterval.Monthly,
            (basic.Id, 500, kwd.Id), (housing.Id, 0, kwd.Id));

        //Act
        var output = await ImportAsync(Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 600, kwd.NameEn));

        //Assert
        output.Errors.ShouldHaveSingleItem().ShouldBe("Excel RowNo : 2 - Amount cannot be blank Or Zero.");
        GetBasicPays(employee.Id).ShouldHaveSingleItem().StartDate.ShouldBe(new DateTime(2023, 1, 1));
    }

    #endregion

    #region << End dates and row isolation >>

    [Fact]
    public async Task Should_Rechain_End_Dates_Of_All_The_Employees_Basic_Pays()
    {
        //Arrange
        var employee = CreateEmployee("BPI300");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");
        SeedBasicPay(employee.Id, new DateTime(2023, 1, 1), Infinite, PayType.Regular, PayInterval.Monthly,
            (basic.Id, 500, kwd.Id));

        //Act -- one record after the existing one, one back-dated before it.
        var output = await ImportAsync(
            Row(employee.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 600, kwd.NameEn),
            Row(employee.Code, new DateTime(2022, 6, 1), "Regular", "Monthly", basic.NameEn, 400, kwd.NameEn));

        //Assert
        output.SuccessfulRecords.ShouldBe(2);
        var records = GetBasicPays(employee.Id).OrderBy(p => p.StartDate).ToList();
        records.Select(p => p.StartDate).ShouldBe(new[] { new DateTime(2022, 6, 1), new DateTime(2023, 1, 1), new DateTime(2024, 1, 1) });
        records[0].EndDate.ShouldBe(new DateTime(2022, 12, 31));
        records[1].EndDate.ShouldBe(new DateTime(2023, 12, 31));
        records[2].EndDate.ShouldBe(Infinite);
    }

    [Fact]
    public async Task Should_Roll_Back_Only_The_Failing_Row()
    {
        //Arrange
        var first = CreateEmployee("BPI310");
        var failing = CreateEmployee("BPI311");
        var last = CreateEmployee("BPI312");
        var basic = CreateWageType("1000", "Basic");
        var kwd = CreateCurrency("KWD");

        //Act
        var output = await ImportAsync(
            Row(first.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 100, kwd.NameEn),
            Row(failing.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 0, kwd.NameEn),
            Row(last.Code, new DateTime(2024, 1, 1), "Regular", "Monthly", basic.NameEn, 300, kwd.NameEn));

        //Assert
        output.TotalRecords.ShouldBe(3);
        output.SuccessfulRecords.ShouldBe(2);
        output.ErrorRecords.ShouldBe(1);
        output.Errors.ShouldHaveSingleItem().ShouldStartWith("Excel RowNo : 3 - ");
        GetBasicPays(first.Id).ShouldHaveSingleItem().Amount.ShouldBe(100);
        GetBasicPays(failing.Id).ShouldBeEmpty();
        GetBasicPays(last.Id).ShouldHaveSingleItem().Amount.ShouldBe(300);
    }

    #endregion

    #region << Helpers >>

    private static object[] Row(string code, object effectiveDate, string payType, string payInterval, string wageType, object amount, string currency)
    {
        return new[] { code, effectiveDate, payType, payInterval, wageType, amount, currency };
    }

    private Task<ImportEmployeeBasicPaysOutput> ImportAsync(params object[][] rows)
    {
        return ImportAsync(Headers, rows);
    }

    private Task<ImportEmployeeBasicPaysOutput> ImportAsync(string[] headers, params object[][] rows)
    {
        return _appService.ImportEmployeeBasicPays(new ImportEmployeeBasicPaysInput
        {
            FileName = "basic-pay.xlsx",
            FileBase64 = BuildWorkbookBase64(headers, rows)
        });
    }

    private static string BuildWorkbookBase64(string[] headers, object[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");

        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var cell = sheet.Cell(r + 2, c + 1);
                switch (rows[r][c])
                {
                    case null:
                        break;
                    case DateTime date:
                        cell.Value = date;
                        break;
                    case double number:
                        cell.Value = number;
                        break;
                    case int number:
                        cell.Value = (double)number;
                        break;
                    case string text:
                        cell.Value = text;
                        break;
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    /// <summary>The seeded database may already hold the tenant's settings row, so update it rather than add one.</summary>
    private void SetCarryAllowanceOnSalaryImport(bool enabled)
    {
        UsingDbContext(context =>
        {
            var settings = context.ManagePayrollSettingses.FirstOrDefault(s => s.TenantId == TenantId);
            if (settings == null)
            {
                settings = new ManagePayrollSettings { TenantId = TenantId };
                context.ManagePayrollSettingses.Add(settings);
            }

            settings.CarryAllowanceOnSalaryImport = enabled;
        });
    }

    private Employee CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            employee = context.Employees.Add(new Employee
            {
                TenantId = TenantId,
                Code = code + _suffix,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private WageType CreateWageType(string code, string name)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.WageTypes.Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = name + "-" + _suffix,
                NameAr = name
            }).Entity;
        });
        return wageType;
    }

    private Currency CreateCurrency(string name)
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Currencies.Add(new Currency
            {
                TenantId = TenantId,
                Code = name + _suffix,
                NameEn = name + "-" + _suffix,
                NameAr = name
            }).Entity;
        });
        return currency;
    }

    private int SeedBasicPay(int employeeId, DateTime start, DateTime end, PayType payType, PayInterval payInterval,
        params (int WageTypeId, double Amount, int CurrencyId)[] lines)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var header = context.EmployeeBasicPays.Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = start,
                EndDate = end,
                PayType = payType,
                PayInterval = payInterval,
                Amount = lines.Sum(l => l.Amount)
            }).Entity;
            context.SaveChanges();

            foreach (var line in lines)
            {
                context.EmployeeBasicPayDetails.Add(new EmployeeBasicPayDetail
                {
                    TenantId = TenantId,
                    EmployeeBasicPayId = header.Id,
                    WageTypeId = line.WageTypeId,
                    Amount = line.Amount,
                    CurrencyId = line.CurrencyId
                });
            }

            context.SaveChanges();
            id = header.Id;
        });
        return id;
    }

    private List<EmployeeBasicPay> GetBasicPays(int employeeId)
    {
        return UsingDbContext(context => context.EmployeeBasicPays.Where(p => p.EmployeeId == employeeId).ToList());
    }

    private List<EmployeeBasicPayDetail> GetDetails(int basicPayId)
    {
        return UsingDbContext(context => context.EmployeeBasicPayDetails.Where(d => d.EmployeeBasicPayId == basicPayId).ToList());
    }

    #endregion
}
