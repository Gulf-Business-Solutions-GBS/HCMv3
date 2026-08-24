using System;
using HCMv3.OrganizationManagement;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.EntityFrameworkCore;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Shared arrangement helpers for the customer-specific Journal Voucher grid-report tests
/// (<c>PayrollJVCustom019/023</c>). Those two reports are driven by the same three tables --
/// <c>SalaryRegister(Detail)</c>, <c>WageType</c> and <c>EmployeeWageAccount</c> -- so the arrangement
/// lives here once instead of being copied per suite.
///
/// It is a plain helper rather than a test base class so each suite keeps its own
/// <see cref="AppTestBase"/> inheritance and its own service resolution.
/// </summary>
public class PayrollJVReportTestDataBuilder
{
    private readonly Action<Action<HCMv3DbContext>> _usingDbContext;
    private readonly int _tenantId;

    public PayrollJVReportTestDataBuilder(Action<Action<HCMv3DbContext>> usingDbContext, int tenantId)
    {
        _usingDbContext = usingDbContext;
        _tenantId = tenantId;
    }

    public Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        _usingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = _tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        _usingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = _tenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    /// <summary>
    /// A wage type that participates in FI posting -- the JV reports require
    /// <c>IncludeInFIPosting == true</c>, so it defaults to true here.
    /// </summary>
    public WageType CreateWageType(
        string code,
        BillingType billingType = BillingType.Earning,
        bool includeInFiPosting = true,
        bool dontImpactNetPay = false)
    {
        WageType wageType = null;
        _usingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = _tenantId,
                Code = code,
                NameEn = "WT " + code,
                NameAr = "WT " + code,
                ShortNameEn = code,
                ShortNameAr = code,
                BillingType = billingType,
                IncludeInFIPosting = includeInFiPosting,
                DontImpactNetPay = dontImpactNetPay,
                PayrollOrder = 1,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    public SalaryRegister CreateRegister(
        Employee employee,
        DateTime forPeriod,
        DateTime? inPeriod = null,
        PayrollRunType runType = PayrollRunType.Regular)
    {
        SalaryRegister register = null;
        _usingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = _tenantId,
                EmployeeId = employee.Id,
                ForPeriod = forPeriod,
                InPeriod = inPeriod ?? forPeriod,
                PayrollRunType = runType,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = forPeriod
            }).Entity;
        });

        return register;
    }

    public void CreateDetail(SalaryRegister register, WageType wageType, double amount)
    {
        _usingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = _tenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Rate = 0,
                Number = 0
            });
        });
    }

    public AccountInfo CreateAccount(string code, string nameEn)
    {
        AccountType accountType = null;
        _usingDbContext(context =>
        {
            accountType = context.Set<AccountType>().Add(new AccountType
            {
                TenantId = _tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Ledger",
                NameAr = "Ledger"
            }).Entity;
        });

        AccountInfo account = null;
        _usingDbContext(context =>
        {
            account = context.Set<AccountInfo>().Add(new AccountInfo
            {
                TenantId = _tenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = nameEn,
                AccountTypeId = accountType.Id
            }).Entity;
        });

        return account;
    }

    public CostElement CreateCostElement(string code)
    {
        CostElement costElement = null;
        _usingDbContext(context =>
        {
            costElement = context.Set<CostElement>().Add(new CostElement
            {
                TenantId = _tenantId,
                Code = code,
                NameEn = "CE " + code,
                NameAr = "CE " + code
            }).Entity;
        });

        return costElement;
    }

    /// <summary>
    /// The <c>EmployeeWageAccount</c> mapping the JV reports left-join to. A wage type with NO such row
    /// is exactly what the "Missing Accounts" format lists.
    /// </summary>
    public void CreateWageAccount(
        Employee employee,
        WageType wageType,
        AccountInfo account,
        AccountInfo offsetAccount = null,
        CostElement costElement1 = null,
        CostElement costElement2 = null,
        CostElement costElement3 = null,
        CostElement costElement4 = null,
        double costPercentage = 100,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _usingDbContext(context =>
        {
            context.Set<EmployeeWageAccount>().Add(new EmployeeWageAccount
            {
                TenantId = _tenantId,
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                StartDate = startDate ?? new DateTime(2000, 1, 1),
                EndDate = endDate ?? new DateTime(2099, 12, 31),
                AccountInfoId = account.Id,
                OffsetAccountInfoId = offsetAccount?.Id,
                CostElement1Id = costElement1?.Id,
                CostElement2Id = costElement2?.Id,
                CostElement3Id = costElement3?.Id,
                CostElement4Id = costElement4?.Id,
                CostPercentage = costPercentage
            });
        });
    }

    public PayrollArea CreatePayrollArea(string code, string name)
    {
        Currency currency = null;
        _usingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = _tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Dinar",
                NameAr = "Dinar",
                DecimalCount = 3,
                CurrencyShortFormEn = "KWD",
                CurrencyShortFormAr = "KWD",
                CurrencyDenominationFormEn = "Fils",
                CurrencyDenominationFormAr = "Fils"
            }).Entity;
        });

        PayrollPeriod period = null;
        _usingDbContext(context =>
        {
            period = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = _tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Monthly",
                NameAr = "Monthly"
            }).Entity;
        });

        PayrollArea payrollArea = null;
        _usingDbContext(context =>
        {
            payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = _tenantId,
                Code = code,
                Name = name,
                CurrencyId = currency.Id,
                PayrollPeriodId = period.Id,
                CutOffDay = 25
            }).Entity;
        });

        return payrollArea;
    }

    public void CreateOrgAssignment(Employee employee, PayrollArea payrollArea, DateTime start, DateTime end)
    {
        _usingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = _tenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                PayrollAreaId = payrollArea?.Id
            });
        });
    }
}
