using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.Dto;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// Step 3, what the month's payroll paid -- read from the salary registers the engine wrote, the month's
/// CURRENT regular results only: a re-run leaves the earlier results behind as "last" and "archive", and
/// showing them too would count everyone twice.
///
/// <para>The per-employee view and the grid use the month's own register (paid for the month it is paid in).
/// The Excel of register lines takes every current register paid in the month, retro months included, with
/// the period each one is for -- that is what is set line by line beside the legacy payroll.</para>
/// </summary>
public class GoLiveV2PayrollResultsReader : AbpServiceBase, ITransientDependency
{
    private const string NetPayCode = "9000";

    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<SalaryRegister> _registerRepository;
    private readonly IRepository<SalaryRegisterDetail> _detailRepository;
    private readonly IRepository<SalaryRegisterDaysDetail> _daysRepository;
    private readonly IGridReportExcelExporter _excelExporter;

    public GoLiveV2PayrollResultsReader(
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IRepository<Employee> employeeRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<SalaryRegister> registerRepository,
        IRepository<SalaryRegisterDetail> detailRepository,
        IRepository<SalaryRegisterDaysDetail> daysRepository,
        IGridReportExcelExporter excelExporter)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _payrollAreaResolver = payrollAreaResolver;
        _employeeRepository = employeeRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _registerRepository = registerRepository;
        _detailRepository = detailRepository;
        _daysRepository = daysRepository;
        _excelExporter = excelExporter;
    }

    public async Task<GoLiveV2PayrollResultsOutput> GetResultsAsync(string payrollAreaName, DateTime payInDate)
    {
        var output = new GoLiveV2PayrollResultsOutput { PayInDate = payInDate };
        var areaName = payrollAreaName.Trim();

        output.PayrollAreaId = (await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == areaName))?.Id;

        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(areaName, payInDate);
        if (ids.Count == 0)
        {
            return output;
        }

        var employees = await _employeeRepository.GetAll()
            .Where(e => ids.Contains(e.Id))
            .OrderBy(e => e.Code)
            .Select(e => new { e.Id, e.Code, e.FullNameEn })
            .ToListAsync();

        var registers = await CurrentRegisters(payInDate, ownMonthOnly: true)
            .Where(r => ids.Contains(r.EmployeeId))
            .Select(r => new { r.Id, r.EmployeeId })
            .ToListAsync();
        var registerByEmployee = registers.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToList());

        var registerIds = registers.Select(r => r.Id).ToList();
        var lines = (await _detailRepository.GetAll()
                .Where(d => registerIds.Contains(d.SalaryRegisterId))
                .Select(d => new { d.SalaryRegisterId, d.WageType.Code, d.WageType.BillingType, d.Amount })
                .ToListAsync())
            .ToLookup(d => d.SalaryRegisterId);

        foreach (var e in employees)
        {
            var row = new GoLiveV2PayrollResultRowDto { EmployeeId = e.Id, EmployeeCode = e.Code, EmployeeName = e.FullNameEn };

            if (registerByEmployee.TryGetValue(e.Id, out var own))
            {
                var mine = own.SelectMany(id => lines[id]).ToList();
                row.HasRegister = true;
                row.NetPay = mine.Where(l => l.Code == NetPayCode).Sum(l => l.Amount);
                row.Earnings = mine.Where(l => l.BillingType == BillingType.Earning && l.Code != NetPayCode).Sum(l => l.Amount);
                row.Deductions = mine.Where(l => l.BillingType == BillingType.Deduction).Sum(l => l.Amount);
            }

            output.Employees.Add(row);
        }

        output.EmployeesPaid = output.Employees.Count(e => e.HasRegister);
        output.TotalEarnings = output.Employees.Sum(e => e.Earnings);
        output.TotalDeductions = output.Employees.Sum(e => e.Deductions);
        output.TotalNetPay = output.Employees.Sum(e => e.NetPay);

        return output;
    }

    public async Task<GoLiveV2SalaryRegisterDto> GetRegisterAsync(int employeeId, DateTime payInDate)
    {
        var employee = await _employeeRepository.GetAsync(employeeId);
        var dto = new GoLiveV2SalaryRegisterDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullNameEn,
            ForPeriod = payInDate,
            InPeriod = payInDate,
        };

        var registerIds = await CurrentRegisters(payInDate, ownMonthOnly: true)
            .Where(r => r.EmployeeId == employeeId)
            .Select(r => r.Id)
            .ToListAsync();
        if (registerIds.Count == 0)
        {
            return dto;
        }

        dto.Lines = await _detailRepository.GetAll()
            .Where(d => registerIds.Contains(d.SalaryRegisterId))
            .OrderBy(d => d.WageType.BillingType)
            .ThenBy(d => d.WageType.Code)
            .Select(d => new GoLiveV2SalaryRegisterLineDto
            {
                WageCode = d.WageType.Code,
                WageName = d.WageType.NameEn,
                Group = d.WageType.BillingType,
                Rate = d.Rate,
                Number = d.Number,
                Amount = d.Amount,
            })
            .ToListAsync();

        dto.Days = await _daysRepository.GetAll()
            .Where(x => registerIds.Contains(x.SalaryRegisterDetail.SalaryRegisterId))
            .OrderBy(x => x.SalaryRegisterDetail.WageType.Code)
            .ThenBy(x => x.Period)
            .Select(x => new GoLiveV2SalaryRegisterDaysDto
            {
                WageCode = x.SalaryRegisterDetail.WageType.Code,
                WageName = x.SalaryRegisterDetail.WageType.NameEn,
                Period = x.Period,
                Rate = x.Rate,
                Number = x.Number,
                Amount = x.Amount,
            })
            .ToListAsync();

        return dto;
    }

    /// <summary>Every register line paid in the month, one row each -- for a lookup against the legacy payroll.</summary>
    public async Task<FileDto> ExportRegisterLinesAsync(string payrollAreaName, DateTime payInDate)
    {
        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName.Trim(), payInDate);

        var registerIds = await CurrentRegisters(payInDate, ownMonthOnly: false)
            .Where(r => ids.Contains(r.EmployeeId))
            .Select(r => r.Id)
            .ToListAsync();

        var rows = await _detailRepository.GetAll()
            .Where(d => registerIds.Contains(d.SalaryRegisterId))
            .Select(d => new RegisterLine
            {
                EmployeeCode = d.SalaryRegister.Employee.Code,
                EmployeeName = d.SalaryRegister.Employee.FullNameEn,
                ForPeriod = d.SalaryRegister.ForPeriod,
                InPeriod = d.SalaryRegister.InPeriod,
                WageCode = d.WageType.Code,
                WageName = d.WageType.NameEn,
                Group = d.WageType.BillingType,
                Rate = d.Rate,
                Number = d.Number,
                Amount = d.Amount,
            })
            .ToListAsync();

        rows = rows.OrderBy(r => r.EmployeeCode).ThenBy(r => r.ForPeriod).ThenBy(r => r.WageCode).ToList();

        var columns = new List<GridReportColumn<RegisterLine>>
        {
            new("GoLiveV2ColCode", r => r.EmployeeCode),
            new("GoLiveV2ColFullName", r => r.EmployeeName),
            new("GoLiveV2RegisterForPeriod", r => r.ForPeriod),
            new("GoLiveV2RegisterInPeriod", r => r.InPeriod),
            new("GoLiveV2RegisterWageCode", r => r.WageCode),
            new("GoLiveV2RegisterWageType", r => r.WageName),
            new("GoLiveV2RegisterGroup", r => L("GoLiveV2RegisterGroup" + r.Group)),
            new("GoLiveV2RegisterRate", r => r.Rate),
            new("GoLiveV2RegisterNumber", r => r.Number),
            new("GoLiveV2RegisterAmount", r => r.Amount),
        };

        return await _excelExporter.ExportAsync($"Salary Register {payInDate:yyyy-MM}.xlsx", rows, columns);
    }

    private IQueryable<SalaryRegister> CurrentRegisters(DateTime payInDate, bool ownMonthOnly) =>
        _registerRepository.GetAll().Where(r =>
            r.InPeriod == payInDate
            && r.PayrollRunType == PayrollRunType.Regular
            && r.PayrollResultType == PayrollResultType.Current
            && (!ownMonthOnly || r.ForPeriod == payInDate));

    private class RegisterLine
    {
        public string EmployeeCode { get; init; }

        public string EmployeeName { get; init; }

        public DateTime ForPeriod { get; init; }

        public DateTime InPeriod { get; init; }

        public string WageCode { get; init; }

        public string WageName { get; init; }

        public BillingType Group { get; init; }

        public double Rate { get; init; }

        public double Number { get; init; }

        public double Amount { get; init; }
    }
}
