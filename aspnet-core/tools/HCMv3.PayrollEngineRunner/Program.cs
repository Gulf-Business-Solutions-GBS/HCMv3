using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PayrollEngineRunner;

/// <summary>
/// Runs the ported Payroll Engine for a set of employee codes for one pay-in month against a real HCMv3
/// database. Purpose-built for migration verification, not part of the application.
///
/// <para>NOTE the engine deliberately isolates per-employee failures (see
/// <see cref="IPayrollEngineService"/>): the exit code reflects how many employees failed, but every failure
/// message is printed so the caller can tell an expected unported-rule failure from a new one.</para>
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        string connectionString = null;
        string employeeCodes = null;
        string payrollArea = null;
        DateTime? payInDate = null;
        var tenantId = 1;
        var prepare = false;
        var normalizeNullDates = false;
        var payrollReasonTypeSentinel = false;
        var traceExceptions = false;

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => args[++i];
            switch (args[i].ToLowerInvariant())
            {
                case "--connection": connectionString = Next(); break;
                case "--employee-codes": employeeCodes = Next(); break;
                case "--payroll-area": payrollArea = Next(); break;
                case "--pay-in-date": payInDate = DateTime.ParseExact(Next(), "yyyy-MM-dd", CultureInfo.InvariantCulture); break;
                case "--tenant-id": tenantId = int.Parse(Next()); break;
                case "--prepare": prepare = true; break;
                case "--normalize-null-dates": normalizeNullDates = true; break;
                case "--payroll-reason-type-sentinel": payrollReasonTypeSentinel = true; break;
                case "--trace-exceptions": traceExceptions = true; break;
                default:
                    Console.Error.WriteLine($"Unknown argument '{args[i]}'.");
                    return 2;
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString) || payInDate == null || string.IsNullOrWhiteSpace(payrollArea))
        {
            Console.Error.WriteLine("Usage: --connection <connstr> --payroll-area <name> --pay-in-date yyyy-MM-dd "
                                    + "[--employee-codes a,b,c] [--tenant-id N] [--prepare] [--normalize-null-dates] [--payroll-reason-type-sentinel]");
            return 2;
        }

        HCMv3PayrollEngineRunnerModule.ConnectionString = connectionString;

        // The engine deliberately swallows per-employee exceptions and keeps only ex.Message (legacy lines
        // 1438-1460), which for a NullReferenceException is the useless "Object reference not set...". A
        // first-chance hook is the only way to see WHERE it was thrown without touching engine code.
        if (traceExceptions)
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                if (e.Exception is Abp.UI.UserFriendlyException)
                {
                    return;
                }

                var key = e.Exception.GetType().Name + "|" + (e.Exception.StackTrace ?? string.Empty).Split('\n').FirstOrDefault();
                if (seen.Add(key))
                {
                    Console.WriteLine("  [first-chance] " + e.Exception.GetType().Name + ": " + e.Exception.Message);
                    Console.WriteLine(e.Exception.StackTrace);
                }
            };
        }

        var swBoot = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("Bootstrapping ABP...");
        Console.Out.Flush();

        using var bootstrapper = AbpBootstrapper.Create<HCMv3PayrollEngineRunnerModule>();
        bootstrapper.Initialize();

        Console.WriteLine($"Bootstrapped in {swBoot.Elapsed.TotalSeconds:F1}s.");
        Console.Out.Flush();

        try
        {
            return RunAsync(bootstrapper.IocManager, tenantId, employeeCodes, payrollArea, payInDate.Value, prepare, normalizeNullDates, connectionString, payrollReasonTypeSentinel)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("RUN FAILED: " + ex);
            return 1;
        }
    }

    private static async Task<int> RunAsync(
        IIocManager ioc, int tenantId, string employeeCodes, string payrollArea, DateTime payInDate,
        bool prepare, bool normalizeNullDates, string connectionString, bool payrollReasonTypeSentinel)
    {
        var uowManager = ioc.Resolve<IUnitOfWorkManager>();
        var session = ioc.Resolve<IAbpSession>();

        // The engine's entities are IMustHaveTenant; without a tenant on the session ABP's data filter would
        // return nothing at all and the run would silently do zero work.
        using (session.Use(tenantId, null))
        {
            if (prepare)
            {
                await PrepareRunPrerequisites.RunAsync(
                    ioc, payInDate, employeeCodes, normalizeNullDates, connectionString, payrollReasonTypeSentinel);
            }

            var engine = ioc.Resolve<IPayrollEngineService>();

            var input = new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                EmployeeCodes = employeeCodes,
                PayInDate = payInDate,
                PayrollAreaName = payrollArea,
                RegisterTarget = PayrollRegisterTarget.Live
            };

            using (var uow = uowManager.Begin())
            {
                // ---------------------------------------------------------------------------------------
                // NAVIGATION WARM-UP -- compensates for a PORT DEFECT, and is NOT a calculation change.
                //
                // AccrualService.cs line 1354 materialises the employee's TimeTransfer rows with .ToList()
                // and no Include(r => r.TransferType); line 1457 then dereferences r.TransferType.Code in
                // LINQ-to-Objects. This solution does not enable EF lazy-loading proxies, so that navigation
                // is null and the call throws NullReferenceException for ANY employee that has even one
                // TimeTransfer row -- all five of ours do. The test suite never caught it because it seeds no
                // TimeTransfer rows.
                //
                // Materialising the lookup table into the SAME DbContext first makes EF Core's relationship
                // fix-up populate the navigation, so the ported code reads the CORRECT value rather than
                // being handed altered data. Nothing is written; no input is changed.
                // ---------------------------------------------------------------------------------------
                var warmed = ioc.Resolve<IRepository<HCMv3.TimeManagement.TimeTransferType>>().GetAll().ToList().Count;
                Console.WriteLine($"  [warm-up] TimeTransferType rows tracked for EF fix-up: {warmed}");

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await engine.RunAsync(input);
                await uow.CompleteAsync();

                Console.WriteLine();
                Console.WriteLine("=== Payroll Engine run result ===");
                Console.WriteLine($"  PayrollArea : {payrollArea}");
                Console.WriteLine($"  PayInDate   : {result.PayInDate:yyyy-MM-dd}");
                Console.WriteLine($"  Aborted     : {result.Aborted}");
                Console.WriteLine($"  Employees   : {result.EmployeeResults.Count}");
                Console.WriteLine($"  Elapsed     : {sw.Elapsed.TotalSeconds:F1}s");
                Console.WriteLine();

                foreach (var employeeResult in result.EmployeeResults.OrderBy(r => r.EmployeeCode))
                {
                    Console.WriteLine($"  [{(employeeResult.Succeeded ? "OK  " : "FAIL")}] {employeeResult.EmployeeCode} "
                                      + $"(id {employeeResult.EmployeeId}) registers=[{string.Join(",", employeeResult.SalaryRegisterIds)}]");

                    if (!employeeResult.Succeeded)
                    {
                        Console.WriteLine("         " + (employeeResult.Message ?? string.Empty).Replace("\r\n", " / "));
                    }
                }

                return result.EmployeeResults.Count(r => !r.Succeeded);
            }
        }
    }
}
