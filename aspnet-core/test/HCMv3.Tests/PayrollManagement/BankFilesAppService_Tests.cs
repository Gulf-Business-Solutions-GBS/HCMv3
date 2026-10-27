using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Organizations;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;
using PayrollBankFilesAppService = HCMv3.PayrollManagement.IBankFilesAppService;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The Bank File screen brought up to the LIVE legacy screen (<c>uscBankFile.ascx.cs</c>): period Current / Other
/// worked out on the server, the ALL / BANK / CASH payment type, the sponsor's own accounts, the run log with a
/// stored copy of every file, the salary-register lock, and the totals shown for text layouts.
///
/// <para>Each test builds its own small payroll on the in-memory database: one payroll area paid on the calendar
/// month, its control record open on August 2026, and employees with a net pay (wage type 9000) register.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class BankFilesAppService_Tests : AppTestBase
{
    private const int TenantId = 1;
    private static readonly DateTime MonthStart = new(2026, 8, 1);
    private static readonly DateTime MonthEnd = new(2026, 8, 31);
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly PayrollBankFilesAppService _service;
    private readonly int _wageType9000Id;
    private readonly int _payrollAreaId;
    private readonly int _nbkId;
    private int _seq;

    public BankFilesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<PayrollBankFilesAppService>();

        _wageType9000Id = UsingDbContext(c =>
        {
            var wt = c.Set<WageType>().Add(new WageType { TenantId = TenantId, Code = "9000", NameEn = "Net Pay", NameAr = "Net Pay" }).Entity;
            c.SaveChanges();
            return wt.Id;
        });

        _payrollAreaId = CreatePayrollArea("Main", "Jan-Dec");
        CreateControlRecord(_payrollAreaId, 8, 2026, MonthStart, MonthEnd);
        _nbkId = CreateBank("NBK", "National Bank of Kuwait");
    }

    // ================================================================== 1. period Current / Other

    [Fact]
    public async Task Should_Take_The_Current_Period_From_The_Payroll_Areas_Control_Record()
    {
        var period = await _service.GetBankFilePeriod(new GetBankFilePeriodInput
        {
            PayrollAreaId = _payrollAreaId,
            PeriodType = BankFilePeriodType.Current
        });

        period.HasPeriod.ShouldBeTrue();
        period.PayrollMonth.ShouldBe(8);
        period.PayrollYear.ShouldBe(2026);
        period.StartDateText.ShouldBe("01/08/2026");
        period.EndDateText.ShouldBe("31/08/2026");
    }

    [Fact]
    public async Task Should_Keep_A_Current_Period_That_Does_Not_Run_On_Calendar_Months()
    {
        // A payroll area paid 26th to 25th: the control record's own dates, not a calendar month.
        var areaId = CreatePayrollArea("Shifted", "Jan-Dec");
        CreateControlRecord(areaId, 9, 2026, new DateTime(2026, 8, 26), new DateTime(2026, 9, 25));

        var period = await _service.GetBankFilePeriod(new GetBankFilePeriodInput
        {
            PayrollAreaId = areaId,
            PeriodType = BankFilePeriodType.Current
        });

        period.StartDateText.ShouldBe("26/08/2026");
        period.EndDateText.ShouldBe("25/09/2026");
    }

    [Fact]
    public async Task Should_Say_Why_There_Is_No_Current_Period_Without_A_Payroll_Area()
    {
        var period = await _service.GetBankFilePeriod(new GetBankFilePeriodInput { PeriodType = BankFilePeriodType.Current });

        period.HasPeriod.ShouldBeFalse();
        period.Message.ShouldNotBeNullOrWhiteSpace();
        period.EndDateText.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Say_Why_There_Is_No_Current_Period_Without_A_Control_Record()
    {
        var areaId = CreatePayrollArea("NoRecord", "Jan-Dec");

        var period = await _service.GetBankFilePeriod(new GetBankFilePeriodInput
        {
            PayrollAreaId = areaId,
            PeriodType = BankFilePeriodType.Current
        });

        period.HasPeriod.ShouldBeFalse();
        period.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Map_An_Other_Payroll_Month_Through_A_Fiscal_Payroll_Period()
    {
        // Apr-Mar: payroll month 1 is April (legacy GetStartEndDateFromPayrollPeriod).
        var areaId = CreatePayrollArea("Fiscal", "Apr-Mar");

        var period = await _service.GetBankFilePeriod(new GetBankFilePeriodInput
        {
            PayrollAreaId = areaId,
            PeriodType = BankFilePeriodType.Other,
            PayrollMonth = 1,
            PayrollYear = 2026
        });

        period.HasPeriod.ShouldBeTrue();
        period.PayrollMonth.ShouldBe(1);
        period.StartDateText.ShouldBe("01/04/2026");
        period.EndDateText.ShouldBe("30/04/2026");
    }

    [Fact]
    public async Task Should_Use_The_Calendar_Month_For_Other_Without_A_Payroll_Area()
    {
        var period = await _service.GetBankFilePeriod(new GetBankFilePeriodInput
        {
            PeriodType = BankFilePeriodType.Other,
            PayrollMonth = 2,
            PayrollYear = 2028
        });

        period.StartDateText.ShouldBe("01/02/2028");
        period.EndDateText.ShouldBe("29/02/2028");
    }

    [Fact]
    public async Task Should_Refuse_An_Unknown_Payroll_Period_Code()
    {
        var areaId = CreatePayrollArea("Odd", "Weekly");

        var period = await _service.GetBankFilePeriod(new GetBankFilePeriodInput
        {
            PayrollAreaId = areaId,
            PeriodType = BankFilePeriodType.Other,
            PayrollMonth = 8,
            PayrollYear = 2026
        });

        period.HasPeriod.ShouldBeFalse();
        period.Message.ShouldNotBeNullOrWhiteSpace();
    }

    // ================================================================== 8. dates cannot shift a day

    [Fact]
    public async Task Should_Ignore_A_Browser_End_Date_That_Arrived_A_Day_Early()
    {
        // Kuwait midnight on the 31st, read by a server on UTC: 21:00 on the 30th. With the period type set, the
        // server takes the end date from the control record and still finds the register dated the 31st.
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 500);

        var output = await _service.GenerateBankFile(Input(i =>
        {
            i.PeriodType = BankFilePeriodType.Current;
            i.StartDate = new DateTime(2026, 7, 31, 21, 0, 0);
            i.EndDate = new DateTime(2026, 8, 30, 21, 0, 0);
            i.Month = 1;
            i.Year = 2020;
        }));

        output.RecordCount.ShouldBe(1);
        output.EndDateText.ShouldBe("31/08/2026");
        output.StartDateText.ShouldBe("01/08/2026");
        LogRow(output.ExecutionLogId!.Value).Parameters.ShouldContain("Period : 31/08/2026");
    }

    [Fact]
    public async Task Should_Work_Out_Other_Period_Dates_From_Month_And_Year_Not_From_The_Browser()
    {
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 500);

        var output = await _service.GenerateBankFile(Input(i =>
        {
            i.PeriodType = BankFilePeriodType.Other;
            i.Month = 8;
            i.Year = 2026;
            i.EndDate = new DateTime(2026, 8, 30, 21, 0, 0);
        }));

        output.RecordCount.ShouldBe(1);
        output.EndDateText.ShouldBe("31/08/2026");
    }

    [Fact]
    public async Task Should_Still_Use_The_Given_Dates_For_A_Server_Side_Caller()
    {
        // Go-Live Setup calls without a period type and passes a date it built itself.
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 500);

        var output = await _service.GenerateBankFile(Input(i =>
        {
            i.PeriodType = null;
            i.EndDate = new DateTime(2026, 8, 31, 10, 30, 0);
        }));

        output.RecordCount.ShouldBe(1);
        output.EndDateText.ShouldBe("31/08/2026");
    }

    // ================================================================== 2. payment type ALL / BANK / CASH

    [Fact]
    public async Task Should_Take_Bank_And_Cash_Payees_For_All_But_Not_Those_Without_A_Bank_Record()
    {
        SeedPaymentTypeMix();

        var output = await _service.GenerateBankFile(Input(i => i.PaymentType = null));

        output.RecordCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Take_Only_Bank_Payees_For_Bank()
    {
        SeedPaymentTypeMix();

        var output = await _service.GenerateBankFile(Input(i => i.PaymentType = PaymentType.Bank));

        output.RecordCount.ShouldBe(1);
        output.TotalAmount.ShouldBe(100d);
    }

    [Fact]
    public async Task Should_Take_Only_Cash_Payees_For_Cash()
    {
        SeedPaymentTypeMix();

        var output = await _service.GenerateBankFile(Input(i => i.PaymentType = PaymentType.Cash));

        output.RecordCount.ShouldBe(1);
        output.TotalAmount.ShouldBe(200d);
    }

    [Fact]
    public async Task Should_Read_Na_As_All()
    {
        SeedPaymentTypeMix();

        var output = await _service.GenerateBankFile(Input(i => i.PaymentType = PaymentType.Na));

        output.RecordCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Record_The_Payment_Type_In_The_Log_Parameters()
    {
        SeedPaymentTypeMix();

        var all = await _service.GenerateBankFile(Input(i => i.PaymentType = null));
        var cash = await _service.GenerateBankFile(Input(i => i.PaymentType = PaymentType.Cash));

        LogRow(all.ExecutionLogId!.Value).Parameters.ShouldContain("Payment Type : ALL");
        LogRow(cash.ExecutionLogId!.Value).Parameters.ShouldContain("Payment Type : CASH");
    }

    // ================================================================== 7. payroll area via the org assignment

    [Fact]
    public async Task Should_Select_The_Payroll_Area_From_The_Org_Assignment()
    {
        var inArea = CreateEmployeeInArea("E001");
        CreateEmployeeBank(inArea, _nbkId, PaymentType.Bank);
        CreateRegister(inArea, PayrollRunType.Regular, 100);

        var otherArea = CreatePayrollArea("Other", "Jan-Dec");
        var elsewhere = CreateEmployee("E002");
        AssignToArea(elsewhere, otherArea);
        CreateEmployeeBank(elsewhere, _nbkId, PaymentType.Bank);
        CreateRegister(elsewhere, PayrollRunType.Regular, 999);

        var output = await _service.GenerateBankFile(Input());

        output.RecordCount.ShouldBe(1);
        output.TotalAmount.ShouldBe(100d);
    }

    [Fact]
    public async Task Should_Fall_Back_To_The_Org_Units_Payroll_Area_For_An_Assignment_Without_One()
    {
        // DIFFERENCE FROM LEGACY, through IEmployeePayrollAreaResolver: an assignment made before
        // EmployeeOrgAssignment.PayrollAreaId existed has none, and is matched through its org unit's profile.
        var unitId = CreateOrgUnit("00077", "Head Office", "Level1", "Company", payrollAreaId: _payrollAreaId);
        var employeeId = CreateEmployee("E001");
        UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = employeeId, StartDate = new DateTime(2020, 1, 1), EndDate = OpenEnded,
            OrganizationUnitId = unitId, PayrollAreaId = null
        }));
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 100);

        var output = await _service.GenerateBankFile(Input());

        output.RecordCount.ShouldBe(1);
    }

    // ================================================================== 3. sponsor bank list

    [Fact]
    public async Task Should_List_Only_The_Chosen_Sponsors_Bank_Accounts()
    {
        var sponsorA = CreateSponsor("SPA");
        var sponsorB = CreateSponsor("SPB");
        var cbkId = CreateBank("CBK", "Commercial Bank");
        CreateSponsorBank(sponsorA, _nbkId, "1111111111");
        CreateSponsorBank(sponsorA, cbkId, "2222222222");
        CreateSponsorBank(sponsorB, _nbkId, "3333333333");

        var banks = await _service.GetBankFileSponsorBanks(new EntityDto(sponsorA));

        banks.Count.ShouldBe(2);
        banks.ShouldAllBe(b => b.SponsorId == sponsorA);
        banks.Select(b => b.DisplayText).ShouldBe(new[] { "NBK (1111111111)", "CBK (2222222222)" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Name_Each_Sponsor_Account_With_Its_Sponsor_In_The_Lookups()
    {
        var sponsorA = CreateSponsor("SPA");
        CreateSponsorBank(sponsorA, _nbkId, "1111111111");

        var lookups = await _service.GetBankFileLookups();

        lookups.SponsorBanks.Single().SponsorId.ShouldBe(sponsorA);
        lookups.MosalIds.ShouldContain(0L);
    }

    // ================================================================== 4. run log + stored file

    [Fact]
    public async Task Should_Log_A_Successful_Run_With_Who_And_Every_Filter()
    {
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 500);

        var output = await _service.GenerateBankFile(Input(i => i.EmployeeCodes = "E001"));

        var log = LogRow(output.ExecutionLogId!.Value);
        log.Module.ShouldBe(ExecutionLogModule.BankFile);
        log.Action.ShouldBe(ExecutionLogAction.GENERATE);
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
        log.UserName.ShouldBe("admin");
        log.ScreenCaption.ShouldBe("Bank File");
        log.TotalRecords.ShouldBe(1);
        log.Parameters.ShouldBe(
            "Payroll Area : Main | Period : 31/08/2026 | Payment Bank : NBK-National Bank of Kuwait | " +
            "Payroll RunType : Regular | Payment Type : ALL | Period Type : Current | Employee Code : E001 | " +
            "Generate Separate Bank File(Employee Bank wise) : NO");
    }

    [Fact]
    public async Task Should_Store_A_Zipped_Copy_Of_The_File_In_The_Tenants_File_Store()
    {
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 500);

        var output = await _service.GenerateBankFile(Input());

        var attachment = UsingDbContext(c => c.Set<AppAttachment>().Single(a =>
            a.EntityType == "ExecutionLog" && a.EntityId == output.ExecutionLogId.ToString()));

        attachment.FileName.ShouldBe(Path.GetFileNameWithoutExtension(output.FileName) + ".zip");
        attachment.ContentType.ShouldBe("application/zip");
        // Bytes on disk under the tenant's own folder -- only the path is in the database.
        attachment.StoragePath.ShouldStartWith("tenant-1/");

        var stored = await Resolve<IFileStore>().ReadOrNullAsync(attachment.StoragePath);
        stored.ShouldNotBeNull();
        attachment.Size.ShouldBe(stored.LongLength);

        var entries = Unzip(stored);
        entries.Count.ShouldBe(1);
        entries.Single().Key.ShouldBe(output.FileName);
        entries.Single().Value.ShouldBe(Convert.FromBase64String(output.Base64Content));
    }

    [Fact]
    public async Task Should_Store_The_Split_Zip_As_It_Was_Downloaded()
    {
        var cbkId = CreateBank("CBK", "Commercial Bank");
        var e1 = CreateEmployeeInArea("E001");
        CreateEmployeeBank(e1, _nbkId, PaymentType.Bank);
        CreateRegister(e1, PayrollRunType.Regular, 100);
        var e2 = CreateEmployeeInArea("E002");
        CreateEmployeeBank(e2, cbkId, PaymentType.Bank);
        CreateRegister(e2, PayrollRunType.Regular, 200);

        var output = await _service.GenerateBankFile(Input(i => i.SplitBankFiles = true));

        output.ContentType.ShouldBe("application/zip");
        var attachment = UsingDbContext(c => c.Set<AppAttachment>().Single(a => a.EntityId == output.ExecutionLogId.ToString()));
        attachment.FileName.ShouldBe(output.FileName);

        var stored = await Resolve<IFileStore>().ReadOrNullAsync(attachment.StoragePath);
        stored.ShouldBe(Convert.FromBase64String(output.Base64Content));
        Unzip(stored).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Download_The_Stored_Copy_From_The_Log()
    {
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 500);
        var output = await _service.GenerateBankFile(Input());

        var file = await _service.GetBankFileLogDownload(new EntityDto(output.ExecutionLogId!.Value));

        file.FileName.ShouldEndWith(".zip");
        var bytes = Resolve<ITempFileCacheManager>().GetFile(file.FileToken);
        Unzip(bytes).Single().Value.ShouldBe(Convert.FromBase64String(output.Base64Content));
    }

    [Fact]
    public async Task Should_Refuse_To_Download_From_A_Log_Row_Without_A_File()
    {
        var logId = InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 1));

        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetBankFileLogDownload(new EntityDto(logId)));
    }

    [Fact]
    public async Task Should_Not_Hand_Out_Another_Modules_Log_Through_The_Bank_File_Screen()
    {
        var logId = InsertLog(ExecutionLogModule.PayrollEngine, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 1));

        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetBankFileLogDownload(new EntityDto(logId)));
    }

    [Fact]
    public async Task Should_Log_A_Failed_Run_With_Its_Error_And_Still_Report_The_Error()
    {
        // No salary register at all: the generate fails.
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.GenerateBankFile(Input()));

        var log = UsingDbContext(c => c.Set<ExecutionLog>().Single(l => l.Module == ExecutionLogModule.BankFile));
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
        log.UserName.ShouldBe("admin");
        log.Parameters.ShouldStartWith("Payroll Area : Main | Period : 31/08/2026");
        log.Parameters.ShouldContain(" | Error : ");
        UsingDbContext(c => c.Set<AppAttachment>().Count()).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Log_A_Run_Refused_For_Having_No_Current_Period()
    {
        var employeeId = CreateEmployee("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.GenerateBankFile(Input(i =>
        {
            i.PayrollAreaId = null;
            i.EmployeeCodes = "E001";
            i.PeriodType = BankFilePeriodType.Current;
        })));

        var log = UsingDbContext(c => c.Set<ExecutionLog>().Single());
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
        log.Parameters.ShouldContain("Period Type : Current");
    }

    [Fact]
    public async Task Should_List_The_Log_Newest_First_With_All_Successful_And_Failed_Counts()
    {
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 1, 8, 0, 0));
        var newest = InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 3, 8, 0, 0));
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.FAILED, new DateTime(2026, 9, 2, 8, 0, 0));
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.PARTIAL, new DateTime(2026, 9, 2, 9, 0, 0));
        InsertLog(ExecutionLogModule.TimeEngine, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 4, 8, 0, 0));

        var all = await _service.GetBankFileLogs(new GetBankFileLogsInput { MaxResultCount = 10 });

        all.AllCount.ShouldBe(4);
        all.SuccessfulCount.ShouldBe(2);
        // Legacy: Failed = All - Successful, so PARTIAL counts as failed.
        all.FailedCount.ShouldBe(2);
        all.TotalCount.ShouldBe(4);
        all.Items.First().Id.ShouldBe(newest);
        all.Items.Select(i => i.ActionDateTime).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public async Task Should_Filter_The_Log_By_Status()
    {
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 1));
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.FAILED, new DateTime(2026, 9, 2));

        var failed = await _service.GetBankFileLogs(new GetBankFileLogsInput { StatusFilter = BankFileLogFilter.Failed, MaxResultCount = 10 });
        var successful = await _service.GetBankFileLogs(new GetBankFileLogsInput { StatusFilter = BankFileLogFilter.Successful, MaxResultCount = 10 });

        failed.Items.Single().Succeeded.ShouldBeFalse();
        successful.Items.Single().Succeeded.ShouldBeTrue();
        // The counts on the filter buttons never depend on the filter chosen.
        failed.AllCount.ShouldBe(2);
        successful.AllCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Show_Which_Log_Rows_Have_A_Stored_File()
    {
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.Regular, 500);
        var output = await _service.GenerateBankFile(Input());
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.FAILED, new DateTime(2000, 1, 1));

        var logs = await _service.GetBankFileLogs(new GetBankFileLogsInput { MaxResultCount = 10 });

        var withFile = logs.Items.Single(i => i.Id == output.ExecutionLogId);
        withFile.HasFile.ShouldBeTrue();
        withFile.AttachmentId.ShouldNotBeNull();
        withFile.FileName.ShouldEndWith(".zip");
        logs.Items.Single(i => i.Id != output.ExecutionLogId).HasFile.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Sort_The_Log_By_A_Grid_Column()
    {
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 1), "zed");
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.FAILED, new DateTime(2026, 9, 2), "amy");

        var byUser = await _service.GetBankFileLogs(new GetBankFileLogsInput { Sorting = "userName ASC", MaxResultCount = 10 });
        var byStatus = await _service.GetBankFileLogs(new GetBankFileLogsInput { Sorting = "succeeded ASC", MaxResultCount = 10 });

        byUser.Items.Select(i => i.UserName).ShouldBe(new[] { "amy", "zed" });
        byStatus.Items.First().Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Export_The_Filtered_Log_To_Excel()
    {
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.SUCCESS, new DateTime(2026, 9, 1));
        InsertLog(ExecutionLogModule.BankFile, ExecutionLogStatus.FAILED, new DateTime(2026, 9, 2));

        var file = await _service.ExportBankFileLogs(new GetBankFileLogsInput { StatusFilter = BankFileLogFilter.Failed });

        file.FileName.ShouldStartWith("BankFileExecutionLog_");
        file.FileName.ShouldEndWith(".xlsx");
        var bytes = Resolve<ITempFileCacheManager>().GetFile(file.FileToken);
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
    }

    // ================================================================== 5. salary register lock

    [Fact]
    public async Task Should_Lock_The_Runs_Registers_When_The_Company_Locks_On_Payroll_Exit()
    {
        UsePayrollSettings(s => s.LockSalaryRegisterOnPayrollExit = true);
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        var registerId = CreateRegister(employeeId, PayrollRunType.Regular, 500);

        var output = await _service.GenerateBankFile(Input());

        IsLocked(registerId).ShouldBeTrue();
        output.RegistersLocked.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Lock_When_The_Company_Does_Not_Lock_On_Payroll_Exit()
    {
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        var registerId = CreateRegister(employeeId, PayrollRunType.Regular, 500);

        var output = await _service.GenerateBankFile(Input());

        IsLocked(registerId).ShouldBeFalse();
        output.RegistersLocked.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Lock_Registers_The_File_Leaves_Out_As_Legacy_Locked_Its_Whole_Register_List()
    {
        // Below the bank-file minimum: no amount in the file, but legacy passed allSalRegList to the lock.
        UsePayrollSettings(s =>
        {
            s.LockSalaryRegisterOnPayrollExit = true;
            s.BankFileMinAmount = 50;
        });
        var paid = CreateEmployeeInArea("E001");
        CreateEmployeeBank(paid, _nbkId, PaymentType.Bank);
        var paidRegister = CreateRegister(paid, PayrollRunType.Regular, 500);
        var small = CreateEmployeeInArea("E002");
        CreateEmployeeBank(small, _nbkId, PaymentType.Bank);
        var smallRegister = CreateRegister(small, PayrollRunType.Regular, 10);

        var output = await _service.GenerateBankFile(Input());

        output.TotalAmount.ShouldBe(500d);
        IsLocked(paidRegister).ShouldBeTrue();
        IsLocked(smallRegister).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Lock_Every_Selected_Register_When_Splitting_Per_Employee_Bank()
    {
        // DECISION: legacy locked only the last bank's registers here (its loop reassigns allSalRegList), in an
        // order the database chose. Every register in the run is locked instead.
        UsePayrollSettings(s => s.LockSalaryRegisterOnPayrollExit = true);
        var cbkId = CreateBank("CBK", "Commercial Bank");
        var e1 = CreateEmployeeInArea("E001");
        CreateEmployeeBank(e1, _nbkId, PaymentType.Bank);
        var r1 = CreateRegister(e1, PayrollRunType.Regular, 100);
        var e2 = CreateEmployeeInArea("E002");
        CreateEmployeeBank(e2, cbkId, PaymentType.Bank);
        var r2 = CreateRegister(e2, PayrollRunType.Regular, 200);

        var output = await _service.GenerateBankFile(Input(i => i.SplitBankFiles = true));

        output.RegistersLocked.ShouldBe(2);
        IsLocked(r1).ShouldBeTrue();
        IsLocked(r2).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Lock_Off_Cycle_Registers_In_A_Regular_And_Off_Cycle_Run()
    {
        UsePayrollSettings(s => s.LockSalaryRegisterOnPayrollExit = true);
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        var regular = CreateRegister(employeeId, PayrollRunType.Regular, 100);
        var offCycle = CreateRegister(employeeId, PayrollRunType.OffCycle, 50, new DateTime(2026, 8, 20));

        await _service.GenerateBankFile(Input(i => i.PayrollRunType = "A"));

        IsLocked(regular).ShouldBeTrue();
        IsLocked(offCycle).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Leave_An_Already_Locked_Register_Alone_And_Not_Count_It()
    {
        UsePayrollSettings(s => s.LockSalaryRegisterOnPayrollExit = true);
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        var registerId = CreateRegister(employeeId, PayrollRunType.Regular, 500);
        UsingDbContext(c => c.Set<SalaryRegister>().Single(r => r.Id == registerId).Locked = true);

        var output = await _service.GenerateBankFile(Input());

        output.RegistersLocked.ShouldBe(0);
        IsLocked(registerId).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Conflict_With_The_Payroll_Exit_Lock()
    {
        // Bank file first, then exit payroll: the exit finds the register locked already and carries on.
        UsePayrollSettings(s => s.LockSalaryRegisterOnPayrollExit = true);
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        var registerId = CreateRegister(employeeId, PayrollRunType.Regular, 500);
        await _service.GenerateBankFile(Input());

        var recordId = UsingDbContext(c => c.Set<PayrollControlRecord>().Single(r => r.PayrollAreaId == _payrollAreaId).Id);
        await Resolve<IPayrollControlRecordsAppService>().SetState(new SetPayrollPeriodStateInput
        {
            Id = recordId,
            State = PayrollPeriodState.Closed
        });

        IsLocked(registerId).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Lock_Anything_When_The_Run_Fails()
    {
        UsePayrollSettings(s => s.LockSalaryRegisterOnPayrollExit = true);
        var employeeId = CreateEmployeeInArea("E001");
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        var registerId = CreateRegister(employeeId, PayrollRunType.Regular, 500);
        var kibId = CreateBank("KIB", "Kuwait International Bank");

        // KIB with no layout assigned makes no file (legacy), so nothing may be locked.
        await Should.ThrowAsync<UserFriendlyException>(() => _service.GenerateBankFile(Input(i => i.PaymentBankId = kibId)));

        IsLocked(registerId).ShouldBeFalse();
    }

    // ================================================================== 6. totals for text layouts, GBK-T1 company

    [Fact]
    public async Task Should_Report_The_Trailer_Count_And_Total_For_A_Text_Layout()
    {
        var cbkId = CreateBank("CBK", "Commercial Bank", formatCode: "CBK-T1");
        var e1 = CreateEmployeeInArea("E001");
        CreateEmployeeBank(e1, cbkId, PaymentType.Bank, account: "1000000001");
        CreateRegister(e1, PayrollRunType.Regular, 100.5);
        var e2 = CreateEmployeeInArea("E002");
        CreateEmployeeBank(e2, cbkId, PaymentType.Bank, account: "1000000002");
        CreateRegister(e2, PayrollRunType.Regular, 200.25);
        // No account number: CBK-T1 leaves the payee out, so neither the count nor the total include him.
        var e3 = CreateEmployeeInArea("E003");
        CreateEmployeeBank(e3, cbkId, PaymentType.Bank, account: null);
        CreateRegister(e3, PayrollRunType.Regular, 999);

        var output = await _service.GenerateBankFile(Input(i => i.PaymentBankId = cbkId));

        output.FormatCode.ShouldBe("CBK-T1");
        output.ContentType.ShouldBe("text/plain");
        output.RecordCount.ShouldBe(2);
        output.TotalAmount.ShouldBe(300.75d, 0.0001);

        var text = Encoding.UTF8.GetString(Convert.FromBase64String(output.Base64Content));
        text.ShouldContain("TRLINT 002 00002        00000300.750");
    }

    [Fact]
    public async Task Should_Print_The_Employees_Current_Company_On_Gbk_T1()
    {
        var gbkId = CreateBank("GBK", "Gulf Bank", formatCode: "GBK-T1");
        var companyId = CreateOrgUnit("00077", "Acme Trading Co", "Level1", "Company");
        var departmentId = CreateOrgUnit("00077.00001", "Finance", "Level2", "Department", parentId: companyId);
        var employeeId = CreateEmployee("E001");
        UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = employeeId, StartDate = new DateTime(2020, 1, 1), EndDate = OpenEnded,
            OrganizationUnitId = departmentId, PayrollAreaId = _payrollAreaId
        }));
        CreateEmployeeBank(employeeId, gbkId, PaymentType.Bank, account: "1000000001");
        CreateRegister(employeeId, PayrollRunType.Regular, 750);

        var output = await _service.GenerateBankFile(Input(i => i.PaymentBankId = gbkId));

        output.FormatCode.ShouldBe("GBK-T1");
        var detail = Encoding.UTF8.GetString(Convert.FromBase64String(output.Base64Content))
            .Split('\n').Select(l => l.TrimEnd('\r')).Single(l => l.StartsWith("DTL"));
        detail.ShouldEndWith("ACME TRADING CO");
        output.TotalAmount.ShouldBe(750d);
        output.RecordCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Leave_The_Gbk_T1_Company_Blank_Without_A_Company_Unit_Type()
    {
        var gbkId = CreateBank("GBK", "Gulf Bank", formatCode: "GBK-T1");
        var unitId = CreateOrgUnit("00077", "Acme Trading Co", "Level1", "Division");
        var employeeId = CreateEmployee("E001");
        UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = employeeId, StartDate = new DateTime(2020, 1, 1), EndDate = OpenEnded,
            OrganizationUnitId = unitId, PayrollAreaId = _payrollAreaId
        }));
        CreateEmployeeBank(employeeId, gbkId, PaymentType.Bank, account: "1000000001");
        CreateRegister(employeeId, PayrollRunType.Regular, 750);

        var output = await _service.GenerateBankFile(Input(i => i.PaymentBankId = gbkId));

        var detail = Encoding.UTF8.GetString(Convert.FromBase64String(output.Base64Content))
            .Split('\n').Select(l => l.TrimEnd('\r')).Single(l => l.StartsWith("DTL"));
        detail.ShouldEndWith("000000003Y111303268258                       ");
    }

    // ================================================================== off-cycle cutoff (legacy GetCutoffDate)

    [Fact]
    public async Task Should_Take_The_Off_Cycle_Cutoff_From_The_Payroll_Month_Even_For_A_Fiscal_Period()
    {
        // LEGACY AS-IS: payroll month 5 of an Apr-Mar area is AUGUST, but the cutoff is built from month 5 as if it
        // were May. An off-cycle paid in June is therefore inside the cutoff and goes into the file.
        var areaId = CreatePayrollArea("Fiscal", "Apr-Mar");
        CreateControlRecord(areaId, 5, 2026, MonthStart, MonthEnd);
        var employeeId = CreateEmployee("E001");
        AssignToArea(employeeId, areaId);
        CreateEmployeeBank(employeeId, _nbkId, PaymentType.Bank);
        CreateRegister(employeeId, PayrollRunType.OffCycle, 75, new DateTime(2026, 6, 15));

        var output = await _service.GenerateBankFile(Input(i =>
        {
            i.PayrollAreaId = areaId;
            i.PayrollRunType = "A";
            i.PeriodType = BankFilePeriodType.Other;
            i.Month = 5;
            i.Year = 2026;
        }));

        output.EndDateText.ShouldBe("31/08/2026");
        output.TotalAmount.ShouldBe(75d);
    }

    // ================================================================== helpers

    private GenerateBankFileInput Input(Action<GenerateBankFileInput> configure = null)
    {
        var input = new GenerateBankFileInput
        {
            PayrollAreaId = _payrollAreaId,
            PaymentBankId = _nbkId,
            PayrollRunType = "R",
            PeriodType = BankFilePeriodType.Current,
            Month = 8,
            Year = 2026
        };
        configure?.Invoke(input);
        return input;
    }

    /// <summary>E001 paid by bank (100), E002 in cash (200), E003 with a register but no employee-bank record.</summary>
    private void SeedPaymentTypeMix()
    {
        var bank = CreateEmployeeInArea("E001");
        CreateEmployeeBank(bank, _nbkId, PaymentType.Bank);
        CreateRegister(bank, PayrollRunType.Regular, 100);

        var cash = CreateEmployeeInArea("E002");
        CreateEmployeeBank(cash, null, PaymentType.Cash, iban: null, account: null);
        CreateRegister(cash, PayrollRunType.Regular, 200);

        var none = CreateEmployeeInArea("E003");
        CreateRegister(none, PayrollRunType.Regular, 300);
    }

    private ExecutionLog LogRow(int id) => UsingDbContext(c => c.Set<ExecutionLog>().Single(l => l.Id == id));

    private bool IsLocked(int registerId) =>
        UsingDbContext(c => c.Set<SalaryRegister>().Single(r => r.Id == registerId).Locked);

    private int InsertLog(ExecutionLogModule module, ExecutionLogStatus status, DateTime at, string userName = "admin") =>
        UsingDbContext(c =>
        {
            var row = c.Set<ExecutionLog>().Add(new ExecutionLog
            {
                TenantId = TenantId, UserName = userName, ActionDateTime = at, Action = ExecutionLogAction.GENERATE,
                Module = module, ExecutionStatus = status, Parameters = "seeded"
            }).Entity;
            c.SaveChanges();
            return row.Id;
        });

    private void UsePayrollSettings(Action<ManagePayrollSettings> configure) =>
        UsingDbContext(c =>
        {
            var settings = new ManagePayrollSettings { TenantId = TenantId };
            configure(settings);
            c.Set<ManagePayrollSettings>().Add(settings);
        });

    private static Dictionary<string, byte[]> Unzip(byte[] zip)
    {
        var result = new Dictionary<string, byte[]>();
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            result[entry.FullName] = copy.ToArray();
        }

        return result;
    }

    private int CreatePayrollArea(string name, string payrollPeriodCode) =>
        UsingDbContext(c =>
        {
            var tag = Next();
            var currency = c.Set<Currency>().Add(new Currency { TenantId = TenantId, Code = "C" + tag, NameEn = "KWD", NameAr = "KWD", DecimalCount = 3 }).Entity;
            var period = c.Set<PayrollPeriod>().Add(new PayrollPeriod { TenantId = TenantId, Code = payrollPeriodCode, NameEn = payrollPeriodCode, NameAr = payrollPeriodCode }).Entity;
            c.SaveChanges();

            var area = c.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId, Code = "PA" + tag, Name = name, PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 25
            }).Entity;
            c.SaveChanges();
            return area.Id;
        });

    private void CreateControlRecord(int areaId, int month, int year, DateTime start, DateTime end) =>
        UsingDbContext(c => c.Set<PayrollControlRecord>().Add(new PayrollControlRecord
        {
            TenantId = TenantId, PayrollAreaId = areaId, PayrollMonth = month, PayrollYear = year,
            CalendarMonthStart = start, CalendarMonthEnd = end
        }));

    private int CreateBank(string code, string nameEn, string formatCode = null) =>
        UsingDbContext(c =>
        {
            int? bankFileId = null;
            if (formatCode != null)
            {
                var format = c.Set<BankFile>().Add(new BankFile { TenantId = TenantId, Code = formatCode, NameEn = formatCode, NameAr = formatCode }).Entity;
                c.SaveChanges();
                bankFileId = format.Id;
            }

            var bank = c.Set<Bank>().Add(new Bank { TenantId = TenantId, Code = code, NameEn = nameEn, NameAr = nameEn, BankFileId = bankFileId }).Entity;
            c.SaveChanges();
            return bank.Id;
        });

    private int CreateEmployee(string code) =>
        UsingDbContext(c =>
        {
            var nationality = c.Set<Nationality>().Add(new Nationality { TenantId = TenantId, Code = "N" + Next(), NameEn = "N", NameAr = "N" }).Entity;
            c.SaveChanges();

            var employee = c.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId, Code = code, FirstNameEn = "F", LastNameEn = "L", FirstNameAr = "ف", LastNameAr = "ل",
                FullNameEn = "Employee " + code, FullNameAr = code, BirthDate = new DateTime(1990, 1, 1), Gender = Gender.Male,
                NationalityId = nationality.Id, JoiningDate = new DateTime(2020, 1, 1), CivilID = 290010100000 + _seq
            }).Entity;
            c.SaveChanges();
            return employee.Id;
        });

    private int CreateEmployeeInArea(string code)
    {
        var employeeId = CreateEmployee(code);
        AssignToArea(employeeId, _payrollAreaId);
        return employeeId;
    }

    private void AssignToArea(int employeeId, int areaId) =>
        UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = employeeId, StartDate = new DateTime(2020, 1, 1), EndDate = OpenEnded, PayrollAreaId = areaId
        }));

    private void CreateEmployeeBank(int employeeId, int? bankId, PaymentType paymentType,
                                    string iban = "KW81NBOK0000000000001234560101", string account = "1234567890") =>
        UsingDbContext(c => c.Set<EmployeeBank>().Add(new EmployeeBank
        {
            TenantId = TenantId, EmployeeId = employeeId, BankId = bankId, PaymentType = paymentType,
            StartDate = new DateTime(2020, 1, 1), EndDate = OpenEnded, IBAN = iban, AccountNum = account
        }));

    private int CreateRegister(int employeeId, PayrollRunType runType, double netPay, DateTime? offCyclePaymentDate = null) =>
        UsingDbContext(c =>
        {
            var register = c.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId, EmployeeId = employeeId, ForPeriod = MonthEnd, InPeriod = MonthEnd,
                PayrollRunType = runType, PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = offCyclePaymentDate ?? default
            }).Entity;
            c.SaveChanges();

            c.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId, SalaryRegisterId = register.Id, WageTypeId = _wageType9000Id, Amount = netPay
            });
            c.SaveChanges();
            return register.Id;
        });

    private long CreateOrgUnit(string code, string name, string levelCode, string typeName,
                               long? parentId = null, int? payrollAreaId = null) =>
        UsingDbContext(c =>
        {
            var type = c.Set<OrganizationUnitType>().FirstOrDefault(t => t.Code == levelCode && t.NameEn == typeName)
                       ?? c.Set<OrganizationUnitType>().Add(new OrganizationUnitType { TenantId = TenantId, Code = levelCode, NameEn = typeName, NameAr = typeName }).Entity;
            var unit = new OrganizationUnit(TenantId, name, parentId) { Code = code };
            c.OrganizationUnits.Add(unit);
            c.SaveChanges();

            c.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId, OrganizationUnitId = unit.Id, Code = code, NameAr = name,
                OrganizationUnitTypeId = type.Id, PayrollAreaId = payrollAreaId
            });
            c.SaveChanges();
            return unit.Id;
        });

    private int CreateSponsor(string code) =>
        UsingDbContext(c =>
        {
            var nationality = c.Set<Nationality>().Add(new Nationality { TenantId = TenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            var businessType = c.Set<BusinessType>().Add(new BusinessType { TenantId = TenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            var governorate = c.Set<Governorate>().Add(new Governorate { TenantId = TenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            var unit = new OrganizationUnit(TenantId, code, null) { Code = "9" + Next().PadLeft(4, '0') };
            c.OrganizationUnits.Add(unit);
            c.SaveChanges();

            var sponsor = c.Set<Sponsor>().Add(new Sponsor
            {
                TenantId = TenantId, Code = code, NameEn = code, NameAr = code, NationalityId = nationality.Id,
                BusinessTypeId = businessType.Id, GovernorateId = governorate.Id, OrganizationUnitId = unit.Id
            }).Entity;
            c.SaveChanges();
            return sponsor.Id;
        });

    private void CreateSponsorBank(int sponsorId, int paymentBankId, string accountNumber) =>
        UsingDbContext(c => c.Set<SponsorBank>().Add(new SponsorBank
        {
            TenantId = TenantId, SponsorId = sponsorId, PaymentBankId = paymentBankId, AccountNumber = accountNumber,
            StartDate = new DateTime(2020, 1, 1), EndDate = OpenEnded
        }));

    private string Next() => (++_seq).ToString();
}
