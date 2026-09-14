using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Organizations;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// What "Load Employee Data" loaded before, in the grid's own shape, so coming back to the step shows the sheet
/// as it was: the company, and each employee in the payroll area with code, name, joining date, civil ID and
/// the salary columns. Read back from the records the load made -- not from a saved copy of the grid -- so an
/// employee corrected on the employee screen shows as corrected.
/// </summary>
public class GoLiveV2LoadedEmployeesReader : ITransientDependency
{
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _assignmentRepository;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<OrganizationUnit, long> _orgUnitRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;

    public GoLiveV2LoadedEmployeesReader(
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> assignmentRepository,
        IRepository<EmployeeBasicPay> basicPayRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<OrganizationUnit, long> orgUnitRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<TimeTransfer> timeTransferRepository)
    {
        _payrollAreaResolver = payrollAreaResolver;
        _employeeRepository = employeeRepository;
        _assignmentRepository = assignmentRepository;
        _basicPayRepository = basicPayRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _orgUnitRepository = orgUnitRepository;
        _employeeBankRepository = employeeBankRepository;
        _timeTransferRepository = timeTransferRepository;
    }

    public async Task<GoLiveV2LoadedEmployeesOutput> ReadAsync(string payrollAreaName)
    {
        var output = new GoLiveV2LoadedEmployeesOutput();
        if (string.IsNullOrWhiteSpace(payrollAreaName))
        {
            return output;
        }

        // Everyone in the area on the open-ended end of their assignment -- the load's own assignments never end.
        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName.Trim(), new DateTime(9999, 12, 31));
        if (ids.Count == 0)
        {
            return output;
        }

        var employees = await _employeeRepository.GetAll()
            .Where(e => ids.Contains(e.Id))
            .OrderBy(e => e.Code)
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.JoiningDate, e.CivilID })
            .ToListAsync();

        output.OrganizationUnitName = await CompanyNameAsync(ids);

        // The latest pay record of each, and its lines by wage code.
        var pays = (await _basicPayRepository.GetAll()
                .Where(p => ids.Contains(p.EmployeeId))
                .Select(p => new { p.Id, p.EmployeeId, p.StartDate })
                .ToListAsync())
            .GroupBy(p => p.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.StartDate).First().Id);

        var payIds = pays.Values.ToList();
        var lines = (await _basicPayDetailRepository.GetAll()
                .Where(d => payIds.Contains(d.EmployeeBasicPayId))
                .Select(d => new { d.EmployeeBasicPayId, d.WageType.Code, d.Amount })
                .ToListAsync())
            .ToLookup(d => d.EmployeeBasicPayId);

        // The latest bank record of each -- the one a load keeps.
        var banks = (await _employeeBankRepository.GetAll()
                .Where(b => ids.Contains(b.EmployeeId))
                .Select(b => new { b.EmployeeId, b.StartDate, BankName = b.Bank.NameEn, b.IBAN, b.AccountNum })
                .ToListAsync())
            .GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.StartDate).First());

        // The opening balance each was loaded with -- the transfer this setup keeps.
        var openingBalances = (await _timeTransferRepository.GetAll()
                .Where(t => ids.Contains(t.EmployeeId) && t.Comments == GoLiveV2OpeningBalances.Comment)
                .Select(t => new { t.EmployeeId, t.Id, t.TransferCount })
                .ToListAsync())
            .GroupBy(t => t.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.Id).First().TransferCount);

        foreach (var e in employees)
        {
            var row = new GoLiveV2EmployeeRowDto
            {
                Code = e.Code,
                FullName = e.FullNameEn,
                JoiningDate = e.JoiningDate,
                CivilId = e.CivilID == 0 ? null : e.CivilID.ToString(),
            };

            if (pays.TryGetValue(e.Id, out var payId))
            {
                var amounts = lines[payId].GroupBy(l => l.Code).ToDictionary(g => g.Key, g => g.Sum(l => l.Amount));
                row.BasicSalary = Amount(amounts, "1000");
                row.AccommodationAllowance = Amount(amounts, "1027");
                row.MobileAllowance = Amount(amounts, "1011");
                row.TransportAllowance = Amount(amounts, "1002");
                row.FoodAllowance = Amount(amounts, "1038");
                row.HousingAllowance = Amount(amounts, "1001");
                row.OtherAllowance = Amount(amounts, "1018");
            }

            if (openingBalances.TryGetValue(e.Id, out var openingBalance))
            {
                row.AnnualLeaveOpeningBalance = openingBalance;
            }

            if (banks.TryGetValue(e.Id, out var bank))
            {
                row.BankName = bank.BankName;
                row.Iban = bank.IBAN;
                row.AccountNumber = bank.AccountNum;
            }

            output.Employees.Add(row);
        }

        return output;
    }

    /// <summary>The organisation unit most of the area's employees are assigned to -- for a V2 load, the company.</summary>
    private async Task<string> CompanyNameAsync(List<int> employeeIds)
    {
        var unitId = (await _assignmentRepository.GetAll()
                .Where(a => employeeIds.Contains(a.EmployeeId) && a.OrganizationUnitId != null)
                .Select(a => a.OrganizationUnitId.Value)
                .ToListAsync())
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .Select(g => (long?)g.Key)
            .FirstOrDefault();

        if (unitId == null)
        {
            return null;
        }

        return (await _orgUnitRepository.FirstOrDefaultAsync(unitId.Value))?.DisplayName;
    }

    private static double? Amount(IReadOnlyDictionary<string, double> amounts, string wageCode) =>
        amounts.TryGetValue(wageCode, out var amount) ? amount : null;
}
