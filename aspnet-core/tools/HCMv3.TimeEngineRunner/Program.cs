using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement.Services;

namespace HCMv3.TimeEngineRunner;

/// <summary>
/// Runs the ported Time Engine for a set of employee codes over a date range against a real HCMv3 database.
/// Purpose-built for migration verification, not part of the application.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        string connectionString = null;
        string employeeCodes = null;
        DateTime? start = null;
        DateTime? end = null;
        string payrollArea = null;
        var tenantId = 1;

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => args[++i];
            switch (args[i].ToLowerInvariant())
            {
                case "--connection": connectionString = Next(); break;
                case "--employee-codes": employeeCodes = Next(); break;
                case "--start": start = DateTime.ParseExact(Next(), "yyyy-MM-dd", CultureInfo.InvariantCulture); break;
                case "--end": end = DateTime.ParseExact(Next(), "yyyy-MM-dd", CultureInfo.InvariantCulture); break;
                case "--payroll-area": payrollArea = Next(); break;
                case "--tenant-id": tenantId = int.Parse(Next()); break;
                default:
                    Console.Error.WriteLine($"Unknown argument '{args[i]}'.");
                    return 2;
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString) || end == null)
        {
            Console.Error.WriteLine("Usage: --connection <connstr> --employee-codes a,b,c --start yyyy-MM-dd --end yyyy-MM-dd [--payroll-area N] [--tenant-id N]");
            return 2;
        }

        HCMv3TimeEngineRunnerModule.ConnectionString = connectionString;

        Console.Out.Flush();
        var swBoot = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("Bootstrapping ABP...");
        Console.Out.Flush();

        using var bootstrapper = AbpBootstrapper.Create<HCMv3TimeEngineRunnerModule>();
        bootstrapper.Initialize();

        Console.WriteLine($"Bootstrapped in {swBoot.Elapsed.TotalSeconds:F1}s.");
        Console.Out.Flush();

        var ioc = bootstrapper.IocManager;

        try
        {
            return RunAsync(ioc, tenantId, employeeCodes, start, end.Value, payrollArea).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("RUN FAILED: " + ex);
            return 1;
        }
    }

    private static async Task<int> RunAsync(
        IIocManager ioc, int tenantId, string employeeCodes, DateTime? start, DateTime end, string payrollArea)
    {
        var uowManager = ioc.Resolve<IUnitOfWorkManager>();
        var session = ioc.Resolve<IAbpSession>();

        // The engine's entities are IMustHaveTenant; without a tenant on the session ABP's data filter would
        // return nothing at all and the run would silently do zero work.
        using (session.Use(tenantId, null))
        {
            Dictionary<int, int> subGroupIds;

            using (var uow = uowManager.Begin())
            {
                subGroupIds = ResolveSubGroupIds(ioc, employeeCodes);
                await uow.CompleteAsync();
            }

            Console.WriteLine($"Resolved EmployeeSubGroup for {subGroupIds.Count} employee(s) from EmployeeOrgAssignment.");

            var engine = ioc.Resolve<ITimeEngineService>();
            var input = new TimeEngineRunInput
            {
                EmployeeCodes = employeeCodes,
                StartDate = start,
                EndDate = end,
                PayrollAreaName = payrollArea,
                CurrentUserName = "legacy-migration-verification",
                IsRanFromPayrollEngine = false,
                EmployeeSubGroupIds = subGroupIds
            };

            using (var uow = uowManager.Begin())
            {
                var result = await engine.CreateTimeRegisterAsync(input);
                await uow.CompleteAsync();

                Console.WriteLine();
                Console.WriteLine("=== Time Engine run result ===");
                Console.WriteLine($"  ExecutionLogId : {result.ExecutionLogId}");
                Console.WriteLine($"  TotalRecords   : {result.TotalRecords}");
                Console.WriteLine($"  Successful     : {result.Successfull}");
                Console.WriteLine($"  Failed         : {result.Failed}");
                Console.WriteLine($"  Status         : {result.ExecutionStatus}");
                Console.WriteLine($"  Aborted        : {result.Aborted}");

                return result.Failed > 0 ? 1 : 0;
            }
        }
    }

    /// <summary>
    /// Builds the employeeId -&gt; EmployeeSubGroupId map the engine requires as an explicit input.
    ///
    /// <para>The engine documents this as caller-supplied because <c>EmployeeOrgAssignment</c> historically had
    /// no <c>EmployeeSubGroupId</c> FK. That FK now exists (migration 20260728103447) and the legacy migration
    /// populates it, so the map is derived here from the employee's latest org assignment -- rather than being
    /// hand-fed, which is what would hide a migration defect.</para>
    /// </summary>
    private static Dictionary<int, int> ResolveSubGroupIds(IIocManager ioc, string employeeCodes)
    {
        var employeeRepo = ioc.Resolve<IRepository<Employee>>();
        var orgRepo = ioc.Resolve<IRepository<EmployeeOrgAssignment>>();

        var codes = (employeeCodes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var employeeIds = employeeRepo.GetAll()
            .Where(e => codes.Count == 0 || codes.Contains(e.Code))
            .Select(e => e.Id)
            .ToList();

        var map = new Dictionary<int, int>();
        foreach (var id in employeeIds)
        {
            var subGroupId = orgRepo.GetAll()
                .Where(o => o.EmployeeId == id && o.EmployeeSubGroupId != null)
                .OrderByDescending(o => o.StartDate)
                .Select(o => o.EmployeeSubGroupId)
                .FirstOrDefault();

            if (subGroupId != null)
            {
                map[id] = subGroupId.Value;
            }
            else
            {
                Console.WriteLine($"  !! employee id {id} has NO EmployeeOrgAssignment.EmployeeSubGroupId - engine will report 'Employee Sub Group not found'.");
            }
        }

        return map;
    }
}
