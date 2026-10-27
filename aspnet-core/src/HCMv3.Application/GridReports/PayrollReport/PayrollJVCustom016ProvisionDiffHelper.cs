using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PayrollManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// SHARED core of legacy <c>uscRptPayrollJV_Custom016.ascx.cs</c>, <c>Table1()</c>, the "Leave Provision"
/// (lines 2397-2456, wage type "3001") and "Indemnity Provision" (lines 2459-2518, wage type "3000")
/// branches -- structurally identical except for the single wage-type code they diff, so both
/// <see cref="PayrollJVCustom016LeaveProvisionReportAppService"/> and
/// <see cref="PayrollJVCustom016IndemnityProvisionReportAppService"/> call this instead of duplicating it.
///
/// For every employee in the selected payroll area with a non-zero Regular-run "provision" wage-type
/// amount, this lists the ones whose employee code did NOT show up carrying that same wage type anywhere
/// in the <see cref="PayrollJVCustom016PostingLineLoader"/> output for the same period/filters -- i.e. the
/// provision amount that "fell out" of the JV. Legacy never re-joins a matched amount back in (its
/// <c>dsNotInJV</c> is the whole output), so every emitted row's AmountJV is 0 by construction.
/// </summary>
internal static class PayrollJVCustom016ProvisionDiffHelper
{
    public static async Task<List<PayrollJVCustom016ProvisionDiffRowDto>> BuildRowsAsync(
        string wageTypeCode,
        GetPayrollJVCustom016ProvisionDiffReportInput input,
        DateTime today,
        PayrollJVCustom016PostingLineLoader loader,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IReadOnlyCollection<int> rlsEmployeeIds)
    {
        var rows = new List<PayrollJVCustom016ProvisionDiffRowDto>();

        // Legacy line 595: the whole branch is gated on a resolved payroll area.
        if (string.IsNullOrWhiteSpace(input.PayrollAreaCode))
        {
            return rows;
        }

        var area = await payrollAreaRepository.GetAll()
            .Where(a => a.Code == input.PayrollAreaCode)
            .Select(a => new { a.Id })
            .FirstOrDefaultAsync();
        if (area == null)
        {
            return rows;
        }

        var endDate = ResolveEndDate(input, today);
        var employeeCodes = input.ResolveEmployeeCodes();

        // Legacy's common preprocessing pipeline (lines 573-1168), used ONLY to compute which employees
        // already carry this wage type in the JV lines (legacy line 2439).
        var ds = await loader.LoadAsync(endDate, area.Id, input.PayrollRunType, employeeCodes, rlsEmployeeIds);
        var employeesInJv = ds
            .Where(l => l.WageTypeCode == wageTypeCode)
            .Select(l => l.EmployeeCode)
            .ToHashSet();

        // Legacy lines 2421-2435: dsProv is NOT narrowed by employee code (only by period/run type/wage
        // type/payroll area) -- deliberately not applying employeeCodes here to match.
        var rlsIds = (rlsEmployeeIds ?? Array.Empty<int>()).ToArray();
        var provisionLines = await salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.SalaryRegister.PayrollRunType == PayrollRunType.Regular
                        && d.WageType.Code == wageTypeCode
                        && d.Amount != 0)
            .Where(d => employeeOrgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= endDate && o.EndDate >= endDate
                     && o.PayrollAreaId == area.Id))
            // DIVERGENCE (agreed for this wave): see PayrollJVCustom016PostingLineLoader remarks -- RLS
            // applied although legacy never did.
            .WhereIf(rlsIds.Length > 0, d => rlsIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new { EmployeeCode = d.SalaryRegister.Employee.Code, d.Amount })
            .ToListAsync();

        var rowNumber = 0;
        foreach (var line in provisionLines.Where(l => !employeesInJv.Contains(l.EmployeeCode)))
        {
            rowNumber++;
            rows.Add(new PayrollJVCustom016ProvisionDiffRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                EmployeeCode = line.EmployeeCode,
                AmountReg = line.Amount,
                AmountJV = 0,
                Difference = line.Amount
            });
        }

        return rows;
    }

    /// <summary>Legacy <c>GetTable()</c>: blank month/year mean "now"; an invalid pair fell back to 1900.</summary>
    public static DateTime ResolveEndDate(GetPayrollJVCustom016ProvisionDiffReportInput input, DateTime today)
    {
        var month = input.Month ?? today.Month;
        var year = input.Year ?? today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(1900, 1, 31).Date;
        }
    }

    /// <summary>Legacy <c>GetParameterStr()</c>: Payroll Area | Run Type | Month | Year | Type | Emp Code.</summary>
    public static string BuildJvParameterString(
        GetPayrollJVCustom016ProvisionDiffReportInput input, DateTime today, string typeLabel)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaCode))
        {
            parts.Add("Payroll Area : " + input.PayrollAreaCode);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? today.Month));
        parts.Add("Year : " + (input.Year ?? today.Year));
        parts.Add("Type : " + typeLabel);

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }
}
