using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using Microsoft.Data.SqlClient;

namespace HCMv3.PayrollEngineRunner;

/// <summary>
/// Puts the HCMv3 database into the state the engine requires before it will run a REGULAR payroll for a
/// historical month, and prints every row it touches.
///
/// <para><b>This is run-state setup, NOT a calculation fix.</b> Nothing here influences a single wage-type
/// result -- it only satisfies the three run-level gates the engine applies before any calculation starts.
/// It is opt-in (<c>--prepare</c>) precisely so that a verification run can also be executed WITHOUT it, to
/// observe what the migrated data does on its own.</para>
///
/// <para>Three prerequisites, each of which is a genuine finding in its own right:</para>
/// <list type="number">
/// <item><description><b><see cref="PayrollCalendar"/> is empty.</b> The legacy <c>PayrollCalendar</c> table
/// has 240 rows; the migrator has no spec for it at all, so HCMv3 has zero. The engine hard-fails at
/// "Payroll Calender does not have record for Payroll month you are running." (legacy Program.cs lines
/// 248-255). Seeded here for the month under test only.</description></item>
/// <item><description><b><see cref="PayrollControlRecord"/> sits on the LATEST period</b> (Mar/Apr 2025),
/// because that is what legacy holds today. A regular run requires
/// <c>CalendarMonthEnd == payInDate &amp;&amp; PeriodLocked</c>, so the control record must be wound back to
/// the month under test. This is normal payroll-period bookkeeping, not a data repair.</description></item>
/// <item><description><b>NULL dates migrated as <c>1900-01-01</c>, not <c>DateTime.MinValue</c>.</b> Legacy
/// stores NULL in <c>EmployeePayrollStatus.EarliestMasterDataChange</c> and <c>DoNoAccountAfter</c>; the
/// migration landed <c>1900-01-01</c> in the non-nullable HCMv3 columns. Every "is this date set?" guard in
/// the engine tests <c>!= default(DateTime)</c> / <c>!= new DateTime(1,1,1)</c>, so <c>1900-01-01</c> reads
/// as SET. See the two concrete consequences documented on
/// <see cref="NormalizeNullDateSentinelsAsync"/>.</description></item>
/// </list>
/// </summary>
public static class PrepareRunPrerequisites
{
    public static async Task RunAsync(
        IIocManager ioc, DateTime payInDate, string employeeCodes, bool normalizeNullDates,
        string connectionString, bool payrollReasonTypeSentinel)
    {
        if (payrollReasonTypeSentinel)
        {
            AddPayrollReasonTypeSentinelRow(connectionString);
        }

        var uowManager = ioc.Resolve<IUnitOfWorkManager>();

        using (var uow = uowManager.Begin())
        {
            await SeedPayrollCalendarAsync(ioc, payInDate);
            await WindControlRecordsAsync(ioc, payInDate);

            if (normalizeNullDates)
            {
                await NormalizeNullDateSentinelsAsync(ioc, employeeCodes);
            }

            await uow.CompleteAsync();
        }
    }

    /// <summary>
    /// WORKAROUND, NOT A FIX -- for a blocking port defect that this verification run discovered.
    ///
    /// <para>Legacy <c>SalaryRegister.PayrollReasonType</c> is NULLABLE (62,400 of its 64,999 rows are NULL --
    /// i.e. every regular run). The port narrowed it to a non-nullable required FK,
    /// <c>SalaryRegister.PayrollReasonTypeId int NOT NULL</c> with
    /// <c>FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId</c>. A REGULAR run never assigns a reason
    /// (only <c>ProcessOffCycleEmployeeAsync</c> does), so the column stays at its CLR default of 0, and
    /// <c>SaveChanges</c> dies with a foreign-key violation. On top of that <c>PayrollReasonTypes</c> is
    /// EMPTY -- the migrator has no spec for it either.</para>
    ///
    /// <para>Net effect: <b>the ported engine cannot persist a single regular-run salary register against the
    /// migrated database.</b> The correct fix is to make <c>PayrollReasonTypeId</c> nullable (matching legacy)
    /// -- that is an application change and is deliberately NOT made here. Instead this inserts an
    /// <c>Id = 0</c> "(none)" row so 0 becomes a legal FK target and the run can proceed far enough to produce
    /// numbers to compare. It touches no calculation input and no wage rule.</para>
    /// </summary>
    private static void AddPayrollReasonTypeSentinelRow(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM PayrollReasonTypes WHERE Id = 0)
BEGIN
    SET IDENTITY_INSERT PayrollReasonTypes ON;
    INSERT INTO PayrollReasonTypes (Id, TenantId, Code, NameEn, NameAr, CreationTime, IsDeleted)
    VALUES (0, 1, '(none)', '(none)', '(none)', SYSUTCDATETIME(), 0);
    SET IDENTITY_INSERT PayrollReasonTypes OFF;
    SELECT 1;
END
ELSE SELECT 0;";

