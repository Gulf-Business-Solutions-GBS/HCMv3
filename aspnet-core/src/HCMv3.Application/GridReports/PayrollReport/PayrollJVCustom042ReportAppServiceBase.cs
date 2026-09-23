using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.SystemSecurity;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Shared helpers for the ten Payroll JV (Custom 042) report services -- legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>. Each concrete service still owns its own single
/// BuildRowsAsync (filtering + projection), per the porting-wave structural rule; only pure formatting
/// and month/year resolution live here.
/// </summary>
public abstract class PayrollJVCustom042ReportAppServiceBase : GridReportAppServiceBase
{
    /// <summary>Legacy "Oid"/1st column, and the parameter-string date literal used across the family.</summary>
    protected static string VoucherDate(DateTime endDate) => endDate.ToString("MMM, yyyy");

    /// <summary>
    /// Legacy <c>uscRptPayrollJV_Custom042</c> lines 681-683: the <c>ENABLE_WAGEACCOUNT_BY_ORGUNIT</c>
    /// tenant config key set to "1" makes this format take its wage accounts from the group mapped to the
    /// employee's organization unit instead of the employee's own rows. Only Custom 042 reads it.
    /// </summary>
    protected async Task<PayrollJVWageAccountSource> ResolveWageAccountSourceAsync()
    {
        var value = await SysSettingsAccessor.GetSysTenantConfigAsync(SysTenantConfigKeys.EnableWageAccountByOrgUnit);
        return value == "1"
            ? PayrollJVWageAccountSource.OrgUnitWageAccGroup
            : PayrollJVWageAccountSource.EmployeeWageAccount;
    }

    /// <summary>Legacy <c>GetTable()</c>: blank month/year mean "now"; an invalid pair fell back to 1900.</summary>
    protected DateTime ResolveEndDate(int? month, int? year)
    {
        var m = month ?? Today.Month;
        var y = year ?? Today.Year;

        try
        {
            return new DateTime(y, m, DateTime.DaysInMonth(y, m)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(1900, 1, 31).Date;
        }
    }

    /// <summary>Legacy <c>GetParameterStr()</c>: Payroll Area | Run Type | Month | Year | Emp Code.</summary>
    protected string BuildJvParameterString(GetPayrollJVCustom042ReportInputBase input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Legacy row builder for the eight branches that write <c>dr[Div]/dr[Dept]/dr[Anly1]</c> as the
    /// literal STRING "NULL" when the cost element is unmapped (e.g. lines 1063-1065, 1871-1873): NOT
    /// database null, an actual three-letter string that reaches the voucher. Reproduced verbatim.
    /// </summary>
    protected static PayrollJVCustom042ReportRowDto NewRowNullLiteral(
        List<PayrollJVCustom042ReportRowDto> rows,
        string mainCode, string mainName, string subCode, string subName,
        string description,
        string divisionCode, string departmentCode, string analysis1Code,
        string currency, double amount, string drCr, string employeeCode = "")
    {
        return NewRow(rows, mainCode, mainName, subCode, subName, description,
            divisionCode ?? "NULL", departmentCode ?? "NULL", analysis1Code ?? "NULL",
            currency, amount, drCr, employeeCode);
    }

    /// <summary>
    /// Legacy row builder for the branches that write <c>dr[Div]/dr[Dept]/dr[Anly1]</c> as an empty
    /// string when the cost element is unmapped (e.g. Advance Salary JV lines 1535-1537, Overtime JV
    /// lines 3002-3004).
    /// </summary>
    protected static PayrollJVCustom042ReportRowDto NewRow(
        List<PayrollJVCustom042ReportRowDto> rows,
        string mainCode, string mainName, string subCode, string subName,
        string description,
        string divisionCode, string departmentCode, string analysis1Code,
        string currency, double amount, string drCr, string employeeCode = "")
    {
        return new PayrollJVCustom042ReportRowDto
        {
            Id = rows.Count + 1,
            LineNumber = rows.Count + 1,
            EmployeeCode = employeeCode ?? string.Empty,
            MainAccountCode = mainCode ?? string.Empty,
            MainAccountName = mainName ?? string.Empty,
            SubAccountCode = subCode ?? string.Empty,
            SubAccountName = subName ?? string.Empty,
            Description = description ?? string.Empty,
            Division = divisionCode ?? string.Empty,
            Department = departmentCode ?? string.Empty,
            Analysis1 = analysis1Code ?? string.Empty,
            Analysis2 = string.Empty,
            Activity1 = string.Empty,
            Activity2 = string.Empty,
            Currency = currency ?? string.Empty,
            // Legacy: dr[Fcamount] = dr[Netamount] = Math.Abs(amount) on every branch except EmployeeWise
            // BreakDown, which zeroes negative amounts instead -- see PayrollJVCustom042EmployeeWiseBreak
            // DownReportAppService.NewDebitRow, which does NOT call this helper.
            FcAmount = Math.Abs(amount),
            NetAmount = Math.Abs(amount),
            DrCr = drCr
        };
    }

    protected static IEnumerable<IGrouping<(int? AccountInfoId, int? OffsetAccountInfoId, int? CostElement1Id,
            int? CostElement2Id, int? CostElement3Id, string Currency), PayrollJVPostingLine>>
        GroupByAccountAndCostElements(IEnumerable<PayrollJVPostingLine> lines)
    {
        return lines.GroupBy(l => (l.AccountInfoId, l.OffsetAccountInfoId, l.CostElement1Id, l.CostElement2Id,
            l.CostElement3Id, Currency: "KD"));
    }

    /// <summary>
    /// The Main/Sub/Div/Dept/Anly1/Anly2/Act1/Act2/Currency/Fc-amount/Net-amount/Dr-Cr export column set
    /// shared by every "voucher-shaped" Custom 042 format. EmployeeWise BreakDown prepends its own
    /// EmployeeCode column rather than reusing this list verbatim.
    /// </summary>
    protected static List<HCMv3.GridReports.Exporting.GridReportColumn<PayrollJVCustom042ReportRowDto>>
        GetPostingExportColumns()
    {
        return new List<HCMv3.GridReports.Exporting.GridReportColumn<PayrollJVCustom042ReportRowDto>>
        {
            new("SrNo", r => r.LineNumber),
            new("Main", r => r.MainAccountCode),
            new("MainDescription", r => r.MainAccountName),
            new("Sub", r => r.SubAccountCode),
            new("SubDescription", r => r.SubAccountName),
            new("Description", r => r.Description),
            new("Division", r => r.Division),
            new("Department", r => r.Department),
            new("Analysis1", r => r.Analysis1),
            new("Analysis2", r => r.Analysis2),
            new("Activity1", r => r.Activity1),
            new("Activity2", r => r.Activity2),
            new("Currency", r => r.Currency),
            new("FcAmount", r => r.FcAmount),
            new("NetAmount", r => r.NetAmount),
            new("DrCr", r => r.DrCr)
        };
    }
}
