using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PayrollManagement;

/// <summary>
/// One Post JV run: legacy <c>CreatePostJV</c> (<c>uscCostingFile.ascx.cs</c> lines 765-1078) posting through
/// <c>PostTablewiseJV_D360</c>.
///
/// <list type="bullet">
/// <item><description><b>Regular</b> (legacy lines 783-930): the whole area as ONE salary journal, or two
/// journals -- salary and provision -- when <c>SeparateProvisionPosting</c> is on, then one closing log row
/// with the accumulated message.</description></item>
/// <item><description><b>OffCycle</b> (legacy lines 932-1075): ONE JOURNAL PER EMPLOYEE, for every employee
/// with an off-cycle cost register for the period in the area, each with its own header
/// "Employee : {code} - Payroll/Salary (OffCycle) For {MMM-yyyy}" and its own log row. Provision posting is
/// never separated here, as in legacy.</description></item>
/// </list>
///
/// <para><b>Added before the first journal (not in legacy):</b> the Integration Center switch, the balance
/// test and the preview fingerprint, all through <see cref="PostJvPreconditions"/>. They are checked here
/// even though the screen checked them before queuing, because the run starts later.</para>
///
/// <para><b>Partial postings are recorded, not undone.</b> Legacy kept going after a failed line batch and
/// left whatever had reached D365 in D365. So does this: every header and batch is logged the moment it is
/// known (by the client's observer), and when a later journal fails the failure row names the journals that
/// had already reached D365. Nothing is reversed (the user's decision).</para>
///
/// <para><b>Stop</b> is honoured before each off-cycle employee (legacy line 980) and, for a Regular run,
/// before each of its journals. It is not honoured between the line batches of one journal: legacy
/// commented that check out (lines 1171-1176), and stopping there would leave a half-filled journal.</para>
///
/// <para><b>Never throws</b>: it runs in a background job, and a retried Post JV would post twice.</para>
/// </summary>
public class PostJvRunner : AbpServiceBase, ITransientDependency
{
    private readonly PostJvPreconditions _preconditions;
    private readonly IJournalVoucherBuilder _journalVoucherBuilder;
    private readonly IFinancePostingClient _financePostingClient;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<CostRegister> _costRegisterRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IFinanceRunLog _runLog;

