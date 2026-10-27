using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.Dto;
using HCMv3.EngineRuns.Dto;
using HCMv3.Storage;

namespace HCMv3.PayrollManagement.Exporting;

public interface IPayrollRunEmployeesExcelExporter
{
    /// <param name="employees">The run's per-employee rows, already filtered by the caller.</param>
    Task<FileDto> ExportToFile(List<EngineRunEmployeeDto> employees);
}

/// <summary>
/// Legacy <c>uscPayrollEngine.btnExportPayrollLog_Click</c> (<c>uscPayrollEngine.ascx.cs</c> line 2166): the
/// payroll log grid written to Excel, named <c>PayrollExecutionLog_yyyyMMdd_HHmmss</c>. Columns are legacy's
/// (code, employee, run time, status, message); sequence and time taken are added because this port records them.
/// </summary>
public class PayrollRunEmployeesExcelExporter : MiniExcelExcelExporterBase, IPayrollRunEmployeesExcelExporter
{
    public PayrollRunEmployeesExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : base(tempFileCacheManager)
    {
    }

    public async Task<FileDto> ExportToFile(List<EngineRunEmployeeDto> employees)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var employee in employees ?? new List<EngineRunEmployeeDto>())
        {
            items.Add(new Dictionary<string, object>
            {
                { "#", employee.SequenceNo },
                { L("EmployeeCode"), employee.EmployeeCode },
                { L("EmployeeName"), employee.EmployeeName },

                // A real date-time, so the column sorts chronologically in Excel.
                { L("PayrollRunRunTime"), employee.CompletedAt },
                { L("Status"), employee.Succeeded ? L("Successfull") : L("Failed") },
                { L("Message"), employee.Message },
                { L("PayrollRunElapsedSeconds"), employee.ElapsedSeconds }
            });
        }

        return await CreateExcelPackageAsync(
            "PayrollExecutionLog_" + Clock.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx",
            items);
    }
}
