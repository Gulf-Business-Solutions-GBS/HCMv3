using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.Runtime.Caching;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.SystemSecurity;
using NSubstitute;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Shared seeding and wiring for the Costing File / Post JV tests: a payroll area with both FI links, employees,
/// cost registers, and a real <see cref="FinanceRunLog"/> over the costing Sqlite context.
/// </summary>
public abstract class FinancePostingTestBase : CostingTestBase
{
    protected const string AreaName = "PA";

    /// <summary>Already the last day of the month, as the screens send it after normalising.</summary>
    protected static readonly DateTime PayInDate = new DateTime(2026, 9, 30);

    protected static readonly DateTime PostingDate = new DateTime(2026, 9, 30);

    protected int TenantId;
    protected int NationalityId;
    protected int PayrollAreaId;

    protected void SeedArea(bool withFiLinks = true)
    {
        TenantId = AbpSession.TenantId ?? 1;

        var currency = new Currency
        {
            TenantId = TenantId, Code = "KWD", NameEn = "Kuwaiti Dinar", NameAr = "دينار",
            CurrencyShortFormEn = "KWD", CurrencyShortFormAr = "د.ك", DecimalCount = 3
        };
        Context.Currencies.Add(currency);

        var bankType = new AccountType { TenantId = TenantId, Code = "B", NameEn = "Bank", NameAr = "Bank" };
        Context.AccountTypes.Add(bankType);
        Context.SaveChanges();

        var bankAccount = new AccountInfo
        { TenantId = TenantId, Code = "1001", NameEn = "Payroll Bank", NameAr = "Bank", AccountTypeId = bankType.Id };
        Context.AccountInfos.Add(bankAccount);

        var company = new CostElement { TenantId = TenantId, Code = "TGTC", NameEn = "Company", NameAr = "Company" };
        Context.CostElements.Add(company);

        var payrollPeriod = new PayrollPeriod { TenantId = TenantId, Code = "Jan-Dec", NameEn = "Jan-Dec", NameAr = "Jan-Dec" };
        Context.PayrollPeriods.Add(payrollPeriod);

        var nationality = new Nationality { TenantId = TenantId, Code = "KW", NameEn = "Kuwaiti", NameAr = "كويتي" };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var area = new PayrollArea
        {
            TenantId = TenantId,
            Code = "PA1",
            Name = AreaName,
            PayrollPeriodId = payrollPeriod.Id,
            CurrencyId = currency.Id,
            CutOffDay = 25,
            FICompanyId = withFiLinks ? company.Id : null,
            FIBankId = withFiLinks ? bankAccount.Id : null
        };
        Context.PayrollAreas.Add(area);
        Context.SaveChanges();

        NationalityId = nationality.Id;
        PayrollAreaId = area.Id;
    }

    protected int AddEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        var employee = new Employee
        {
            TenantId = TenantId, Code = code,
            FirstNameEn = "Emp", LastNameEn = code, FirstNameAr = "Emp", LastNameAr = code,
            FullNameEn = "Emp " + code, FullNameAr = "موظف " + code,
            NationalityId = NationalityId,
            Status = status
        };
        Context.Employees.Add(employee);
        Context.SaveChanges();
        return employee.Id;
    }

    protected void AddCostRegister(int employeeId, PayrollRunType runType, DateTime? period = null)
    {
        Context.CostRegisters.Add(new CostRegister
        {
            TenantId = TenantId, EmployeeId = employeeId, Period = period ?? PayInDate, PayrollRunType = runType
        });
        Context.SaveChanges();
    }

    protected FinanceRunLog CreateRunLog()
    {
        return new FinanceRunLog(
            Repo<ProjectLog>(),
            Repo<ExecutionLog>(),
            Repo<Employee>(),
            Substitute.For<IUnitOfWorkManager>(),
            LocalIocManager.Resolve<ICacheManager>(),
            AbpSession)
        {
            LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>()
        };
    }

    /// <summary>The text a localization key resolves to, so assertions survive the key being added to the XML.</summary>
    protected string Text(string key)
    {
        return LocalIocManager.Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, key);
    }

    protected static CostingRunInput Input(PayrollRunType runType = PayrollRunType.Regular, string employeeCode = "") =>
        new CostingRunInput
        {
            PayrollAreaName = AreaName,
            EmployeeCode = employeeCode,
            PayInDate = PayInDate,
            PostingDate = PostingDate,
            PayrollRunType = runType
        };

    protected static JournalVoucherLine Line(string empCode, double debit, double credit, string billingType = "Earning",
                                             string description = "Basic Salary")
    {
        return new JournalVoucherLine
        {
            EmpCode = empCode,
            CompanyAccounts = "TGTC",
            LineNumberAccountType = "Ledger",
            LedgerDimensionMainAccount = "7101",
            Debit = debit,
            Credit = credit,
            BillingType = billingType,
            Description = description,
            TransDate = PostingDate
        };
    }

    protected static JournalVoucherResult Voucher(params JournalVoucherLine[] lines)
    {
        var result = new JournalVoucherResult();
        result.Lines.AddRange(lines);
        return result;
    }

    protected List<ProjectLog> LogRows() => Context.ProjectLogs.OrderBy(l => l.Id).ToList();
}