        var inserted = Convert.ToInt32(command.ExecuteScalar());
        Console.WriteLine(inserted == 1
            ? "  [prepare] PayrollReasonTypes Id=0 '(none)' sentinel inserted (workaround for the non-nullable SalaryRegister.PayrollReasonTypeId FK)."
            : "  [prepare] PayrollReasonTypes Id=0 '(none)' sentinel already present.");
    }

    private static async Task SeedPayrollCalendarAsync(IIocManager ioc, DateTime payInDate)
    {
        var calendarRepository = ioc.Resolve<IRepository<PayrollCalendar>>();
        var payrollAreaRepository = ioc.Resolve<IRepository<PayrollArea>>();

        var monthStart = new DateTime(payInDate.Year, payInDate.Month, 1);
        var monthEnd = new DateTime(payInDate.Year, payInDate.Month, DateTime.DaysInMonth(payInDate.Year, payInDate.Month));

        foreach (var payrollArea in payrollAreaRepository.GetAll().ToList())
        {
            var exists = calendarRepository.GetAll().Any(c => c.PayrollAreaId == payrollArea.Id
                                                              && c.PayrollMonth == payInDate.Month
                                                              && c.PayrollYear == payInDate.Year);
            if (exists)
            {
                continue;
            }

            await calendarRepository.InsertAsync(new PayrollCalendar
            {
                TenantId = payrollArea.TenantId,
                PayrollAreaId = payrollArea.Id,
                PayrollMonth = payInDate.Month,
                PayrollYear = payInDate.Year,
                StartDate = monthStart,
                EndDate = monthEnd
            });

            Console.WriteLine($"  [prepare] PayrollCalendar seeded: area {payrollArea.Id} '{payrollArea.Name}' {payInDate:yyyy-MM}");
        }
    }

    private static async Task WindControlRecordsAsync(IIocManager ioc, DateTime payInDate)
    {
        var controlRecordRepository = ioc.Resolve<IRepository<PayrollControlRecord>>();

        var monthStart = new DateTime(payInDate.Year, payInDate.Month, 1);
        var monthEnd = new DateTime(payInDate.Year, payInDate.Month, DateTime.DaysInMonth(payInDate.Year, payInDate.Month));

        foreach (var record in controlRecordRepository.GetAll().ToList())
        {
            Console.WriteLine($"  [prepare] PayrollControlRecord {record.Id} (area {record.PayrollAreaId}): "
                              + $"{record.CalendarMonthEnd:yyyy-MM-dd} locked={record.PeriodLocked} "
                              + $"-> {monthEnd:yyyy-MM-dd} locked=True");

            record.PayrollMonth = payInDate.Month;
            record.PayrollYear = payInDate.Year;
            record.CalendarMonthStart = monthStart;
            record.CalendarMonthEnd = monthEnd;
            record.PeriodLocked = true;
            record.PeriodClosed = false;

            await controlRecordRepository.UpdateAsync(record);
        }
    }

    /// <summary>
    /// Rewrites the migrated <c>1900-01-01</c> "no value" sentinel back to <see cref="DateTime.MinValue"/>,
    /// which is what every engine guard actually tests for.
    ///
    /// <para><b>Left as-is, the migrated value breaks a regular run in two independent ways:</b></para>
    /// <list type="number">
    /// <item><description><c>EarliestMasterDataChange = 1900-01-01</c> is read as a real retro trigger.
    /// <c>CollectPayrollPeriods</c> then sees <c>1900-01-01 &lt; EarliestRetroAccountingDate</c> (the
    /// employee's joining date) and THROWS
    /// "EarliestRetroAccountingDate set to '...' for employee : ..." -- every employee fails, no register is
    /// written. If that throw were not there, the same value would instead be clamped to
    /// <c>EarliestEvaluationDate</c> (2021-04-01) and the engine would recalculate ~39 months of retro
    /// registers per employee.</description></item>
    /// <item><description><c>DoNoAccountAfter = 1900-01-01</c> is read as "do not account after 1900", so the
    /// per-period guard <c>DoNoAccountAfter &lt; payForDate &amp;&amp; != DateTime(1,1,1)</c> skips EVERY
    /// period silently -- an employee would be reported as successfully processed with no register at
    /// all.</description></item>
    /// </list>
    /// </summary>
    private static async Task NormalizeNullDateSentinelsAsync(IIocManager ioc, string employeeCodes)
    {
        var payrollStatusRepository = ioc.Resolve<IRepository<EmployeePayrollStatus>>();
        var employeeRepository = ioc.Resolve<IRepository<HCMv3.PersonnelAdministration.Employee>>();

        var codes = (employeeCodes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var employeeIds = employeeRepository.GetAll()
            .Where(e => codes.Count == 0 || codes.Contains(e.Code))
            .Select(e => e.Id)
            .ToList();

        var sentinel = new DateTime(1900, 1, 1);

        foreach (var status in payrollStatusRepository.GetAll().Where(s => employeeIds.Contains(s.EmployeeId)).ToList())
        {
            var changed = false;

            if (status.EarliestMasterDataChange.Date == sentinel)
            {
                status.EarliestMasterDataChange = DateTime.MinValue;
                changed = true;
            }

            if (status.DoNoAccountAfter.Date == sentinel)
            {
                status.DoNoAccountAfter = DateTime.MinValue;
                changed = true;
            }

            if (changed)
            {
                await payrollStatusRepository.UpdateAsync(status);
                Console.WriteLine($"  [prepare] EmployeePayrollStatus emp {status.EmployeeId}: "
                                  + "1900-01-01 NULL-sentinel(s) normalised to DateTime.MinValue");
            }
        }
    }
}
