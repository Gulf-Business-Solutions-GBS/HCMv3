using System;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.MultiTenancy;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;

namespace HCMv3.Tests.SetupChecks;

/// <summary>
/// Builds tenants for the setup check tests straight in the database: a fresh, empty tenant per test (so nothing the
/// default tenant was seeded with leaks in), then exactly the setup a test asks for. Dates run "since 2020 to 9999",
/// so every record covers whatever today is.
/// </summary>
public abstract class TenantSetupCheckTestBase : AppTestBase
{
    protected static readonly DateTime Since = new(2020, 1, 1);
    protected static readonly DateTime Forever = new(9999, 12, 31);

    /// <summary>The parts of a fully set-up employee, so a test can leave exactly one out.</summary>
    [Flags]
    protected enum EmployeeParts
    {
        None = 0,
        Contract = 1,
        OrgAssignment = 2,
        PayrollAreaOnAssignment = 4,
        GroupAndSubGroup = 8,
        SalaryRecord = 16,
        PayrollStatus = 32,
        DutyPeriod = 64,
        TimePolicy = 128,
        BankWithIban = 256,
        All = Contract | OrgAssignment | PayrollAreaOnAssignment | GroupAndSubGroup | SalaryRecord | PayrollStatus
              | DutyPeriod | TimePolicy | BankWithIban,
    }

    protected int CreateEmptyTenant(string tenancyName)
    {
        return UsingDbContext(null, context =>
        {
            var tenant = context.Tenants.Add(new Tenant(tenancyName, tenancyName + " Co") { IsActive = true }).Entity;
            context.SaveChanges();
            return tenant.Id;
        });
    }

    /// <summary>A payroll area with its control record and a calendar row covering today, plus the four settings rows
    /// and one leave-pay link: every PAY check passes.</summary>
    protected int CreateCompletePayrollSetup(int tenantId, string areaCode = "MAIN")
    {
        var areaId = CreatePayrollArea(tenantId, areaCode);
        UsingDbContext(tenantId, context =>
        {
            context.Set<PayrollControlRecord>().Add(new PayrollControlRecord
            {
                TenantId = tenantId, PayrollAreaId = areaId, PayrollMonth = 1, PayrollYear = 2020,
                CalendarMonthStart = Since, CalendarMonthEnd = new DateTime(2020, 1, 31), PeriodLocked = true,
            });
            context.Set<PayrollCalendar>().Add(new PayrollCalendar
            {
                TenantId = tenantId, PayrollAreaId = areaId, PayrollMonth = 1, PayrollYear = 2020, StartDate = Since, EndDate = Forever,
            });
        });
        CreateSettingsRows(tenantId);
        CreateLeavePayLink(tenantId);
        return areaId;
    }