    public PostJvRunner(
        PostJvPreconditions preconditions,
        IJournalVoucherBuilder journalVoucherBuilder,
        IFinancePostingClient financePostingClient,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<CostRegister> costRegisterRepository,
        IRepository<Employee> employeeRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IFinanceRunLog runLog)
    {
        _preconditions = preconditions;
        _journalVoucherBuilder = journalVoucherBuilder;
        _financePostingClient = financePostingClient;
        _sysSettingsAccessor = sysSettingsAccessor;
        _costRegisterRepository = costRegisterRepository;
        _employeeRepository = employeeRepository;
        _payrollAreaResolver = payrollAreaResolver;
        _runLog = runLog;
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    /// <summary>Legacy journal header, Regular salary (line 824).</summary>
    public static string SalaryJournalHeader(DateTime payInDate) =>
        "Employee Payroll/Salary For " + payInDate.ToString("MMM-yyyy", CultureInfo.InvariantCulture);

    /// <summary>Legacy journal header, Regular provision (line 839).</summary>
    public static string ProvisionJournalHeader(DateTime payInDate) =>
        "Employee Provision For " + payInDate.ToString("MMM-yyyy", CultureInfo.InvariantCulture);

    /// <summary>Legacy journal header, one per off-cycle employee (line 977).</summary>
    public static string OffCycleJournalHeader(string employeeCode, PayrollRunType runType, DateTime payInDate) =>
        "Employee : " + employeeCode + " - Payroll/Salary (" + runType + ") For " +
        payInDate.ToString("MMM-yyyy", CultureInfo.InvariantCulture);

    public async Task RunAsync(FinanceRunJobArgs args)
    {
        var input = args.ToInput();
        var executionLogId = args.ExecutionLogId;

        try
        {
            if (!await _runLog.TryClaimAsync(executionLogId))
            {
                // ABP re-runs a job left in its store by a restart. Posting again would send the same
                // journals to D365 a second time, so the run is closed as interrupted instead.
                Logger.Warn("Post JV run " + executionLogId + " was already started once; it is not started again.");
                await _runLog.MarkInterruptedAsync(executionLogId, input.PayInDate);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Post JV run " + executionLogId + " could not be started.", ex);
            return;
        }

        var posted = new List<string>();
        var observer = new PostingObserver(_runLog, executionLogId, posted);

        try
        {
            var payrollArea = await _preconditions.RequirePostableAreaAsync(input.PayrollAreaName);
            await _preconditions.EnsurePostingSwitchedOnAsync();
            var journalVoucher = await _preconditions.BuildPostableAsync(input, args.PreviewFingerprint);

            if (input.PayrollRunType == PayrollRunType.OffCycle)
            {
                await PostOffCycleAsync(payrollArea, input, executionLogId, observer);
            }
            else
            {
                await PostRegularAsync(payrollArea, input, journalVoucher, executionLogId, observer, posted);
            }
        }
        catch (Exception ex)
        {
            // Legacy Main's catch (lines 362-375) / CreatePostJV's catch (lines 885-909): a failed row, and the
            // run carries on to its finally.
            Logger.Error("Post JV run " + executionLogId + " failed.", ex);
            await WriteSafelyAsync(executionLogId, null, input.PayInDate, WithPostedJournals(ex.Message, posted), 1, 0);
        }
        finally
        {
            await _runLog.CloseAsync(executionLogId);
        }
    }

    /// <summary>Legacy lines 783-930.</summary>
    private async Task PostRegularAsync(PayrollArea payrollArea, CostingRunInput input, JournalVoucherResult journalVoucher,
                                        int executionLogId, PostingObserver observer, List<string> posted)
    {
        var lineCount = journalVoucher.Lines.Count;

        // Legacy lines 806-815: with SeparateProvisionPosting the lines split on the BillingType column.
        var fiSettings = await _sysSettingsAccessor.GetFIPostingSettingsAsync();
        var separateProvisionPosting = fiSettings?.SeparateProvisionPosting ?? false;

        List<JournalVoucherLine> salaryLines;
        List<JournalVoucherLine> provisionLines = null;

        if (separateProvisionPosting)
        {
            salaryLines = journalVoucher.Lines
                .Where(l => l.BillingType != JournalVoucherBuilder.ProvisionBillingTypeLabel)
                .ToList();
            provisionLines = journalVoucher.Lines
                .Where(l => l.BillingType == JournalVoucherBuilder.ProvisionBillingTypeLabel)
                .ToList();
        }
        else
        {
            salaryLines = journalVoucher.Lines.ToList();
        }

        var messages = new List<string>();

        try
        {
            // Legacy lines 819-832.
            if (salaryLines.Any())
            {
                if (await _runLog.IsStopRequestedAsync(executionLogId))
                {
                    return;
                }

                await _financePostingClient.PostJournalAsync(payrollArea, salaryLines,
                                                             SalaryJournalHeader(input.PayInDate), observer);
                messages.Add(L("PayrollSalaryJvPostedSuccessfully"));
            }
            else
            {
                messages.Add(L("NoDataFoundToPostPayrollSalaryJv"));
            }

            // Legacy lines 834-846.
            if (provisionLines != null)
            {
                if (provisionLines.Any())
                {
                    if (await _runLog.IsStopRequestedAsync(executionLogId))
                    {
                        await WriteSafelyAsync(executionLogId, null, input.PayInDate,
                                               WithPostedJournals(string.Join(Environment.NewLine, messages), posted), 0, lineCount);
                        return;
                    }

                    await _financePostingClient.PostJournalAsync(payrollArea, provisionLines,
                                                                 ProvisionJournalHeader(input.PayInDate), observer);
                    messages.Add(L("ProvisionJvPostedSuccessfully"));
                }
                else
                {
                    messages.Add(L("NoDataFoundToPostProvisionJv"));
                }
            }
        }
        catch (Exception ex)
        {
            // Legacy lines 885-909: the failure row; NoOfEmployee is the JV's line count. The journals that
            // had already gone are named, because nothing takes them back out of D365.
            Logger.Error("Post JV run " + executionLogId + ": a journal failed.", ex);
            await WriteSafelyAsync(executionLogId, null, input.PayInDate, WithPostedJournals(ex.Message, posted), 1, lineCount);
            return;
        }

        // Legacy lines 848-857: one employee-less SUCCESS row with the accumulated message.
        var message = string.Join(Environment.NewLine, messages);
        if (posted.Any())
        {
            message += Environment.NewLine + L("JournalBatchNumber") + " : " + string.Join(", ", posted);
        }

        await WriteSafelyAsync(executionLogId, null, input.PayInDate, message, 0, lineCount);
    }

    /// <summary>Legacy lines 932-1075: one journal per employee.</summary>
    private async Task PostOffCycleAsync(PayrollArea payrollArea, CostingRunInput input, int executionLogId,
                                         PostingObserver observer)
    {
        var employees = await ResolveOffCycleEmployeesAsync(input);
        var noOfEmployees = employees.Count;

        foreach (var employee in employees)
        {
            // Legacy lines 979-983.
            if (await _runLog.IsStopRequestedAsync(executionLogId))
            {
                break;
            }

            try
            {
                // Legacy line 986: the JV rebuilt for this one employee.
                var employeeVoucher = await _journalVoucherBuilder.BuildAsync(new CostingRunInput
                {
                    PayrollAreaName = input.PayrollAreaName,
                    EmployeeCode = employee.Code,
                    PayInDate = input.PayInDate,
                    PostingDate = input.PostingDate,
                    PayrollRunType = input.PayrollRunType
                });

                if (employeeVoucher.Lines.Any())
                {
                    await _financePostingClient.PostJournalAsync(
                        payrollArea, employeeVoucher.Lines,
                        OffCycleJournalHeader(employee.Code, input.PayrollRunType, input.PayInDate), observer);
                }

                // Legacy lines 1004-1017: the row says "Successfull" whether or not there was anything to post
                // -- the "No Data found" message legacy built is never written.
                await _runLog.WriteAsync(executionLogId, employee.Id, input.PayInDate, CostEngineService.SuccessMessage, 0,
                                         noOfEmployees);
            }
            catch (Exception ex)
            {
                // Legacy lines 1051-1067: a failed row for THIS employee, and on to the next one.
                Logger.Warn("Post JV run " + executionLogId + ": employee " + employee.Code + " failed.", ex);
                await WriteSafelyAsync(executionLogId, employee.Id, input.PayInDate, Truncate(ex.Message), 1, noOfEmployees);
            }
        }
    }

    /// <summary>
    /// Legacy lines 948-959: employees with an off-cycle cost register for the period who are in the area on
    /// that date, narrowed to the code box when it is filled (and then ordered by code).
    /// </summary>
    private async Task<List<(int Id, string Code)>> ResolveOffCycleEmployeesAsync(CostingRunInput input)
    {
        var registerEmployeeIds = _costRegisterRepository.GetAll()
            .Where(r => r.Period == input.PayInDate && r.PayrollRunType == input.PayrollRunType)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToList();

        var areaEmployeeIds = new HashSet<int>(
            await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(input.PayrollAreaName, input.PayInDate));

        var ids = registerEmployeeIds.Where(areaEmployeeIds.Contains).ToList();

        var employees = _employeeRepository.GetAll()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.Code })
            .ToList()
            .OrderBy(e => e.Id)
            .Select(e => (e.Id, e.Code))
            .ToList();

        var requestedCodes = JournalVoucherBuilder.ParseEmployeeCodes(input.EmployeeCode);
        if (requestedCodes.Any())
        {
            var codeSet = new HashSet<string>(requestedCodes, StringComparer.Ordinal);
            employees = employees
                .Where(e => codeSet.Contains(e.Code))
                .OrderBy(e => e.Code, StringComparer.Ordinal)
                .ToList();
        }

        return employees;
    }

