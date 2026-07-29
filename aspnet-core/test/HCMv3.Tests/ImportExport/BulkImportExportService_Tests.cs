using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HCMv3.ImportExport;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ImportExport;

/// <summary>
/// Tests for <see cref="BulkImportExportService"/> -- the generic Excel import/export engine ported from
/// legacy <c>uscBulkImportExport.ascx.cs</c>.
///
/// <para>Weighted toward validation and error reporting: a wrong row that lands in the database silently is
/// the one failure mode of this feature that corrupts the system of record, so the assertions below check
/// BOTH that the row was rejected AND that nothing was written.</para>
///
/// <para><c>Holiday</c> is used as the target entity because it exercises every conversion branch that
/// matters in one shape: a string key (<c>Code</c>), required strings (<c>NameEn</c>/<c>NameAr</c>), a
/// REQUIRED reference (<c>HolidayCalendar</c>) and an OPTIONAL reference (<c>OvertimeType</c>).</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class BulkImportExportService_Tests : AppTestBase
{
    private const int TestTenantId = 1;
    private const string HolidayTypeName = "HCMv3.TimeManagement.Holiday";

    private readonly IBulkImportExportService _service;

    public BulkImportExportService_Tests()
    {
        _service = Resolve<IBulkImportExportService>();
    }

    #region << Entity discovery (legacy BindModuleName) >>

    [Fact]
    public void GetImportableEntities_Should_Include_Concrete_Domain_Entities()
    {
        var entities = _service.GetImportableEntities();

        entities.ShouldContain(e => e.FullName == HolidayTypeName);
        entities.ShouldContain(e => e.FullName == "HCMv3.TimeManagement.TransType");
    }

    [Fact]
    public void GetImportableEntities_Should_Be_Sorted_By_Name()
    {
        var names = _service.GetImportableEntities().Select(e => e.Name).ToList();

        names.ShouldBe(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList());
    }

    [Fact]
    public void Columns_Should_Exclude_Framework_Owned_Columns()
    {
        // Legacy's ExcludeList equivalent: nothing the framework owns may appear in a template, or an
        // operator could overwrite a primary key / tenant / audit stamp.
        var columns = _service.GetEntityDescriptor(HolidayTypeName).Columns;

        columns.ShouldNotContain("Id");
        columns.ShouldNotContain("TenantId");
        columns.ShouldNotContain("IsDeleted");
        columns.ShouldNotContain("CreationTime");
        columns.ShouldNotContain("CreatorUserId");
        columns.ShouldNotContain("LastModificationTime");
        columns.ShouldNotContain("DeleterUserId");
    }

    [Fact]
    public void Columns_Should_Expose_Reference_By_Name_Not_By_Foreign_Key()
    {
        var columns = _service.GetEntityDescriptor(HolidayTypeName).Columns;

        columns.ShouldContain("HolidayCalendar");
        columns.ShouldContain("OvertimeType");
        // The {Nav}Id shadow scalar is suppressed so one relationship never gets two columns that
        // could disagree.
        columns.ShouldNotContain("HolidayCalendarId");
        columns.ShouldNotContain("OvertimeTypeId");
    }

    [Fact]
    public void Columns_Should_Exclude_Binary_Payloads()
    {
        // Employee.Photo is a byte[]; binary does not round-trip through a spreadsheet template and has its
        // own importer (legacy uscBulkImport).
        var columns = _service.GetEntityDescriptor("HCMv3.PersonnelAdministration.Employee").Columns;

        columns.ShouldNotContain("Photo");
    }

    [Fact]
    public void GetEntityDescriptor_Should_Reject_Blank_Entity()
    {
        // Legacy Main(): `if (ModuleName == "") throw new Exception("Entity not selected.");`
        Should.Throw<BulkImportExportException>(() => _service.GetEntityDescriptor(""));
        Should.Throw<BulkImportExportException>(() => _service.GetEntityDescriptor(null));
    }

    [Fact]
    public void GetEntityDescriptor_Should_Reject_Unknown_Entity()
    {
        Should.Throw<BulkImportExportException>(() => _service.GetEntityDescriptor("HCMv3.Nope.NotAThing"));
    }

    #endregion

    #region << Export (legacy DownloadGrid) >>

    [Fact]
    public void BuildWorkbook_Template_Should_Contain_Header_Row_Only()
    {
        var bytes = _service.BuildWorkbook(HolidayTypeName, withData: false);

        var sheet = OpenFirstSheet(bytes);
        var expected = _service.GetEntityDescriptor(HolidayTypeName).Columns;

        for (var i = 0; i < expected.Count; i++)
        {
            sheet.Cell(1, i + 1).GetString().ShouldBe(expected[i]);
        }

        // Legacy produced the same thing by appending "WHERE 1 = 0" to its export query.
        (sheet.LastRowUsed()?.RowNumber() ?? 0).ShouldBe(1);
    }

    [Fact]
    public async Task BuildWorkbook_WithData_Should_Emit_Reference_As_Its_Code()
    {
        var calendarId = await CreateHolidayCalendarAsync("CAL-EXP");
        await CreateHolidayAsync("H-EXP", calendarId);

        var bytes = _service.BuildWorkbook(HolidayTypeName, withData: true);
        var sheet = OpenFirstSheet(bytes);

        var columns = _service.GetEntityDescriptor(HolidayTypeName).Columns;
        var codeColumn = columns.IndexOf("Code") + 1;
        var calendarColumn = columns.IndexOf("HolidayCalendar") + 1;

        var row = Enumerable.Range(2, (sheet.LastRowUsed()?.RowNumber() ?? 1) - 1)
            .FirstOrDefault(r => sheet.Cell(r, codeColumn).GetString() == "H-EXP");

        row.ShouldNotBe(0);
        // Legacy GetClassSelect projected the referenced row's Code, not its key.
        sheet.Cell(row, calendarColumn).GetString().ShouldBe("CAL-EXP");
    }

    #endregion

    #region << Import: happy path >>

    [Fact]
    public async Task ImportAsync_Should_Insert_Rows_And_Resolve_References_By_Code()
    {
        var calendarId = await CreateHolidayCalendarAsync("CAL-OK");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            FileName = "holidays.xlsx",
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(
                new[] { "IMP-1", "National Day", "اليوم الوطني", "CAL-OK" })
        });

        result.TotalRecords.ShouldBe(1);
        result.Successfull.ShouldBe(1);
        result.Failed.ShouldBe(0);
        result.Aborted.ShouldBeFalse();

        await UsingDbContextAsync(async ctx =>
        {
            var holiday = await ctx.Holidays.FirstOrDefaultAsync(h => h.Code == "IMP-1");
            holiday.ShouldNotBeNull();
            holiday.NameEn.ShouldBe("National Day");
            holiday.HolidayCalendarId.ShouldBe(calendarId);
            holiday.TenantId.ShouldBe(TestTenantId);
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Write_One_Log_Row_Per_Spreadsheet_Row()
    {
        await CreateHolidayCalendarAsync("CAL-LOG");
        await _service.ClearLogAsync();

        await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(
                new[] { "LOG-1", "One", "واحد", "CAL-LOG" },
                new[] { "LOG-2", "Two", "اثنان", "CAL-LOG" })
        });

        await UsingDbContextAsync(async ctx =>
        {
            var logs = await ctx.BulkUploadLogs.OrderBy(l => l.RecordNo).ToListAsync();

            logs.Count.ShouldBe(2);
            // Legacy recorded the 1-based EXCEL row number, i.e. data rows start at 2.
            logs[0].RecordNo.ShouldBe(2);
            logs[1].RecordNo.ShouldBe(3);
            logs.ShouldAllBe(l => l.Status == 0);
            logs.ShouldAllBe(l => l.Message == "Successfull");
            // Legacy's generic importer never attached an employee to its log rows.
            logs.ShouldAllBe(l => l.EmployeeId == null);
            logs.ShouldAllBe(l => l.NoOfEmployee == 2);
            logs.ShouldAllBe(l => l.LogType == BulkUploadLogType.BulkDataImport);
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Record_An_ExecutionLog_Summary()
    {
        await CreateHolidayCalendarAsync("CAL-EXEC");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            CurrentUserName = "admin",
            FileContent = BuildHolidayWorkbook(new[] { "EXEC-1", "One", "واحد", "CAL-EXEC" })
        });

        result.ExecutionLogId.ShouldBeGreaterThan(0);

        await UsingDbContextAsync(async ctx =>
        {
            var log = await ctx.ExecutionLogs.FirstAsync(e => e.Id == result.ExecutionLogId);

            log.Module.ShouldBe(ExecutionLogModule.BulkImportExport);
            log.Action.ShouldBe(ExecutionLogAction.RUN);
            // The legacy screen name is preserved verbatim so existing execution-log reports keep working.
            log.ScreenName.ShouldBe("uscBulkImportExport");
            log.ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
            log.TotalRecords.ShouldBe(1);
            log.Successfull.ShouldBe(1);
            log.Failed.ShouldBe(0);
            log.UserName.ShouldBe("admin");
        });
    }

    #endregion

    #region << Import: validation and error reporting >>

    [Fact]
    public async Task ImportAsync_Should_Reject_A_Column_That_Is_Not_A_Property()
    {
        var content = BuildWorkbook(
            new[] { "Code", "NameEn", "NameAr", "HolidayCalendar", "Nonsense" },
            new[] { "X", "X", "X", "X", "X" });

        var ex = await Should.ThrowAsync<BulkImportExportException>(() => _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = content
        }));

        // Legacy message text, which is the operator-facing contract.
        ex.Message.ShouldContain("Nonsense");
        ex.Message.ShouldContain("Holiday");
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_The_Row_When_A_Reference_Does_Not_Exist()
    {
        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(new[] { "BAD-REF", "One", "واحد", "NO-SUCH-CALENDAR" })
        });

        result.Successfull.ShouldBe(0);
        result.Failed.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            // The critical assertion: a rejected row must leave NOTHING behind.
            (await ctx.Holidays.AnyAsync(h => h.Code == "BAD-REF")).ShouldBeFalse();

            var log = await ctx.BulkUploadLogs.OrderByDescending(l => l.Id).FirstAsync();
            log.Status.ShouldBe(1);
            log.Message.ShouldContain("HolidayCalendar");
            log.Message.ShouldContain("NO-SUCH-CALENDAR");
            log.Message.ShouldContain("Not exists");
            // Legacy prefixed row-level failures with the spreadsheet row number.
            log.Message.ShouldContain("2");
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Continue_After_A_Bad_Row()
    {
        await CreateHolidayCalendarAsync("CAL-MIX");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(
                new[] { "MIX-BAD", "Bad", "سيئ", "NO-SUCH" },
                new[] { "MIX-GOOD", "Good", "جيد", "CAL-MIX" })
        });

        // Legacy created a fresh IObjectSpace per row precisely so one bad row could not take the batch
        // with it. This is the behaviour that makes a run non-atomic, and it is deliberate.
        result.Failed.ShouldBe(1);
        result.Successfull.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.Holidays.AnyAsync(h => h.Code == "MIX-BAD")).ShouldBeFalse();
            (await ctx.Holidays.AnyAsync(h => h.Code == "MIX-GOOD")).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Mark_A_Mixed_Run_As_Partial()
    {
        await CreateHolidayCalendarAsync("CAL-PART");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(
                new[] { "PART-BAD", "Bad", "سيئ", "NO-SUCH" },
                new[] { "PART-GOOD", "Good", "جيد", "CAL-PART" })
        });

        await UsingDbContextAsync(async ctx =>
        {
            var log = await ctx.ExecutionLogs.FirstAsync(e => e.Id == result.ExecutionLogId);
            log.ExecutionStatus.ShouldBe(ExecutionLogStatus.PARTIAL);
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Mark_An_All_Failed_Run_As_Failed()
    {
        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(new[] { "ALLBAD", "Bad", "سيئ", "NO-SUCH" })
        });

        await UsingDbContextAsync(async ctx =>
        {
            var log = await ctx.ExecutionLogs.FirstAsync(e => e.Id == result.ExecutionLogId);
            log.ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Reject_An_Empty_File()
    {
        // Legacy Main(): `if (file == "") throw new Exception("File not selected.");`
        await Should.ThrowAsync<BulkImportExportException>(() => _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            FileContent = Array.Empty<byte>()
        }));
    }

    [Fact]
    public async Task ImportAsync_Should_Reject_A_Corrupt_Workbook()
    {
        // Legacy swallowed this and imported zero rows while reporting success. Not reproduced.
        await Should.ThrowAsync<BulkImportExportException>(() => _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            FileContent = new byte[] { 1, 2, 3, 4, 5 }
        }));
    }

    #endregion

    #region << Import: validate-only (legacy OnlyValidate) >>

    [Fact]
    public async Task ValidateOnly_Should_Report_Without_Writing_Anything()
    {
        await CreateHolidayCalendarAsync("CAL-VAL");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            ValidateOnly = true,
            FileContent = BuildHolidayWorkbook(
                new[] { "VAL-GOOD", "Good", "جيد", "CAL-VAL" },
                new[] { "VAL-BAD", "Bad", "سيئ", "NO-SUCH" })
        });

        result.ValidateOnly.ShouldBeTrue();
        result.Successfull.ShouldBe(1);
        result.Failed.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.Holidays.AnyAsync(h => h.Code == "VAL-GOOD")).ShouldBeFalse();
            (await ctx.Holidays.AnyAsync(h => h.Code == "VAL-BAD")).ShouldBeFalse();
            // ... but the operator still gets the full report.
            (await ctx.BulkUploadLogs.CountAsync()).ShouldBe(2);
        });
    }

    [Fact]
    public async Task ValidateOnly_Should_Not_Purge()
    {
        var calendarId = await CreateHolidayCalendarAsync("CAL-NOPURGE");
        await CreateHolidayAsync("SURVIVOR", calendarId);

        await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Purge,
            ValidateOnly = true,
            FileContent = BuildHolidayWorkbook(new[] { "NP-1", "One", "واحد", "CAL-NOPURGE" })
        });

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.Holidays.AnyAsync(h => h.Code == "SURVIVOR")).ShouldBeTrue();
        });
    }

    #endregion

    #region << Import type: purge (legacy "P") >>

    [Fact]
    public async Task Purge_Should_Remove_Existing_Rows_Before_Importing()
    {
        var calendarId = await CreateHolidayCalendarAsync("CAL-PURGE");
        await CreateHolidayAsync("OLD-ROW", calendarId);

        await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Purge,
            FileContent = BuildHolidayWorkbook(new[] { "NEW-ROW", "New", "جديد", "CAL-PURGE" })
        });

        await UsingDbContextAsync(async ctx =>
        {
            // Purge goes through IRepository.DeleteAsync, so a FullAuditedEntity is SOFT-deleted -- see
            // BulkImportType.Purge, which spells out how this differs from legacy's raw
            // "DELETE FROM [X]; DBCC CHECKIDENT(...)". The raw test DbContext has no soft-delete filter,
            // hence the explicit IsDeleted check.
            (await ctx.Holidays.AnyAsync(h => h.Code == "OLD-ROW" && !h.IsDeleted)).ShouldBeFalse();
            (await ctx.Holidays.AnyAsync(h => h.Code == "NEW-ROW" && !h.IsDeleted)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Incremental_Should_Keep_Existing_Rows()
    {
        var calendarId = await CreateHolidayCalendarAsync("CAL-INC");
        await CreateHolidayAsync("KEEP-ME", calendarId);

        await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(new[] { "ADDED", "New", "جديد", "CAL-INC" })
        });

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.Holidays.AnyAsync(h => h.Code == "KEEP-ME")).ShouldBeTrue();
            (await ctx.Holidays.AnyAsync(h => h.Code == "ADDED")).ShouldBeTrue();
        });
    }

    #endregion

    #region << Stop handshake (legacy btnStopUploadingFile) >>

    [Fact]
    public async Task RequestStopAsync_Should_Abort_The_Next_Run_At_Its_First_Row()
    {
        await CreateHolidayCalendarAsync("CAL-STOP");
        await _service.RequestStopAsync();

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(new[] { "STOPPED", "One", "واحد", "CAL-STOP" })
        });

        result.Aborted.ShouldBeTrue();
        result.Successfull.ShouldBe(0);

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.Holidays.AnyAsync(h => h.Code == "STOPPED")).ShouldBeFalse();

            var log = await ctx.ExecutionLogs.FirstAsync(e => e.Id == result.ExecutionLogId);
            log.ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
        });
    }

    [Fact]
    public async Task ClearLogAsync_Should_Empty_The_Log()
    {
        await CreateHolidayCalendarAsync("CAL-CLR");

        await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildHolidayWorkbook(new[] { "CLR-1", "One", "واحد", "CAL-CLR" })
        });

        await _service.ClearLogAsync();

        await UsingDbContextAsync(async ctx =>
        {
            // Legacy TRUNCATEd the table; this soft-deletes, so nothing is visible through the repository
            // but the rows remain physically present for the raw test DbContext.
            (await ctx.BulkUploadLogs.CountAsync(l => !l.IsDeleted)).ShouldBe(0);
        });
    }

    #endregion

    #region << Cell conversion (legacy GetDateTimeValue / bool / enum) >>

    [Fact]
    public async Task Booleans_Should_Follow_Legacys_YES_Rule()
    {
        // Legacy: `PropValue = Convert.ToString(dr[key]) == "YES" ? true : false;` -- anything else,
        // INCLUDING the string "TRUE", is false. Faithfully preserved, because a template produced by the
        // export side only ever emits YES/NO.
        var employeeId = await CreateEmployeeForTimeEventAsync("TE-BOOL");
        var transTypeId = await CreateTransTypeAsync("TT-BOOL");

        await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = "HCMv3.TimeManagement.TimeEvent",
            ImportType = BulkImportType.Incremental,
            FileContent = BuildWorkbook(
                new[] { "Employee", "TransType", "TransDate", "TransTime", "PunchInRange", "RecordSource", "TransSource" },
                new[] { "TE-BOOL", "TT-BOOL", "15-03-2024", "08:30", "YES", "9001", "System" },
                new[] { "TE-BOOL", "TT-BOOL", "16-03-2024", "08:30", "TRUE", "9002", "System" })
        });

        await UsingDbContextAsync(async ctx =>
        {
            var yes = await ctx.TimeEvents.FirstOrDefaultAsync(e => e.RecordSource == 9001);
            var notYes = await ctx.TimeEvents.FirstOrDefaultAsync(e => e.RecordSource == 9002);

            yes.ShouldNotBeNull();
            yes.PunchInRange.ShouldBeTrue();
            yes.EmployeeId.ShouldBe(employeeId);
            yes.TransTypeId.ShouldBe(transTypeId);
            // Legacy parsed dd-MM-yyyy, InvariantCulture. 15 March, not 3 December.
            yes.TransDate.ShouldBe(new DateTime(2024, 3, 15));
            // A column whose name contains "time" keeps only the time-of-day (legacy `.GetTime()`).
            yes.TransTime.TimeOfDay.ShouldBe(new TimeSpan(8, 30, 0));
            yes.TransSource.ShouldBe(TransSource.System);

            notYes.ShouldNotBeNull();
            notYes.PunchInRange.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task An_Unparseable_Date_Should_Fail_The_Row_Not_The_Run()
    {
        await CreateEmployeeForTimeEventAsync("TE-DATE");
        await CreateTransTypeAsync("TT-DATE");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = "HCMv3.TimeManagement.TimeEvent",
            ImportType = BulkImportType.Incremental,
            FileContent = BuildWorkbook(
                new[] { "Employee", "TransType", "TransDate", "TransTime", "RecordSource" },
                new[] { "TE-DATE", "TT-DATE", "not-a-date", "08:30", "9101" })
        });

        result.Failed.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.TimeEvents.AnyAsync(e => e.RecordSource == 9101)).ShouldBeFalse();

            var log = await ctx.BulkUploadLogs.OrderByDescending(l => l.Id).FirstAsync();
            log.Status.ShouldBe(1);
            log.Message.ShouldContain("TransDate");
            log.Message.ShouldContain("not valid");
        });
    }

    [Fact]
    public async Task An_Unknown_Enum_Name_Should_Fail_The_Row()
    {
        await CreateEmployeeForTimeEventAsync("TE-ENUM");
        await CreateTransTypeAsync("TT-ENUM");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = "HCMv3.TimeManagement.TimeEvent",
            ImportType = BulkImportType.Incremental,
            FileContent = BuildWorkbook(
                new[] { "Employee", "TransType", "TransDate", "TransTime", "RecordSource", "TransSource" },
                new[] { "TE-ENUM", "TT-ENUM", "15-03-2024", "08:30", "9201", "Telepathy" })
        });

        result.Failed.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.TimeEvents.AnyAsync(e => e.RecordSource == 9201)).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task A_Blank_Optional_Reference_Should_Be_Accepted_As_Null()
    {
        // Legacy's "is blank" error branch in GetClassObject is commented out -- blank means null, not an
        // error. Holiday.OvertimeType is the optional reference here.
        await CreateHolidayCalendarAsync("CAL-BLANK");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildWorkbook(
                new[] { "Code", "NameEn", "NameAr", "HolidayCalendar", "OvertimeType" },
                new[] { "BLANK-OT", "One", "واحد", "CAL-BLANK", "" })
        });

        result.Successfull.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            var holiday = await ctx.Holidays.FirstAsync(h => h.Code == "BLANK-OT");
            holiday.OvertimeTypeId.ShouldBeNull();
        });
    }

    [Fact]
    public async Task A_Blank_Required_Reference_Should_Fail_The_Row()
    {
        // Legacy would have written FK 0 here and let the database reject it (or, on a legacy XPO schema
        // without the constraint, silently store a dangling reference). Failed loudly instead.
        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildWorkbook(
                new[] { "Code", "NameEn", "NameAr", "HolidayCalendar" },
                new[] { "NO-CAL", "One", "واحد", "" })
        });

        result.Failed.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            (await ctx.Holidays.AnyAsync(h => h.Code == "NO-CAL")).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Blank_Rows_Should_Not_Be_Counted()
    {
        await CreateHolidayCalendarAsync("CAL-EMPTY");

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Incremental,
            FileContent = BuildWorkbook(
                new[] { "Code", "NameEn", "NameAr", "HolidayCalendar" },
                new[] { "REAL-ROW", "One", "واحد", "CAL-EMPTY" },
                new[] { "", "", "", "" })
        });

        result.TotalRecords.ShouldBe(1);
        result.Successfull.ShouldBe(1);
        result.Failed.ShouldBe(0);
    }

    #endregion

    #region << Round trip >>

    [Fact]
    public async Task An_Exported_File_Should_Re_Import_Cleanly()
    {
        // The single most valuable property of this feature: export -> edit -> import must not mangle
        // dates, booleans, enums or references.
        var calendarId = await CreateHolidayCalendarAsync("CAL-RT");
        await CreateHolidayAsync("RT-1", calendarId);

        var exported = _service.BuildWorkbook(HolidayTypeName, withData: true);

        var result = await _service.ImportAsync(new BulkImportRunInput
        {
            TenantId = TestTenantId,
            EntityFullName = HolidayTypeName,
            ImportType = BulkImportType.Purge,
            FileContent = exported
        });

        result.Failed.ShouldBe(0);
        result.Successfull.ShouldBe(result.TotalRecords);

        await UsingDbContextAsync(async ctx =>
        {
            var holiday = await ctx.Holidays.FirstAsync(h => h.Code == "RT-1");
            holiday.HolidayCalendarId.ShouldBe(calendarId);
        });
    }

    #endregion

    #region << Helpers >>

    private static IXLWorksheet OpenFirstSheet(byte[] bytes)
    {
        var workbook = new XLWorkbook(new MemoryStream(bytes));
        return workbook.Worksheets.First();
    }

    private static byte[] BuildHolidayWorkbook(params string[][] rows)
    {
        var all = new List<string[]> { new[] { "Code", "NameEn", "NameAr", "HolidayCalendar" } };
        all.AddRange(rows);
        return BuildWorkbook(all.ToArray());
    }

    /// <summary>First array is the header row; the rest are data rows. Everything is written as text,
    /// which is the worst case for the parser and therefore what the tests should exercise.</summary>
    private static byte[] BuildWorkbook(params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                sheet.Cell(r + 1, c + 1).SetValue(rows[r][c]);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<int> CreateHolidayCalendarAsync(string code)
    {
        return await UsingDbContextAsync(async ctx =>
        {
            var calendar = new HolidayCalendar
            {
                TenantId = TestTenantId,
                Code = code,
                NameEn = code,
                NameAr = code
            };

            ctx.HolidayCalendars.Add(calendar);
            await ctx.SaveChangesAsync();
            return calendar.Id;
        });
    }

    private async Task<int> CreateHolidayAsync(string code, int calendarId)
    {
        return await UsingDbContextAsync(async ctx =>
        {
            var holiday = new Holiday
            {
                TenantId = TestTenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                HolidayCalendarId = calendarId
            };

            ctx.Holidays.Add(holiday);
            await ctx.SaveChangesAsync();
            return holiday.Id;
        });
    }

    private async Task<int> CreateTransTypeAsync(string code)
    {
        return await UsingDbContextAsync(async ctx =>
        {
            var transType = new TransType
            {
                TenantId = TestTenantId,
                Code = code,
                NameEn = code,
                NameAr = code
            };

            ctx.TransTypes.Add(transType);
            await ctx.SaveChangesAsync();
            return transType.Id;
        });
    }

    private async Task<int> CreateEmployeeForTimeEventAsync(string code)
    {
        return await UsingDbContextAsync(async ctx =>
        {
            // Employee.NationalityId is a NON-nullable FK, so a nationality has to exist first.
            var nationality = new HCMv3.GeneralMasterInfo.Nationality
            {
                TenantId = TestTenantId,
                Code = "NAT-" + code,
                NameEn = "NAT-" + code,
                NameAr = "NAT-" + code
            };

            ctx.Nationalities.Add(nationality);
            await ctx.SaveChangesAsync();

            var employee = new HCMv3.PersonnelAdministration.Employee
            {
                TenantId = TestTenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = code,
                FirstNameAr = code,
                LastNameAr = code,
                NationalityId = nationality.Id
            };

            ctx.Employees.Add(employee);
            await ctx.SaveChangesAsync();
            return employee.Id;
        });
    }

    #endregion
}