    protected int CreatePayrollArea(int tenantId, string code)
    {
        return UsingDbContext(tenantId, context =>
        {
            var period = context.Set<PayrollPeriod>().Add(new PayrollPeriod { TenantId = tenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            var currency = context.Set<Currency>().Add(new Currency { TenantId = tenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            context.SaveChanges();
            var area = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = tenantId, Code = code, Name = code, PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 31,
            }).Entity;
            context.SaveChanges();
            return area.Id;
        });
    }

    protected void CreateSettingsRows(int tenantId, bool time = true, bool payroll = true, bool personnel = true, bool security = true)
    {
        UsingDbContext(tenantId, context =>
        {
            if (time) context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = tenantId });
            if (payroll) context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings { TenantId = tenantId });
            if (personnel) context.Set<ManagePersonnelSettings>().Add(new ManagePersonnelSettings { TenantId = tenantId });
            if (security) context.Set<ManageSecuritySettings>().Add(new ManageSecuritySettings { TenantId = tenantId });
        });
    }

    protected void CreateLeavePayLink(int tenantId)
    {
        UsingDbContext(tenantId, context =>
        {
            var wageType = context.Set<WageType>().Add(new WageType { TenantId = tenantId, Code = "1000", NameEn = "Basic", NameAr = "Basic" }).Entity;
            var absenceType = context.Set<AbsenceType>().Add(new AbsenceType { TenantId = tenantId, Code = "1", NameEn = "Annual", NameAr = "Annual" }).Entity;
            context.SaveChanges();
            context.Set<WageTypeLeave>().Add(new WageTypeLeave { TenantId = tenantId, WageTypeId = wageType.Id, AbsenceTypeId = absenceType.Id });
        });
    }

    /// <summary>An active employee joined in 2020 with the given parts; returns the employee id.</summary>
    protected int CreateEmployee(
        int tenantId,
        string code,
        int payrollAreaId,
        EmployeeParts parts = EmployeeParts.All,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? joiningDate = null,
        PaymentType paymentType = PaymentType.Bank,
        long? organizationUnitId = null)
    {
        return UsingDbContext(tenantId, context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality { TenantId = tenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = tenantId, Code = code, FirstNameEn = code, LastNameEn = code, FirstNameAr = code, LastNameAr = code,
                FullNameEn = code, FullNameAr = code, BirthDate = new DateTime(1990, 1, 1), JoiningDate = joiningDate ?? Since,
                Email = code + "@example.com", Status = status, NationalityId = nationality.Id,
            }).Entity;
            context.SaveChanges();
            var id = employee.Id;

            if (parts.HasFlag(EmployeeParts.Contract))
            {
                var contractType = context.Set<ContractType>().Add(new ContractType { TenantId = tenantId, Code = code, NameEn = code, NameAr = code }).Entity;
                context.SaveChanges();
                context.Set<EmployeeContract>().Add(new EmployeeContract
                {
                    TenantId = tenantId, EmployeeId = id, ContractTypeId = contractType.Id, StartDate = Since, EndDate = Forever,
                });
            }

            if (parts.HasFlag(EmployeeParts.OrgAssignment))
            {
                int? groupId = null, subGroupId = null;
                if (parts.HasFlag(EmployeeParts.GroupAndSubGroup))
                {
                    var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup { TenantId = tenantId, Code = code, NameEn = code, NameAr = code }).Entity;
                    context.SaveChanges();
                    var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
                    {
                        TenantId = tenantId, EmployeeGroupId = group.Id, Code = code, NameEn = code, NameAr = code,
                    }).Entity;
                    context.SaveChanges();
                    groupId = group.Id;
                    subGroupId = subGroup.Id;
                }

                context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
                {
                    TenantId = tenantId, EmployeeId = id, StartDate = Since, EndDate = Forever,
                    PayrollAreaId = parts.HasFlag(EmployeeParts.PayrollAreaOnAssignment) ? payrollAreaId : null,
                    EmployeeGroupId = groupId, EmployeeSubGroupId = subGroupId, OrganizationUnitId = organizationUnitId,
                });
            }

            if (parts.HasFlag(EmployeeParts.SalaryRecord))
            {
                context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay { TenantId = tenantId, EmployeeId = id, StartDate = Since, EndDate = Forever, Amount = 1 });
            }

            if (parts.HasFlag(EmployeeParts.PayrollStatus))
            {
                context.Set<EmployeePayrollStatus>().Add(new EmployeePayrollStatus
                {
                    TenantId = tenantId, EmployeeId = id, EarliestRetroAccountingDate = Since, NextTimeEvaluationFrom = Since,
                });
            }

            if (parts.HasFlag(EmployeeParts.DutyPeriod))
            {
                var dutyPeriod = context.Set<DutyPeriod>().Add(new DutyPeriod
                {
                    TenantId = tenantId, Code = code, NameEn = code, NameAr = code, StartDate = Since, EndDate = Forever,
                }).Entity;
                context.SaveChanges();
                context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
                {
                    TenantId = tenantId, EmployeeId = id, DutyPeriodId = dutyPeriod.Id, StartDate = Since, EndDate = Forever,
                });
            }

            if (parts.HasFlag(EmployeeParts.TimePolicy))
            {
                var policy = context.Set<TimePolicy>().Add(new TimePolicy { TenantId = tenantId, Code = code, NameEn = code, NameAr = code }).Entity;
                context.SaveChanges();
                context.Set<EmployeeTimePolicy>().Add(new EmployeeTimePolicy
                {
                    TenantId = tenantId, EmployeeId = id, TimePolicyId = policy.Id, StartDate = Since, EndDate = Forever,
                });
            }

            if (parts.HasFlag(EmployeeParts.BankWithIban))
            {
                AddPaymentDetails(context, tenantId, id, paymentType, iban: "KW00TEST" + code, accountNumber: null);
            }

            context.SaveChanges();
            return id;
        });
    }

    protected void AddPaymentDetails(int tenantId, int employeeId, PaymentType paymentType, string iban, string accountNumber)
    {
        UsingDbContext(tenantId, context => AddPaymentDetails(context, tenantId, employeeId, paymentType, iban, accountNumber));
    }

    private static void AddPaymentDetails(
        EntityFrameworkCore.HCMv3DbContext context, int tenantId, int employeeId, PaymentType paymentType, string iban, string accountNumber)
    {
        context.Set<EmployeeBank>().Add(new EmployeeBank
        {
            TenantId = tenantId, EmployeeId = employeeId, PaymentType = paymentType, StartDate = Since, EndDate = Forever,
            IBAN = iban, AccountNum = accountNumber,
        });
    }

    /// <summary>An organisation unit whose profile carries the payroll area: the engine's fallback route to one.</summary>
    protected long CreateOrgUnitWithPayrollArea(int tenantId, int payrollAreaId)
    {
        return UsingDbContext(tenantId, context =>
        {
            var unit = context.OrganizationUnits.Add(new OrganizationUnit(tenantId, "Company") { Code = "00001" }).Entity;
            var type = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = tenantId, Code = "Level1", NameEn = "Company", NameAr = "Company", AssignPayrollArea = true,
            }).Entity;
            context.SaveChanges();
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = tenantId, OrganizationUnitId = unit.Id, Code = "CO", NameAr = "CO", OrganizationUnitTypeId = type.Id,
                PayrollAreaId = payrollAreaId,
            });
            context.SaveChanges();
            return unit.Id;
        });
    }
}