    private string WithPostedJournals(string message, List<string> posted)
    {
        message ??= "";
        // The numbers are appended rather than formatted into the text, so they are on the row whatever the
        // translation says.
        return posted.Any()
            ? message + Environment.NewLine + L("FinanceJournalsAlreadyPosted") + " " + string.Join(", ", posted)
            : message;
    }

    private async Task WriteSafelyAsync(int executionLogId, int? employeeId, DateTime inPeriod, string message, int status,
                                        int noOfEmployee)
    {
        try
        {
            await _runLog.WriteAsync(executionLogId, employeeId, inPeriod, message, status, noOfEmployee);
        }
        catch (Exception ex)
        {
            Logger.Warn("Post JV run " + executionLogId + ": a log row could not be written.", ex);
        }
    }

    private static string Truncate(string message)
    {
        message ??= "";
        return message.Length > FinanceRunLog.MaxEmployeeMessageLength
            ? message.Substring(0, FinanceRunLog.MaxEmployeeMessageLength)
            : message;
    }

    /// <summary>
    /// Legacy's per-call rows from <c>PostJournalHeader</c> / <c>PostMultiJournalLines</c>: employee-less,
    /// <c>InPeriod</c> left at <c>default(DateTime)</c>, <c>NoOfEmployee</c> 0 (lines 1343-1352, 1359-1376,
    /// 1440-1455, 1471-1491). Also collects the batch numbers that reached D365.
    /// </summary>
    private class PostingObserver : IFinancePostingObserver
    {
        private readonly IFinanceRunLog _runLog;
        private readonly int _executionLogId;
        private readonly List<string> _posted;

        public PostingObserver(IFinanceRunLog runLog, int executionLogId, List<string> posted)
        {
            _runLog = runLog;
            _executionLogId = executionLogId;
            _posted = posted;
        }

        public async Task OnPostingEventAsync(FinancePostingEvent postingEvent)
        {
            if (postingEvent.Succeeded
                && !string.IsNullOrWhiteSpace(postingEvent.JournalBatchNumber)
                && !_posted.Contains(postingEvent.JournalBatchNumber))
            {
                _posted.Add(postingEvent.JournalBatchNumber);
            }

            try
            {
                await _runLog.WriteAsync(_executionLogId, null, default, postingEvent.Message,
                                         postingEvent.Succeeded ? 0 : 1, 0);
            }
            catch
            {
                // The posting itself must carry on; the log failure is already in the server log.
            }
        }
    }
}
